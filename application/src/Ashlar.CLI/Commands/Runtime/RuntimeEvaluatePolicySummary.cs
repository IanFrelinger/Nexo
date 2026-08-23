using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeEvaluatePolicySummary(
    string Policy,
    int Total,
    int Passed,
    int Failed,
    long AverageElapsedMs);
