using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeLaneSloEvidence(
    string Lane,
    string BenchmarkSet,
    bool? GateOk,
    string? GateSummary,
    double? PassRate,
    double? MinPassRate,
    int? Total,
    int? PassedCount);
