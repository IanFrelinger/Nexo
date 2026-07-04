using System.Runtime.InteropServices;

namespace Nexo.Infrastructure.Testing;

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
