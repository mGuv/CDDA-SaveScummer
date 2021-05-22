using System;
using System.IO;
using System.IO.Compression;
using CDDABackup.FileHandling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CDDABackup
{
    /// <summary>
    /// Responsible for actually writing backups
    /// </summary>
    public class BackupWriter
    {
        /// <summary>
        /// The logger the backup writer will write to
        /// </summary>
        /// <returns></returns>
        private readonly ILogger<BackupWriter> logger;
        
        /// <summary>
        /// The settings used to configure the Writer
        /// </summary>
        private readonly ScummerSettings settings;

        /// <summary>
        /// The file copier used to actually write the backups
        /// </summary>
        private readonly Copier copier;

        /// <summary>
        /// Creates a new Backup Writer with the given settings
        /// </summary>
        /// <param name="logger">The logger the backup writer will log to</param>
        /// <param name="settings">The settings to use</param>
        /// <param name="copier">The copier for doing the actual backup</param>
        public BackupWriter(ILogger<BackupWriter> logger, IOptions<ScummerSettings> settings, Copier copier)
        {
            this.logger = logger;
            this.settings = settings.Value;
            this.copier = copier;
            
            // Ensure backup directory exists
            Directory.CreateDirectory(this.settings.BackupDirectory);
        }

        public void BackupSave(string save)
        {
            // Grab the Save Directory
            DirectoryInfo saveDirectory = new DirectoryInfo(save);
            
            // Build the Backup Name/Path
            DateTime now = DateTime.Now;
            string backupName = $"{saveDirectory.Name} {now.ToString(this.settings.TimestampFormat)}";
            string backupPath = Path.Combine(this.settings.BackupDirectory, backupName);
            
            // Do the backup
            this.logger.LogTrace($"Making backup of {saveDirectory} to {backupPath}");
            this.copier.CopyDirectory(saveDirectory, backupPath);
            
            // Zip it up and remove unzipped version
            this.logger.LogTrace($"Compressing unzipped backup: {backupPath}");
            ZipFile.CreateFromDirectory(backupPath, backupPath + ".zip", CompressionLevel.Optimal, false);
            
            this.logger.LogTrace($"Deleting unzipped backup: {backupPath}");
            Directory.Delete(backupPath, true);
            
            this.logger.LogInformation($"Wrote backup: {backupPath + ".zip"}");
        }
    }
}