using System.Text;
using System.Text.Json;
using Ashlar.CLI.Runtime;
using Ashlar.Orchestration.Models;

namespace Ashlar.CLI.Commands.Workflow;

internal sealed record OptimizeCandidatePlan(
    string CandidateId,
    WorkflowLabRequestSpec Request,
    WorkflowLabCompositionSpec Composition,
    WorkflowLabModelProfileSpec Profile,
    IReadOnlyList<ScenarioPlan> Plans,
    bool Synthesized = false,
    string? SynthesisRationale = null);
