using System.Text;
using System.Text.Json;
using Ashlar.CLI.Runtime;
using Ashlar.Orchestration.Models;

namespace Ashlar.CLI.Commands.Workflow;

internal sealed record TargetAllocationStat(
    string TargetId,
    int Runs,
    int Successes,
    double SuccessRate,
    long AverageLatencyMs);
