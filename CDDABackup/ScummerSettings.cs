using System.IO;

namespace CDDABackup
{
    /// <summary>
    /// Options for configuring the Save Scummer
    /// </summary>
    public class ScummerSettings
    {
        /// <summary>
        /// The Directory that the game saves exist in, used to watch for changes and the folders to backup
        /// </summary>
        public string SaveDirectory { get; set; }
        
        /// <summary>
        /// The name of the folder that will be created to store backups inside
        /// </summary>
        public string BackupFolderName { get; set; }
        
        /// <summary>
        /// The Directory in which the backups will be located
        /// </summary>
        public string BackupDirectory => Path.Combine(this.SaveDirectory, this.BackupFolderName);
        
        /// <summary>
        /// The DateTime format that will be used when naming the backup
        /// </summary>
        public string TimestampFormat { get; set; }
        
        /// <summary>
        /// The delay in milliseconds after a save changes before the backup happens. Used as a single save change causes
        /// many events, so there needs to be some time to pass to wait for all parts of the save to have been written
        /// </summary>
        public int SaveGracePeriodMilliseconds { get; set; }
        
        /// <summary>
        /// How often in milliseconds the Scummer will check to see if it needs to write a backup
        /// </summary>
        public int TimeBetweenUpdatesMilliseconds { get; set; }
    }
}