using System.Text;
using System.Text.Json;
using Nexo.CLI.Runtime;
using Nexo.Orchestration.Models;

namespace Nexo.CLI.Commands.Workflow;

internal sealed record TargetAllocationStat(
    string TargetId,
    int Runs,
    int Successes,
    double SuccessRate,
    long AverageLatencyMs);
