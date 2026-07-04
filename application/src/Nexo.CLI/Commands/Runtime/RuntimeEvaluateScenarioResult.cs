using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeEvaluateScenarioResult(
    string Goal,
    string Policy,
    bool Ok,
    long? ElapsedMs,
    string? FailureStage,
    string Summary);
