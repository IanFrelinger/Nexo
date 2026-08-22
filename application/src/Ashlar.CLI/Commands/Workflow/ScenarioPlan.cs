using System.Text;
using System.Text.Json;
using Ashlar.CLI.Runtime;
using Ashlar.Orchestration.Models;

namespace Ashlar.CLI.Commands.Workflow;

internal sealed record ScenarioPlan(
    WorkflowLabRequestSpec Request,
    WorkflowLabCompositionSpec Composition,
    WorkflowLabModelProfileSpec Profile,
    int Iteration);
