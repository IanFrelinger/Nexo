using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Infrastructure.Adapters.FileSystem
{
    /// <summary>
    /// Existence check functionality for file system adapter.
    /// </summary>
    public sealed partial class FileSystemAdapter
    {
        /// <summary>
        /// Determines whether a directory exists at the specified path.
        /// </summary>
        /// <param name="path">The path of the directory to check for existence. Cannot be null or whitespace.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the directory exists.</returns>
        public async Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }
            
            return await Task.Run(() => Directory.Exists(path), cancellationToken);
        }

        /// <summary>
        /// Asynchronously determines whether a file exists at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the file exists.</returns>
        public async Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }
            
            return await Task.Run(() => File.Exists(path), cancellationToken);
        }
    }
}
