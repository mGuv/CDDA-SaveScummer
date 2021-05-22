using CDDABackup.FileHandling;
using Microsoft.Extensions.DependencyInjection;

namespace CDDABackup
{
    // Class for CDDA related extensions
    public static class CDDAExtensions
    {
        /// <summary>
        /// Extend Service Collection to add helper method to add just CDDA dependencies
        /// </summary>
        /// <param name="container">The container to add dependecies to</param>
        /// <returns>The container for method chaining</returns>
        public static IServiceCollection AddCDDA(this IServiceCollection container)
        {
            container.AddHostedService<BackupHandler>()
                .AddTransient<SaveWatcher>()
                .AddSingleton<Copier>()
                .AddSingleton<BackupWriter>()
                .AddOptions<ScummerSettings>().BindConfiguration("CDDABackup");

            return container;
        }
    }
}