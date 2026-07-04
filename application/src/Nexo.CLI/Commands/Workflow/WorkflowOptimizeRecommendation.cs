using System.Text;
using System.Text.Json;
using Nexo.CLI.Runtime;
using Nexo.Orchestration.Models;

namespace Nexo.CLI.Commands.Workflow;

internal sealed record WorkflowOptimizeRecommendation(
    string Kind,
    string Action,
    string CandidateId,
    string Rationale);
