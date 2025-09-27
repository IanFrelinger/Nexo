using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Infrastructure.Adapters.FileSystem
{

/// <summary>
/// A sealed class implementing file system operations using standard .NET I/O functionalities.
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public sealed partial class FileSystemAdapter : IFileSystem
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
        if (logger != null)
            _logger = logger;
        else
            throw new ArgumentNullException(nameof(logger));
    }

}
}