using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimePlanResult(
    bool Ok,
    string Summary,
    AdaptiveRuntimeExecutionPlan? Plan = null,
    SelfExtendWorkflowRuntimeSpec? WorkflowSpec = null,
    string? AdaptivePolicyReason = null);
