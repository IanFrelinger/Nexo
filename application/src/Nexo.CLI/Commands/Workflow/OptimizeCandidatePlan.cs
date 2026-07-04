using System.Text;
using System.Text.Json;
using Nexo.CLI.Runtime;
using Nexo.Orchestration.Models;

namespace Nexo.CLI.Commands.Workflow;

internal sealed record OptimizeCandidatePlan(
    string CandidateId,
    WorkflowLabRequestSpec Request,
    WorkflowLabCompositionSpec Composition,
    WorkflowLabModelProfileSpec Profile,
    IReadOnlyList<ScenarioPlan> Plans,
    bool Synthesized = false,
    string? SynthesisRationale = null);
