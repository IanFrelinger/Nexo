using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ashlar.CLI.Commands;

internal sealed record DoctorRemediationAttempt(
    string Id,
    string Problem,
    string Command,
    bool Attempted,
    bool Success,
    string Status,
    string Message,
    int ExitCode,
    string FollowUp);
