using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Nexo.Core.Application.Enums;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Provides platform-specific adaptations for cross-platform operations.
    /// </summary>
    public interface IPlatformAdapter
    {
        /// <summary>
        /// Gets the current platform information.
        /// </summary>
        PlatformInfo PlatformInfo { get; }

        /// <summary>
        /// Gets the preferred container runtime for this platform.
        /// </summary>
        ContainerRuntime PreferredContainerRuntime { get; }

        /// <summary>
        /// Normalizes a file path for the current platform.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        string NormalizePath(string path);

        /// <summary>
        /// Gets the executable extension for the current platform.
        /// </summary>
        /// <returns>The executable extension (e.g., ".exe" on Windows).</returns>
        string GetExecutableExtension();

        /// <summary>
        /// Gets the path separator for the current platform.
        /// </summary>
        /// <returns>The path separator character.</returns>
        char GetPathSeparator();

        /// <summary>
        /// Gets the environment variable names that contain path information.
        /// </summary>
        /// <returns>Array of environment variable names.</returns>
        string[] GetPathEnvironmentVariables();

        /// <summary>
        /// Adapts a command for platform-specific execution.
        /// </summary>
        /// <param name="command">The command to adapt.</param>
        /// <param name="arguments">The command arguments.</param>
        /// <returns>The adapted command and arguments.</returns>
        (string command, string arguments) AdaptCommand(string command, string arguments);

        /// <summary>
        /// Gets platform-specific temporary directory.
        /// </summary>
        /// <returns>The temporary directory path.</returns>
        string GetTempDirectory();

        /// <summary>
        /// Gets platform-specific user data directory.
        /// </summary>
        /// <returns>The user data directory path.</returns>
        string GetUserDataDirectory();

        /// <summary>
        /// Checks if a command is available on the current platform.
        /// </summary>
        /// <param name="command">The command to check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the command is available; otherwise, false.</returns>
        Task<bool> IsCommandAvailableAsync(string command, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets platform-specific shell command for executing scripts.
        /// </summary>
        /// <param name="scriptPath">The script path.</param>
        /// <returns>The shell command and arguments.</returns>
        (string command, string arguments) GetShellCommand(string scriptPath);
    }
}