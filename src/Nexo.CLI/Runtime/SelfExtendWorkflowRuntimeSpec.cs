namespace Nexo.CLI.Runtime;

/// <summary>
/// Runtime-configurable workflow settings for iterative self-extend runs.
/// Mirrors the runtime-spec pattern used by orchestration so behavior can be changed at invocation time.
/// </summary>
public sealed record SelfExtendWorkflowRuntimeSpec
{
    public SelfExtendWorkflowSpec Workflow { get; init; } = new();

    public static SelfExtendWorkflowRuntimeSpec Default() => new();
}

public sealed record SelfExtendWorkflowSpec
{
    /// <summary>Maximum number of improve/retry iterations.</summary>
    public int MaxIterations { get; init; } = 3;

    /// <summary>If true, stop after the first passing QA gate set.</summary>
    public bool StopOnFirstPass { get; init; } = true;

    /// <summary>"balanced", "functional", or "aesthetic".</summary>
    public string Focus { get; init; } = "balanced";

    /// <summary>If true, execute phase objectives with role-specific agent names.</summary>
    public bool EnableMultiAgentPhases { get; init; } = true;

    /// <summary>
    /// Ordered phase names used within each iteration.
    /// Typical values: planner, builder, qa-functional, qa-aesthetic.
    /// </summary>
    public string[] AgentPhases { get; init; } = ["planner", "builder", "qa-functional", "qa-aesthetic"];

    /// <summary>Run generated functional tests as part of QA gates.</summary>
    public bool RunFunctionalQa { get; init; } = true;

    /// <summary>Run UI/domain/aesthetic checks when applicable.</summary>
    public bool RunAestheticQa { get; init; } = true;

    /// <summary>Run visual validation tests via `nexo test --visual` (requires visual model setup).</summary>
    public bool RunVisualQa { get; init; } = false;

    /// <summary>Filter for generated functional test validation.</summary>
    public string FunctionalTestFilter { get; init; } = "SelfExtendGenerated";

    /// <summary>Filter for aesthetic-oriented checks from internal test suite.</summary>
    public string AestheticTestFilter { get; init; } = "UiDomainKnowledgeRetentionTests";

    /// <summary>
    /// Optional smoke project path for generated UI app checks.
    /// If the project exists, command runs `dotnet run --project ... -- --smoke`.
    /// </summary>
    public string UiSmokeProjectPath { get; init; } =
        "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/Nexo.Ui.AvaloniaHost.csproj";
}
