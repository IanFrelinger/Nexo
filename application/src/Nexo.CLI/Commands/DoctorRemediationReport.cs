using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Nexo.CLI.Commands;

/// <summary>Doctor remediation report.</summary>
internal sealed class DoctorRemediationReport
{
    /// <summary>Fix enabled.</summary>
    public bool FixEnabled { get; init; }
    /// <summary>Auto approve fixes.</summary>
    public bool AutoApproveFixes { get; init; }
    /// <summary>Attempts.</summary>
    public IReadOnlyList<DoctorRemediationAttempt> Attempts { get; init; } = Array.Empty<DoctorRemediationAttempt>();
}
