using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeEvaluateResult(
    bool Ok,
    string Summary,
    IReadOnlyList<RuntimeEvaluateScenarioResult>? Scenarios = null,
    IReadOnlyList<RuntimeEvaluatePolicySummary>? PolicySummaries = null);
