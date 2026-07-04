using System.Text;
using System.Text.Json;
using Nexo.CLI.Runtime;
using Nexo.Orchestration.Models;

namespace Nexo.CLI.Commands.Workflow;

internal sealed record ScenarioPlan(
    WorkflowLabRequestSpec Request,
    WorkflowLabCompositionSpec Composition,
    WorkflowLabModelProfileSpec Profile,
    int Iteration);
