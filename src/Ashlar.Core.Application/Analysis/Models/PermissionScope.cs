namespace Ashlar.Core.Application.Analysis.Models;

/// <summary>
/// Permission scope for bricks/operations. Application scope cannot perform OS-level operations.
/// </summary>
public enum PermissionScope
{
    /// <summary>Application-level: file I/O within project, API calls. No Process.Start, no raw OS access.</summary>
    Application,

    /// <summary>OS-level: process control, system-wide file access, etc.</summary>
    OperatingSystem,
}
