using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.UnitySidecarDemo;

/// <summary>Command result value.</summary>
/// <param name="ExitCode">Exit code.</param>
/// <param name="StdOut">Std out.</param>
/// <param name="StdErr">Std err.</param>
internal sealed record CommandResult(int ExitCode, string StdOut, string StdErr);
