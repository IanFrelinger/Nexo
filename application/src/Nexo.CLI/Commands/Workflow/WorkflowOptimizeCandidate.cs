using System.Text;
using System.Text.Json;
using Nexo.CLI.Runtime;
using Nexo.Orchestration.Models;

namespace Nexo.CLI.Commands.Workflow;
internal sealed record WorkflowOptimizeCandidate(
    string CandidateId,
    string RunId,
    string RequestId,
    string CompositionId,
    string ModelProfileId,
    int TotalRuns,
    int Successes,
    int Failures,
    int Skipped,
    double SuccessRate,
    long AverageLatencyMs,
    long P95LatencyMs,
    double AverageScore,
    long AverageCpuTimeDeltaMs,
    long P95WorkingSetMb,
    long P95PrivateMemoryMb,
    long P95ManagedMemoryMb,
    long MaxThreadCount,
    string HardwareProfile,
    IReadOnlyList<string> Models,
    string AutoPullSummary,
    bool AutoPullOk,
    bool Synthesized = false,
    string? SynthesisRationale = null,
    int ObjectiveScore = 0);
