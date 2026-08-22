using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeSubprocessResult(int ExitCode, string StdOut, string StdErr);
