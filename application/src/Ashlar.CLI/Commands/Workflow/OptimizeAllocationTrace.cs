using System.Text;
using System.Text.Json;
using Ashlar.CLI.Runtime;
using Ashlar.Orchestration.Models;

namespace Ashlar.CLI.Commands.Workflow;

internal sealed record OptimizeAllocationTrace(
    int RunIndex,
    string CandidateId,
    string TargetId,
    bool Success,
    long LatencyMs,
    string Reason);
