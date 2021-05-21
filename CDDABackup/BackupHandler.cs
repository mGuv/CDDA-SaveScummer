using System;
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
        /// The Application control so we can gracefully request shutdown via user input
        /// </summary>
        private readonly IHostApplicationLifetime hostApplicationLifetime;

        /// <summary>
        /// The Save Watcher utilised to trigger when a backup should happen
        /// </summary>
        private readonly SaveWatcher saveWatcher;

        /// <summary>
        /// Creates a new Backup Handler with the given config
        /// </summary>
        /// <param name="config">The configuration the Handler will pull values from</param>
        /// <param name="hostApplicationLifetime">The application lifetime to call shutdown on when user requests</param>
        /// <param name="saveWatcher">The Watcher that will be used to detect when a backup should be made</param>
        /// <exception cref="ApplicationException">Thrown when the application is not configured correctly</exception>
        public BackupHandler(IConfiguration config, IHostApplicationLifetime hostApplicationLifetime, SaveWatcher saveWatcher)
        {
            this.hostApplicationLifetime = hostApplicationLifetime;
            
            // Configure Handler
            this.saveDirectory = config["CDDABackup:saveDirectory"];
            this.backupDirectoryPath = saveDirectory + "\\" + config["CDDABackup:backupFolderName"];
            this.timestampFormat = config["CDDABackup:timestampFormat"];
            
            // Check for invalid configuration
            if (this.saveDirectory.Length == 0)
            {
                throw new ApplicationException(
                    "Save directory has not been set! Please modify the appSettings.json to point 'saveDirectory' to your CDDA save directory.");
            }

            this.saveWatcher = saveWatcher;
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

                await saveWatcher.WatchFilesAsync(stoppingToken, save =>
                {
                    // Save has changed, back up time.
                    DirectoryInfo sourceDirectory = new DirectoryInfo(save);
                    
                    // Build the Backup Name/Path
                    DateTime now = DateTime.Now;
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
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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