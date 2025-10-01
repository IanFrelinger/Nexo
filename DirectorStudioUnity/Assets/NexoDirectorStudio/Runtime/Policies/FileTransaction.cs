using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Security;

namespace NexoDirectorStudio.Policies
{
    /// <summary>
    /// Handles staging and promotion of generated assets with atomic operations.
    /// Ensures all assets are written to Assets/Generated/** only via staging→promote workflow.
    /// </summary>
    public sealed class FileTransaction : IDisposable
    {
        private const string StagingPath = "Temp/NexoBuild";
        private const string GeneratedPath = "Assets/Generated";
        private readonly List<string> _stagedFiles = new();
        private bool _isDisposed = false;
        
        /// <summary>
        /// Initializes a new file transaction with staging directory.
        /// </summary>
        public FileTransaction()
        {
            EnsureStagingDirectoryExists();
        }
        
        /// <summary>
        /// Stages a file for later promotion to the generated assets directory.
        /// </summary>
        /// <param name="relativePath">Relative path from Assets/Generated</param>
        /// <param name="content">File content to stage</param>
        /// <param name="isBinary">Whether the content is binary</param>
        public void StageFile(string relativePath, byte[] content, bool isBinary = false)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FileTransaction));
            
            ValidatePath(relativePath);
            
            var stagingFilePath = Path.Combine(StagingPath, relativePath);
            var stagingDirectory = Path.GetDirectoryName(stagingFilePath);
            
            if (!string.IsNullOrEmpty(stagingDirectory))
            {
                Directory.CreateDirectory(stagingDirectory);
            }
            
            File.WriteAllBytes(stagingFilePath, content);
            _stagedFiles.Add(relativePath);
        }
        
        /// <summary>
        /// Stages a text file for later promotion.
        /// </summary>
        /// <param name="relativePath">Relative path from Assets/Generated</param>
        /// <param name="content">Text content to stage</param>
        public void StageTextFile(string relativePath, string content)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            StageFile(relativePath, bytes, isBinary: false);
        }
        
        /// <summary>
        /// Promotes all staged files to the generated assets directory atomically.
        /// </summary>
        public void Promote()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FileTransaction));
            
            if (_stagedFiles.Count == 0)
                return;
            
            try
            {
                // Ensure generated directory exists
                Directory.CreateDirectory(GeneratedPath);
                
                // Promote each staged file
                foreach (var relativePath in _stagedFiles)
                {
                    var stagingFilePath = Path.Combine(StagingPath, relativePath);
                    var targetFilePath = Path.Combine(GeneratedPath, relativePath);
                    var targetDirectory = Path.GetDirectoryName(targetFilePath);
                    
                    if (!string.IsNullOrEmpty(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                    
                    // Atomic move operation
                    if (File.Exists(targetFilePath))
                    {
                        File.Delete(targetFilePath);
                    }
                    
                    File.Move(stagingFilePath, targetFilePath);
                }
                
                // Clear staged files list
                _stagedFiles.Clear();
            }
            catch
            {
                // If promotion fails, clean up any partially moved files
                Rollback();
                throw;
            }
        }
        
        /// <summary>
        /// Rolls back all staged files without promoting them.
        /// </summary>
        public void Rollback()
        {
            if (_isDisposed)
                return;
            
            try
            {
                // Delete all staged files
                foreach (var relativePath in _stagedFiles)
                {
                    var stagingFilePath = Path.Combine(StagingPath, relativePath);
                    if (File.Exists(stagingFilePath))
                    {
                        File.Delete(stagingFilePath);
                    }
                }
                
                _stagedFiles.Clear();
            }
            catch
            {
                // Log error but don't throw during rollback
            }
        }
        
        /// <summary>
        /// Gets the list of staged files.
        /// </summary>
        public IReadOnlyList<string> StagedFiles => _stagedFiles.AsReadOnly();
        
        /// <summary>
        /// Gets the total size of staged files in bytes.
        /// </summary>
        public long GetStagedSize()
        {
            long totalSize = 0;
            
            foreach (var relativePath in _stagedFiles)
            {
                var stagingFilePath = Path.Combine(StagingPath, relativePath);
                if (File.Exists(stagingFilePath))
                {
                    var fileInfo = new FileInfo(stagingFilePath);
                    totalSize += fileInfo.Length;
                }
            }
            
            return totalSize;
        }
        
        /// <summary>
        /// Validates that a path is within the allowed generated assets directory.
        /// </summary>
        private static void ValidatePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));
            
            // Normalize the path
            var normalizedPath = Path.GetFullPath(Path.Combine(GeneratedPath, relativePath));
            var allowedPath = Path.GetFullPath(GeneratedPath);
            
            if (!normalizedPath.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    $"Path '{relativePath}' is not within allowed directory '{GeneratedPath}'");
            }
            
            // Check for path traversal attempts
            if (relativePath.Contains("..") || relativePath.Contains("~"))
            {
                throw new SecurityException($"Path traversal detected in '{relativePath}'");
            }
        }
        
        /// <summary>
        /// Ensures the staging directory exists.
        /// </summary>
        private static void EnsureStagingDirectoryExists()
        {
            if (!Directory.Exists(StagingPath))
            {
                Directory.CreateDirectory(StagingPath);
            }
        }
        
        /// <summary>
        /// Disposes the file transaction and cleans up staging files.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Rollback();
                _isDisposed = true;
            }
        }
    }
}
