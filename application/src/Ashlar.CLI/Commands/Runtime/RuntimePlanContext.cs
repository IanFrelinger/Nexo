using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimePlanContext(
    string Goal,
    AdaptiveRuntimeManifest Manifest,
    AdaptiveRuntimeExecutionPlan Plan,
    SelfExtendWorkflowRuntimeSpec WorkflowSpec,
    string RequestedQaPolicy,
    string? AdaptivePolicyReason);
