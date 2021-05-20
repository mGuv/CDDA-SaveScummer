using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CDDABackup
{
    /// <summary>
    /// Contains the entry point to the application and any bootstrap logic
    ///
    /// Driven entirely by the settings in appSettings.json
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point to the program
        /// </summary>
        /// <returns>Task that upon completion the application haas ended</returns>
        static async Task Main()
        {
            await Program.CreateHostBuilder().RunConsoleAsync();
        }

        /// <summary>
        /// Configures/builds the entire applications main service, including config, DI and logging
        /// </summary>
        /// <returns>The configured Host Builder ready for use</returns>
        private static IHostBuilder CreateHostBuilder()
        {
            return new HostBuilder()
                .ConfigureAppConfiguration(builder => { builder.AddJsonFile("appSettings.json"); })
                .ConfigureServices((services) =>
                    {
                        services
                            // Run CDDA  core as a background service
                            .AddHostedService<BackupHandler>()
                            .AddTransient<SaveWatcher>()
                            .AddOptions<ScummerSettings>().BindConfiguration("CDDABackup");
                    }
                )
                .ConfigureLogging((hostContext, logging) =>
                {
                    ILogger logger =
                        new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration).CreateLogger();
                    logging.AddSerilog(logger);
                });
        }
    }
}