using System.Text;
using System.Text.Json;
using Ashlar.CLI.Runtime;
using Ashlar.Orchestration.Models;

namespace Ashlar.CLI.Commands.Workflow;

internal sealed record CandidateAllocationStat(
    string CandidateId,
    int Runs,
    int Successes,
    double SuccessRate,
    long AverageLatencyMs,
    int ObjectiveScore,
    bool Synthesized);
