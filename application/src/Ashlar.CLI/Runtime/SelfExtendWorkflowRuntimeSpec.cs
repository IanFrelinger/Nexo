namespace Ashlar.CLI.Runtime;

/// <summary>
/// Runtime-configurable workflow settings for iterative self-extend runs.
/// Mirrors the runtime-spec pattern used by orchestration so behavior can be changed at invocation time.
/// </summary>
public sealed record SelfExtendWorkflowRuntimeSpec
{
    /// <summary>Workflow iteration settings for self-extend runs.</summary>
    public SelfExtendWorkflowSpec Workflow { get; init; } = new();

    /// <summary>Returns the built-in default self-extend workflow runtime specification.</summary>
    public static SelfExtendWorkflowRuntimeSpec Default() => new();
}
