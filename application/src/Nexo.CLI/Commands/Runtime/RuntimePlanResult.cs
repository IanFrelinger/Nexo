using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimePlanResult(
    bool Ok,
    string Summary,
    AdaptiveRuntimeExecutionPlan? Plan = null,
    SelfExtendWorkflowRuntimeSpec? WorkflowSpec = null,
    string? AdaptivePolicyReason = null);
