using System.Text;
using System.Text.Json;
using Nexo.CLI.Runtime;
using Nexo.Orchestration.Models;

namespace Nexo.CLI.Commands.Workflow;

internal sealed record OptimizeAllocationTrace(
    int RunIndex,
    string CandidateId,
    string TargetId,
    bool Success,
    long LatencyMs,
    string Reason);
