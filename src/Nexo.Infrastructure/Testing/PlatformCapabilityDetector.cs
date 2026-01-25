using System.Runtime.InteropServices;

namespace Nexo.Infrastructure.Testing;

/// <summary>
/// Detects platform capabilities for test execution.
/// 
/// Determines whether Docker or native execution should be used
/// based on platform constraints and available capabilities.
/// </summary>
public static class PlatformCapabilityDetector
{
    /// <summary>
    /// Determines if Docker can be used for the specified platform.
    /// </summary>
    public static bool CanUseDocker(string platformName)
    {
        // Platforms that can use Docker
        var dockerPlatforms = new[]
        {
            "ubuntu", "alpine", "debian", "android", "windows", "unity"
        };

        var platformLower = platformName.ToLowerInvariant();
        
        // Check if platform supports Docker
        if (dockerPlatforms.Any(p => platformLower.Contains(p)))
        {
            return true;
        }

        // iOS cannot use Docker (devices and simulators don't support it)
        if (platformLower.Contains("ios"))
        {
            return false;
        }

        // Default to Docker if platform is unknown
        return true;
    }

    /// <summary>
    /// Determines if native execution is required for the specified platform.
    /// </summary>
    public static bool RequiresNativeExecution(string platformName)
    {
        var platformLower = platformName.ToLowerInvariant();
        
        // iOS requires native execution (no Docker support)
        return platformLower.Contains("ios");
    }

    /// <summary>
    /// Gets the execution mode for a platform.
    /// </summary>
    public static ExecutionMode GetExecutionMode(string platformName)
    {
        if (RequiresNativeExecution(platformName))
        {
            return ExecutionMode.Native;
        }

        if (CanUseDocker(platformName))
        {
            return ExecutionMode.Docker;
        }

        return ExecutionMode.Unknown;
    }

    /// <summary>
    /// Checks if the current system supports the platform.
    /// </summary>
    public static bool IsPlatformSupported(string platformName)
    {
        var platformLower = platformName.ToLowerInvariant();

        // iOS requires macOS
        if (platformLower.Contains("ios"))
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        // Windows containers require Windows host
        if (platformLower.Contains("windows"))
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        // Other platforms work on any OS with Docker
        return true;
    }
}

/// <summary>
/// Execution mode for platform tests.
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Use Docker containers (portable, works anywhere Docker is available).
    /// </summary>
    Docker,

    /// <summary>
    /// Use native execution (platform-specific, may have limitations).
    /// </summary>
    Native,

    /// <summary>
    /// Unknown execution mode.
    /// </summary>
    Unknown
}
