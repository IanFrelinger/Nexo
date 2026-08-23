using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeHistoryResult(
    bool Ok,
    string Summary,
    IReadOnlyList<AdaptiveRuntimeExecutionReport>? Items = null,
    RuntimeHistorySummary? SummaryStats = null);
