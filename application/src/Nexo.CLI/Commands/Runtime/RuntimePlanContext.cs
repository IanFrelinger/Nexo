using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimePlanContext(
    string Goal,
    AdaptiveRuntimeManifest Manifest,
    AdaptiveRuntimeExecutionPlan Plan,
    SelfExtendWorkflowRuntimeSpec WorkflowSpec,
    string RequestedQaPolicy,
    string? AdaptivePolicyReason);
