using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Nexo.CLI.Commands;

internal sealed record BootstrapDependencyStatus(
    string Id,
    string DisplayName,
    bool Installed,
    bool Required,
    bool Optional,
    string InstallCommand,
    string? ProbeError);
