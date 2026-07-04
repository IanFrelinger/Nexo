using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeHistoryResult(
    bool Ok,
    string Summary,
    IReadOnlyList<AdaptiveRuntimeExecutionReport>? Items = null,
    RuntimeHistorySummary? SummaryStats = null);
