using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CDDABackup
{
    /// <summary>
    /// Handler responsible for noticing changes to a save and writing a backup
    /// </summary>
    public class BackupHandler
    {
        /// <summary>
        /// The core CDDA directory where saves are written to
        /// </summary>
        private readonly string saveDirectory;
        
        /// <summary>
        /// The desired folder name to use to stash backups in
        /// </summary>
        private readonly string backupDirectoryName;
        
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
        
        private Dictionary<string, DateTime> lastWritten = new Dictionary<string, DateTime>();

        /// <summary>
        /// Creates a new Backup Handler with the given config
        /// </summary>
        /// <param name="config">The configuration the Handler will pull values from</param>
        /// <exception cref="ApplicationException">Thrown when the application is not configured correctly</exception>
        public BackupHandler(IConfiguration config)
        {
            // Configure Handler
            this.saveDirectory = config["CDDABackup:saveDirectory"];
            this.backupDirectoryName = saveDirectory + "\\" + config["CDDABackup:backupFolderName"];
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
        /// <param name="cancellationToken">The token that the Handler will watch for when it should stop</param>
        /// <returns>Indefinite backup task that only stops once the token has been cancelled</returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Ensure backup directory exists
                Directory.CreateDirectory(this.backupDirectoryName);

                // Watch Save Directory for changes
                var dirWatcher = new FileSystemWatcher(this.saveDirectory);
                dirWatcher.NotifyFilter = NotifyFilters.LastWrite;
                dirWatcher.EnableRaisingEvents = true;
                dirWatcher.Changed += OnSaveChanged;

                // Run until cancelled
                while (!cancellationToken.IsCancellationRequested)
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

                        // Create backup path/name
                        string saveName = kvp.Key.Substring(kvp.Key.LastIndexOf("\\") + 1);
                        string backupName = $"{saveName} {now.ToString(timestampFormat)}";
                        string backupPath = $"{this.backupDirectoryName}\\{backupName}";

                        // Do the backup
                        Directory.CreateDirectory(backupPath);
                        foreach (var file in Directory.GetFiles(kvp.Key))
                        {
                            File.Copy(file, file.Replace(kvp.Key, backupPath), false);
                        }

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
            if (path == this.backupDirectoryName)
            {
                return;
            }
            
            // Note the time we saw this update
            lastWritten[path] = DateTime.Now;
        }
    }
}