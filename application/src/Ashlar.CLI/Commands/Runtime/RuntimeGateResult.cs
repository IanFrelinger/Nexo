using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeGateResult(
    bool Ok,
    string Summary,
    int? Total = null,
    int? Passed = null,
    double? PassRate = null,
    int? MinTotal = null,
    double? MinPassRate = null,
    int? Streak = null,
    int? MinConsecutivePasses = null,
    RuntimeSloEvidence? SloEvidence = null);
