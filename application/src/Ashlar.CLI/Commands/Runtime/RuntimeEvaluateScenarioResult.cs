using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeEvaluateScenarioResult(
    string Goal,
    string Policy,
    bool Ok,
    long? ElapsedMs,
    string? FailureStage,
    string Summary);
