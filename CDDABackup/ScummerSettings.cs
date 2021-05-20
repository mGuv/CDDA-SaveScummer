using System.IO;

namespace CDDABackup
{
    public class ScummerSettings
    {
        public string SaveDirectory { get; set; }
        public string BackupFolderName { get; set; }
        public string BackupDirectory => Path.Combine(this.SaveDirectory, this.BackupFolderName);
        public string TimestampFormat { get; set; }
        public int SaveGracePeriodMilliseconds { get; set; }
        public int TimeBetweenUpdatesMilliseconds { get; set; }
    }
}