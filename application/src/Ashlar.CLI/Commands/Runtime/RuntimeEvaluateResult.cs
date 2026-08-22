using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeEvaluateResult(
    bool Ok,
    string Summary,
    IReadOnlyList<RuntimeEvaluateScenarioResult>? Scenarios = null,
    IReadOnlyList<RuntimeEvaluatePolicySummary>? PolicySummaries = null);
