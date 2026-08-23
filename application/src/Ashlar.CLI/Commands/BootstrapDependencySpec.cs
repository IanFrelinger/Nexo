using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Ashlar.CLI.Commands;

internal sealed record BootstrapDependencySpec(
    string Id,
    string DisplayName,
    string ProbeCommand,
    string InstallCommand,
    bool Required,
    bool Optional);
