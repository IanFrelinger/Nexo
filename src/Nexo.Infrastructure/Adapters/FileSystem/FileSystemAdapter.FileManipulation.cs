using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Infrastructure.Adapters.FileSystem
{
    /// <summary>
    /// File manipulation functionality for file system adapter.
    /// </summary>
    public sealed partial class FileSystemAdapter
    {
        /// <summary>
        /// Copies a file from the specified source path to the specified destination path asynchronously.
        /// </summary>
        /// <param name="sourcePath">The full path of the source file to copy.</param>
        /// <param name="destinationPath">The full path of the destination file, including the file name, where the source file will be copied.</param>
        /// <param name="overwrite">A boolean value indicating whether to overwrite the file if it already exists at the destination path. Defaults to <c>false</c>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the file copy operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CopyFileAsync(
            string sourcePath, 
            string destinationPath, 
            bool overwrite = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("Source path cannot be null or whitespace.", nameof(sourcePath));
            }
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Destination path cannot be null or whitespace.", nameof(destinationPath));
            }
            
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }
                
                await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite), cancellationToken);
                _logger.LogDebug("Copied file from {Source} to {Destination}", sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file from {Source} to {Destination}", sourcePath, destinationPath);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Moves a file from the specified source path to the specified destination path.
        /// </summary>
        /// <param name="sourcePath">The full path of the source file to be moved.</param>
        /// <param name="destinationPath">The full path where the file will be moved.</param>
        /// <param name="overwrite">Specifies whether to overwrite the destination file if it already exists. Default is false.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous file move operation.</returns>
        /// <exception cref="ArgumentException">Thrown if the source or destination path is null, empty, or whitespace.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs during the file move operation.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the caller does not have the required permissions to access the file or directories.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public async Task MoveFileAsync(
            string sourcePath, 
            string destinationPath, 
            bool overwrite = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("Source path cannot be null or whitespace.", nameof(sourcePath));
            }
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Destination path cannot be null or whitespace.", nameof(destinationPath));
            }
            
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }
                
                await Task.Run(() =>
                {
                    if (overwrite && File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }
                    File.Move(sourcePath, destinationPath);
                }, cancellationToken);
                
                _logger.LogDebug("Moved file from {Source} to {Destination}", sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file from {Source} to {Destination}", sourcePath, destinationPath);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
