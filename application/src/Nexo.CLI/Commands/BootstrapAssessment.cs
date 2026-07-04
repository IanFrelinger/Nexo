using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Nexo.CLI.Commands;

internal sealed record BootstrapAssessment(
    string Profile,
    string OsDescription,
    bool Supported,
    string? Reason,
    IReadOnlyList<BootstrapDependencyStatus> Dependencies)
{
    public IEnumerable<BootstrapDependencyStatus> MissingRequired =>
        Dependencies.Where(d => d.Required && !d.Installed);
}
