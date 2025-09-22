using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.PluginHost.Abstractions
{
    /// <summary>
    /// Abstraction for file system operations to prevent plugins from direct I/O access.
    /// </summary>
    public interface INexoFileSystem
    {
        /// <summary>
        /// Checks if a file exists at the specified path.
        /// </summary>
        bool FileExists(string path);

        /// <summary>
        /// Checks if a directory exists at the specified path.
        /// </summary>
        bool DirectoryExists(string path);

        /// <summary>
        /// Reads all text from a file asynchronously.
        /// </summary>
        Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes text to a file asynchronously.
        /// </summary>
        Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the size of a file in bytes.
        /// </summary>
        long GetFileSize(string path);

        /// <summary>
        /// Gets the last write time of a file.
        /// </summary>
        DateTime GetLastWriteTime(string path);

        /// <summary>
        /// Lists files in a directory with optional search pattern.
        /// </summary>
        IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Lists directories in a path with optional search pattern.
        /// </summary>
        IEnumerable<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Creates a directory at the specified path.
        /// </summary>
        DirectoryInfo CreateDirectory(string path);

        /// <summary>
        /// Deletes a file at the specified path.
        /// </summary>
        void DeleteFile(string path);

        /// <summary>
        /// Deletes a directory and optionally all its contents.
        /// </summary>
        void DeleteDirectory(string path, bool recursive = false);

        /// <summary>
        /// Copies a file from source to destination.
        /// </summary>
        void CopyFile(string sourcePath, string destinationPath, bool overwrite = false);

        /// <summary>
        /// Moves a file from source to destination.
        /// </summary>
        void MoveFile(string sourcePath, string destinationPath);

        /// <summary>
        /// Gets the current working directory.
        /// </summary>
        string GetCurrentDirectory();

        /// <summary>
        /// Gets the full path of a file or directory.
        /// </summary>
        string GetFullPath(string path);

        /// <summary>
        /// Combines path segments.
        /// </summary>
        string CombinePath(params string[] paths);

        /// <summary>
        /// Gets the directory name from a file path.
        /// </summary>
        string GetDirectoryName(string path);

        /// <summary>
        /// Gets the file name from a file path.
        /// </summary>
        string GetFileName(string path);

        /// <summary>
        /// Gets the file name without extension from a file path.
        /// </summary>
        string GetFileNameWithoutExtension(string path);

        /// <summary>
        /// Gets the file extension from a file path.
        /// </summary>
        string GetExtension(string path);

        /// <summary>
        /// Changes the extension of a file path.
        /// </summary>
        string ChangeExtension(string path, string extension);
    }
}
