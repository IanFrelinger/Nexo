using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Infrastructure.Adapters.FileSystem
{
    /// <summary>
    /// Information retrieval functionality for file system adapter.
    /// </summary>
    public sealed partial class FileSystemAdapter
    {
        /// <summary>
        /// Retrieves detailed file information for the specified file path.
        /// </summary>
        /// <param name="path">The path to the file for which information is being retrieved.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="FileInfo"/> object containing metadata about the specified file.</returns>
        public async Task<FileInfo> GetFileInfoAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }
            
            return await Task.Run(() => new FileInfo(path), cancellationToken);
        }

        /// <summary>
        /// Retrieves information about a specified directory.
        /// </summary>
        /// <param name="path">The path to the directory for which information is requested.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="DirectoryInfo"/> object representing the specified directory.</returns>
        public async Task<DirectoryInfo> GetDirectoryInfoAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }
            
            return await Task.Run(() => new DirectoryInfo(path), cancellationToken);
        }
    }
}