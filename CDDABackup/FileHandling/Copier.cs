using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace CDDABackup.FileHandling
{
    /// <summary>
    /// Responsible for doing copy based disk actions
    /// </summary>
    public class Copier
    {
        /// <summary>
        /// The logger the Copier will use for logging
        /// </summary>
        private ILogger<Copier> logger;

        /// <summary>
        /// Creates a new Copier with the given dependencies
        /// </summary>
        /// <param name="logger">The Logger the Copier will use for logging</param>
        public Copier(ILogger<Copier> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Copies the given file to the given destination
        /// </summary>
        /// <param name="from">The file to copy</param>
        /// <param name="to">The path to copy the file to</param>
        /// <exception cref="ArgumentException">Thrown when the file to copy is not valid</exception>
        public void CopyFile(FileInfo from, string to)
        {
            // Ensure there's something to copy
            if (!from.Exists)
            {
                this.logger.LogError($"File does not exist to copy: {from.FullName}");
                throw new ArgumentException($"Cannot copy non-existent file: {from.FullName}");
            }
            
            this.logger.LogTrace($"Copying File `{from}` to `{to}`");
            from.CopyTo(to);
        }

        /// <summary>
        /// Copies the given Directory to the target Directory
        /// </summary>
        /// <param name="from">The Directory to copy</param>
        /// <param name="to">The Directory to copy to</param>
        /// <exception cref="ArgumentException">Thrown when the from directory is not valid</exception>
        public void CopyDirectory(DirectoryInfo from, DirectoryInfo to)
        {
            this.logger.LogTrace($"Copying Directory `{from.FullName}` to `{to.FullName}`");
            
            // Ensure there's something to copy
            if (!from.Exists)
            {
                this.logger.LogError($"Directory does not exist to copy: {from.FullName}");
                throw new ArgumentException($"Cannot copy non-existent directory: {from.FullName}");
            }

            // Ensure there's somewhere to copy to
            if (!to.Exists)
            {
                this.logger.LogTrace($"Creating directory: {to.FullName}");
                to.Create();
            }

            // Copy each file
            foreach (FileInfo fileInfo in from.GetFiles())
            {
                this.CopyFile(fileInfo, Path.Combine(to.FullName, fileInfo.Name));
            }

            // Copy each directory
            foreach (DirectoryInfo directoryInfo in from.GetDirectories())
            {
                // Recursion isn't ideal but it ensures everything happens correctly and I doubt this will ever
                // run in to stack overflow problems on a standard directory structure
                this.CopyDirectory(directoryInfo, to.CreateSubdirectory(directoryInfo.Name));
            }
        }

        /// <summary>
        /// Copies the given Directory to the target Path
        /// </summary>
        /// <param name="from">The Directory to copy</param>
        /// <param name="to">The Path to copy to</param>
        public void CopyDirectory(DirectoryInfo from, string to)
        {
            this.CopyDirectory(from, new DirectoryInfo(to));
        }
    }
}