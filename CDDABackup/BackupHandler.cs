using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using CDDABackup.FileHandling;
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
        /// The Application control so we can gracefully request shutdown via user input
        /// </summary>
        private readonly IHostApplicationLifetime hostApplicationLifetime;

        /// <summary>
        /// The Save Watcher utilised to trigger when a backup should happen
        /// </summary>
        private readonly SaveWatcher saveWatcher;

        /// <summary>
        /// The Backup Writer utilised to actually write backups
        /// </summary>
        private readonly BackupWriter backupWriter;

        /// <summary>
        /// Creates a new Backup Handler with the given config
        /// </summary>
        /// <param name="hostApplicationLifetime">The application lifetime to call shutdown on when user requests</param>
        /// <param name="saveWatcher">The Watcher that will be used to detect when a backup should be made</param>
        /// <param name="backupWriter">The Backup Writer that the Handler will use to make backups</param>
        /// <exception cref="ApplicationException">Thrown when the application is not configured correctly</exception>
        public BackupHandler(IHostApplicationLifetime hostApplicationLifetime, SaveWatcher saveWatcher, BackupWriter backupWriter)
        {
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.saveWatcher = saveWatcher;
            this.backupWriter = backupWriter;
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
                await this.saveWatcher.WatchFilesAsync(stoppingToken, save =>
                {
                    this.backupWriter.BackupSave(save);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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