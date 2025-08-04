using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Provides methods for interacting with the file system.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Checks if a directory exists.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the directory exists; otherwise, false.</returns>
        Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file exists.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a directory.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads text from a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file contents.</returns>
        Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes text to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="content">The content to write.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="recursive">Whether to delete recursively.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists files in a directory.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="recursive">Whether to search recursively.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of file paths.</returns>
        Task<IEnumerable<string>> ListFilesAsync(
            string path, 
            string searchPattern = "*",
            bool recursive = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies a file.
        /// </summary>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="destinationPath">The destination file path.</param>
        /// <param name="overwrite">Whether to overwrite existing file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CopyFileAsync(
            string sourcePath, 
            string destinationPath, 
            bool overwrite = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves a file.
        /// </summary>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="destinationPath">The destination file path.</param>
        /// <param name="overwrite">Whether to overwrite existing file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task MoveFileAsync(
            string sourcePath, 
            string destinationPath, 
            bool overwrite = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file information.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>File information.</returns>
        Task<FileInfo> GetFileInfoAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets directory information.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Directory information.</returns>
        Task<DirectoryInfo> GetDirectoryInfoAsync(string path, CancellationToken cancellationToken = default);
    }
}