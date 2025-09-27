using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Infrastructure.Adapters.FileSystem
{
    /// <summary>
    /// Directory operations functionality for file system adapter.
    /// </summary>
    public sealed partial class FileSystemAdapter
    {
        /// <summary>
        /// Creates a directory at the specified path if it does not already exist.
        /// </summary>
        /// <param name="path">The path where the directory should be created.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }
            
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(path)) return;
                    Directory.CreateDirectory(path);
                    _logger.LogDebug("Created directory: {Path}", path);
                }, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Deletes the specified directory at the given path.
        /// </summary>
        /// <param name="path">The path of the directory to be deleted.</param>
        /// <param name="recursive">A boolean indicating whether to delete directories, subdirectories, and files recursively. Default is false.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous delete operation. The task result contains a boolean indicating whether the operation was successful.</returns>
        public async Task DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }
            
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await Task.Run(() =>
                {
                    if (!Directory.Exists(path)) return;
                    Directory.Delete(path, recursive);
                    _logger.LogDebug("Deleted directory: {Path} (Recursive: {Recursive})", path, recursive);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting directory: {Path}", path);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Lists files in the specified directory based on the provided search pattern and options.
        /// </summary>
        /// <param name="path">The directory path to search for files.</param>
        /// <param name="searchPattern">The search pattern to match against file names. Defaults to "*".</param>
        /// <param name="recursive">
        /// A boolean value indicating whether to perform a recursive search through subdirectories. Defaults to false.
        /// </param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>An asynchronous task that, when completed, contains a collection of file paths as strings.</returns>
        public async Task<IEnumerable<string>> ListFilesAsync(
            string path, 
            string searchPattern = "*",
            bool recursive = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                throw new ArgumentException("Search pattern cannot be null or whitespace.", nameof(searchPattern));
            }
            
            return await Task.Run(() =>
            {
                if (!Directory.Exists(path))
                {
                    _logger.LogWarning("Directory does not exist: {Path}", path);
                    return Enumerable.Empty<string>();
                }
                
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(path, searchPattern, searchOption);
            }, cancellationToken);
        }
    }
}
