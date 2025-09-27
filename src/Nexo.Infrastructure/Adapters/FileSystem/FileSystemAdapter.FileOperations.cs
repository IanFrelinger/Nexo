using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Infrastructure.Adapters.FileSystem
{
    /// <summary>
    /// File operations functionality for file system adapter.
    /// </summary>
    public sealed partial class FileSystemAdapter
    {
        /// <summary>
        /// Reads the contents of a text file asynchronously.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous file read operation. The value of the task contains the contents of the file as a string.</returns>
        /// <exception cref="ArgumentException">Thrown when the file path is null, empty, or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file is not found.</exception>
        /// <exception cref="Exception">Thrown when an unexpected error occurs during file reading.</exception>
        public async Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }
            
            try
            {
                return await File.ReadAllTextAsync(path, cancellationToken);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "File not found: {Path}", path);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {Path}", path);
                throw;
            }
        }

        /// <summary>
        /// Writes the specified text to a file at the given path asynchronously.
        /// </summary>
        /// <param name="path">The file path where the text should be written.</param>
        /// <param name="content">The content to write to the file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="path"/> is null, empty, or consists only of white-space characters.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="content"/> is null.</exception>
        /// <exception cref="Exception">Thrown when an error occurs while writing the file.</exception>
        public async Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }

            if (content != null)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                        _logger.LogDebug("Created directory for file: {Directory}", directory);
                    }

                    await File.WriteAllTextAsync(path, content, cancellationToken);
                    _logger.LogDebug("Wrote file: {Path} ({Length} bytes)", path, content.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error writing file: {Path}", path);
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        /// <summary>
        /// Deletes the specified file if it exists.
        /// </summary>
        /// <param name="path">The full path of the file to delete.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result is true if the file was successfully deleted; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="path"/> is null, empty, or consists only of white-space characters.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs while trying to delete the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the caller does not have the required permission to delete the file.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
        public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
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
                    if (!File.Exists(path)) return;
                    File.Delete(path);
                    _logger.LogDebug("Deleted file: {Path}", path);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Path}", path);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
