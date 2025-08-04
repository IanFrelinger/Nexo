using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reactive.Subjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Shared.Enums;
using Nexo.Shared.Interfaces;
using Nexo.Shared.Models;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Infrastructure.Adapters
{

/// <summary>
/// A sealed class implementing file system operations using standard .NET I/O functionalities.
/// </summary>
public sealed class FileSystemAdapter : IFileSystem
{
    /// <summary>
    /// Logger instance used to log debug, error, and informational messages related to file
    /// and directory operations performed by the <see cref="FileSystemAdapter"/>.
    /// </summary>
    private readonly ILogger<FileSystemAdapter> _logger;

    /// <summary>
    /// A semaphore used to coordinate access to critical sections within the
    /// <see cref="FileSystemAdapter"/>, ensuring thread-safe operations when performing
    /// file system operations such as creating directories or writing files.
    /// </summary>
    /// <remarks>
    /// This semaphore is initialized with a value of 1, enforcing mutual exclusion
    /// by allowing only one thread at a time to access the protected sections.
    /// It is used within async methods like <see cref="FileSystemAdapter.CreateDirectoryAsync"/>
    /// and <see cref="FileSystemAdapter.WriteTextAsync"/> for locking purposes.
    /// </remarks>
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Represents an adapter that provides file system operations using .NET standard I/O functionality.
    /// </summary>
    public FileSystemAdapter(ILogger<FileSystemAdapter> logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }
        _logger = logger;
    }

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
            return File.ReadAllText(path);
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
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }
        
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory for file: {Directory}", directory);
            }
            
            File.WriteAllText(path, content);
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