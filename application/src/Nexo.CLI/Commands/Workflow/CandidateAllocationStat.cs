using System.Text;
using System.Text.Json;
using Nexo.CLI.Runtime;
using Nexo.Orchestration.Models;

namespace Nexo.CLI.Commands.Workflow;

internal sealed record CandidateAllocationStat(
    string CandidateId,
    int Runs,
    int Successes,
    double SuccessRate,
    long AverageLatencyMs,
    int ObjectiveScore,
    bool Synthesized);
