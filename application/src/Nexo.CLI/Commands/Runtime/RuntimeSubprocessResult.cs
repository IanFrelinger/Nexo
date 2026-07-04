using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeSubprocessResult(int ExitCode, string StdOut, string StdErr);
