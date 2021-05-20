using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CDDABackup
{
    /// <summary>
    /// Handler responsible for noticing changes to a save and writing a backup
    /// </summary>
    public class BackupHandler : BackgroundService
    {
        /// <summary>
        /// The core CDDA directory where saves are written to
        /// </summary>
        private readonly string saveDirectory;

        /// <summary>
        /// The directory path that backups are saved to
        /// </summary>
        private readonly string backupDirectoryPath;

        /// <summary>
        /// The timestamp format to attach to each backup
        /// </summary>
        private readonly string timestampFormat;

        /// <summary>
        /// The amount of time a save must be inactive before being backed up, this is to avoid the backup happening
        /// too early on slower machines
        /// </summary>
        private readonly int saveGracePeriodMilliseconds;

        /// <summary>
        /// The amount of time to wait between updates, this is to avoid throttling on slower machines
        /// </summary>
        private readonly int timeBetweenUpdatesMilliseconds;

        /// <summary>
        /// Lookup for the last time a folder (the key) was modified (the value), used to control when to actually start making the backup
        /// </summary>
        private Dictionary<string, DateTime> lastWritten = new Dictionary<string, DateTime>();

        /// <summary>
        /// The Application control so we can gracefully request shutdown via user input
        /// </summary>
        private IHostApplicationLifetime hostApplicationLifetime;

        /// <summary>
        /// Creates a new Backup Handler with the given config
        /// </summary>
        /// <param name="config">The configuration the Handler will pull values from</param>
        /// <param name="hostApplicationLifetime">The application lifetime to call shutdown on when user requests</param>
        /// <exception cref="ApplicationException">Thrown when the application is not configured correctly</exception>
        public BackupHandler(IConfiguration config, IHostApplicationLifetime hostApplicationLifetime)
        {
            this.hostApplicationLifetime = hostApplicationLifetime;
            
            // Configure Handler
            this.saveDirectory = config["CDDABackup:saveDirectory"];
            this.backupDirectoryPath = saveDirectory + "\\" + config["CDDABackup:backupFolderName"];
            this.timestampFormat = config["CDDABackup:timestampFormat"];
            this.saveGracePeriodMilliseconds = int.Parse(config["CDDABackup:saveGracePeriodMilliseconds"]);
            this.timeBetweenUpdatesMilliseconds = int.Parse(config["CDDABackup:timeBetweenUpdatesMilliseconds"]);

            // Check for invalid configuration
            if (this.saveDirectory.Length == 0)
            {
                throw new ApplicationException(
                    "Save directory has not been set! Please modify the appSettings.json to point 'saveDirectory' to your CDDA save directory.");
            }
        }
        
        /// <summary>
        /// Run the main backup tasks until cancelled
        /// </summary>
        /// <param name="stoppingToken">The token that the Handler will watch for when it should stop</param>
        /// <returns>Indefinite backup task that only stops once the token has been cancelled</returns>
        private async Task RunBackups(CancellationToken stoppingToken)
        {
            try
            {
                // Ensure backup directory exists
                Directory.CreateDirectory(this.backupDirectoryPath);

                // Watch Save Directory for changes
                var dirWatcher = new FileSystemWatcher(this.saveDirectory);
                dirWatcher.NotifyFilter = NotifyFilters.LastWrite;
                dirWatcher.EnableRaisingEvents = true;
                dirWatcher.Changed += OnSaveChanged;

                // Run until cancelled
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Monitor for changes
                    HashSet<string> keysToRemove = new HashSet<string>();
                    foreach (var kvp in lastWritten)
                    {
                        // Make sure save has finished writing before copying or we'll get half the save
                        DateTime now = DateTime.Now;
                        if (now - kvp.Value < TimeSpan.FromMilliseconds(this.saveGracePeriodMilliseconds))
                        {
                            continue;
                        }

                        // Get original directory
                        DirectoryInfo sourceDirectory = new DirectoryInfo(kvp.Key);

                        // Build the Backup Name/Path
                        string saveName = sourceDirectory.Name;
                        string backupName = $"{saveName} {now.ToString(timestampFormat)}";
                        string backupPath = Path.Combine(this.backupDirectoryPath, backupName);

                        // Ensure the back up directory exists
                        DirectoryInfo targetDirectory = new DirectoryInfo(backupPath);
                        Directory.CreateDirectory(targetDirectory.FullName);

                        // Do the backup
                        this.BackupFolder(sourceDirectory, targetDirectory);

                        // Zip it up and remove unzipped version
                        ZipFile.CreateFromDirectory(backupPath, backupPath + ".zip", CompressionLevel.Optimal, false);
                        Directory.Delete(backupPath, true);

                        Console.WriteLine($"Wrote backup: {backupPath + ".zip"}");

                        // Mark save as does not need processing
                        keysToRemove.Add(kvp.Key);
                    }

                    // Remove any saves that were processed
                    foreach (string s in keysToRemove)
                    {
                        lastWritten.Remove(s);
                    }

                    // Avoid throttling when no work is required
                    await Task.Delay(this.timeBetweenUpdatesMilliseconds);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Handler for when a file changes within a save directory
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event containing the file changed</param>
        private void OnSaveChanged(object sender, FileSystemEventArgs e)
        {
            // Only mark actual saves
            string path = e.FullPath;
            if (path == this.backupDirectoryPath)
            {
                return;
            }

            // Note the time we saw this update
            lastWritten[path] = DateTime.Now;
        }

        /// <summary>
        /// Takes a given directory and writes an exact copy of all of its contents to the backup directory, recursively.
        /// </summary>
        /// <param name="sourceDirectory">The original Directory to Copy</param>
        /// <param name="backupDirectory">The destination of the Copy</param>
        private void BackupFolder(DirectoryInfo sourceDirectory, DirectoryInfo backupDirectory)
        {
            // Backup all the files
            foreach (var file in sourceDirectory.GetFiles())
            {
                file.CopyTo(Path.Combine(backupDirectory.FullName, file.Name));
            }

            // Backup all the Directories
            foreach (var directory in sourceDirectory.GetDirectories())
            {
                // Recursion isn't ideal but I highly doubt this will ever be nested enough to stack overflow
                this.BackupFolder(directory, backupDirectory.CreateSubdirectory(directory.Name));
            }
        }
        
        /// <summary>
        /// Wraps the backup task with control logic
        /// </summary>
        /// <param name="stoppingToken">The application wide stopping token</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Write out fluffy intro
            Console.WriteLine("[CDDA Backup Tool]");
            Console.WriteLine("----------------");
            Console.WriteLine("Press any key to stop.");
            
            await Task.WhenAny(
                Task.Delay(-1, stoppingToken),
                Task.WhenAll(
                    Task.Run(() =>
                    {
                        Console.ReadKey();
                        Console.WriteLine();
                        Console.WriteLine("Shutting Down...");
                        hostApplicationLifetime.StopApplication();
                    }),
                    this.RunBackups(stoppingToken)
                )
            );
            
            Console.WriteLine("done");
        }
    }
}