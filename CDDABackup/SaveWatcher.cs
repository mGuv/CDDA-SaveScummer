using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CDDABackup
{
    /// <summary>
    /// Class responsible for monitoring the save directory and triggering events when required
    /// </summary>
    public class SaveWatcher
    {
        /// <summary>
        /// The settings used for the Save Watcher
        /// </summary>
        private readonly ScummerSettings settings;

        /// <summary>
        /// The logger used by the SaveWatcher
        /// </summary>
        private readonly ILogger<SaveWatcher> logger;

        /// <summary>
        /// Lookup for the last time a folder (the key) was modified (the value), used to control when to fire OnSaveChanged
        /// </summary>
        private readonly Dictionary<string, DateTime> lastSaveUpdate = new();

        /// <summary>
        /// Builds a new SaveWatcher with the given dependencies
        /// </summary>
        /// <param name="settings">The settings the SaveWatcher will use to configure itself</param>
        /// <param name="logger">The logger the SaveWatcher will write to</param>
        public SaveWatcher(IOptions<ScummerSettings> settings, ILogger<SaveWatcher> logger)
        {
            this.settings = settings.Value;
            this.logger = logger;
        }

        /// <summary>
        /// Runs the SaveWatcher until cancelled, triggering the passed in callback whenever a save changes
        /// </summary>
        /// <param name="stoppingToken">The token to cancel the execution</param>
        /// <param name="onSaveChanged">Action to call upon a Save change being detected, passing in the name of the save</param>
        public async Task WatchFilesAsync(CancellationToken stoppingToken, Action<string> onSaveChanged)
        {
            var dirWatcher = new FileSystemWatcher(this.settings.SaveDirectory)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            dirWatcher.Changed += this.OnFileChanged;
            dirWatcher.Error += (sender, args) =>
            {
                this.logger.LogError(args.GetException(), "Inner Exception thrown in FileWatcher");
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                HashSet<string> savesHandled = new();
                foreach (var (save, date) in this.lastSaveUpdate)
                {
                    // Make sure save has finished writing before copying or we'll get half the save
                    DateTime now = DateTime.Now;
                    if (now - date < TimeSpan.FromMilliseconds(this.settings.SaveGracePeriodMilliseconds))
                    {
                        this.logger.LogTrace($"Skipped save due to grace period: {save}");
                        continue;
                    }

                    // trigger the event
                    this.logger.LogDebug($"Triggering OnSaveChanged: {save}");
                    onSaveChanged(save);

                    // Mark the save as handled
                    savesHandled.Add(save);
                }

                // Remove any saves that were processed
                foreach (string save in savesHandled)
                {
                    this.logger.LogTrace($"Marking save as processed {save}");
                    this.lastSaveUpdate.Remove(save);
                }

                // Avoid throttling when no work is required
                await Task.Delay(this.settings.TimeBetweenUpdatesMilliseconds, stoppingToken);
            }
        }

        /// <summary>
        /// Handler for when a file changes within a save directory
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event containing the file changed</param>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Only mark actual saves
            string path = e.FullPath;
            if (path == this.settings.BackupDirectory)
            {
                this.logger.LogTrace($"Skipped backup directory: {path}");
                return;
            }

            // Note the time we saw this update
            this.lastSaveUpdate[path] = DateTime.Now;
            this.logger.LogTrace($"Marked save as seen: {path}");
        }
    }
}