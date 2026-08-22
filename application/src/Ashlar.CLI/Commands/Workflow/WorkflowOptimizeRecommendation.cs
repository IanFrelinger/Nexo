using System.Text;
using System.Text.Json;
using Ashlar.CLI.Runtime;
using Ashlar.Orchestration.Models;

namespace Ashlar.CLI.Commands.Workflow;

internal sealed record WorkflowOptimizeRecommendation(
    string Kind,
    string Action,
    string CandidateId,
    string Rationale);
