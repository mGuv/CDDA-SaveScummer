using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CDDABackup
{
    public class Program
    {
        static async Task Main()
        {
            await Program.CreateHostBuilder().RunConsoleAsync();
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return new HostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile("appSettings.json");
                })
                .ConfigureServices((services) =>
                    {
                        services
                            // Specify the class that is the app/service that should be ran.
                            .AddHostedService<BackupHandler>();
                    }
                ).ConfigureLogging((hostContext, logging) =>
                {
                    ILogger logger =
                        new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration).CreateLogger();
                    logging.AddSerilog(logger);
                });
        }
    }
}