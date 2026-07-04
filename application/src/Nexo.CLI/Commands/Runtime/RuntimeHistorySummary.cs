using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeHistorySummary(
    int Total,
    int Passed,
    int Failed,
    long AverageElapsedMs);
