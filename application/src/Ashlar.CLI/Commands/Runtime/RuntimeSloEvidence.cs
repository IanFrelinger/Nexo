using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeSloEvidence(
    DateTimeOffset GeneratedAtUtc,
    string Mode,
    bool VisualLaneRequired,
    RuntimeSloThresholds Thresholds,
    IReadOnlyList<RuntimeSloCheck> Checks,
    IReadOnlyList<RuntimeLaneSloEvidence> Lanes,
    int TotalSamples,
    double NcrResolutionP95Ms,
    double NcrLoadP95Ms,
    double NcrOutcomeP95Ms,
    double NcrFailureRate,
    bool Passed);
