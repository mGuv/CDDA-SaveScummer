using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CDDABackup
{
    /// <summary>
    /// Contains the entry point to the application and any bootstrap logic
    ///
    /// Driven entirely by the settings in appSettings.json
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main entry point to the program
        /// </summary>
        /// <returns>Task that upon completion the application haas ended</returns>
        static async Task Main()
        {
            // Log out fluffy intro
            Console.WriteLine("[CDDA Backup Tool]");
            Console.WriteLine("----------------");
            Console.WriteLine("Press any key to stop.");
            
            // Build Handler
            BackupHandler handler = new BackupHandler(Program.buildConfig());

            // Run two tasks, one to catch the user Input to cancel and one to actually run the handler
            CancellationTokenSource cts = new CancellationTokenSource();
            await Task.WhenAll(
                Task.Run(() =>
                {
                    Console.ReadKey();
                    Console.WriteLine();
                    Console.WriteLine("Shutting Down...");
                    cts.Cancel();
                }),
                handler.RunAsync(cts.Token)
            );
        }
        
        static IConfigurationRoot buildConfig()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appSettings.json", false, false);
            return builder.Build();
        }
    }
}