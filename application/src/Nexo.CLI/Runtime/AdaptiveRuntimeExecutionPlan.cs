namespace Nexo.CLI.Runtime;

/// <summary>Resolved adaptive runtime execution plan derived from policy and manifest inputs.</summary>
public sealed record AdaptiveRuntimeExecutionPlan
{
    /// <summary>Bootstrap profile applied before self-extend execution.</summary>
    public string BootstrapProfile { get; init; } = "self-extend-functional";

    /// <summary>QA policy profile used to resolve functional, aesthetic, and visual checks.</summary>
    public string QaPolicyProfile { get; init; } = "demo";

    /// <summary>Primary execution focus lane (e.g. functional, aesthetic, visual).</summary>
    public string Focus { get; init; } = "functional";

    /// <summary>Maximum self-extend iterations allowed for this run.</summary>
    public int MaxIterations { get; init; } = 2;

    /// <summary>When true, stop after the first passing QA iteration.</summary>
    public bool StopOnFirstPass { get; init; } = true;

    /// <summary>When true, run functional QA gates after generation.</summary>
    public bool RunFunctionalQa { get; init; } = true;

    /// <summary>When true, run aesthetic QA gates after generation.</summary>
    public bool RunAestheticQa { get; init; } = false;

    /// <summary>When true, run visual QA gates after generation.</summary>
    public bool RunVisualQa { get; init; } = false;

    /// <summary>Fallback policy when visual QA prerequisites are unavailable.</summary>
    public string VisualQaFallbackPolicy { get; init; } = "degrade";

    /// <summary>When true, run bootstrap preflight before self-extend.</summary>
    public bool RequirePreflight { get; init; } = true;

    /// <summary>Ordered agent phases executed during the adaptive loop.</summary>
    public string[] AgentPhases { get; init; } = ["planner", "builder", "qa-functional"];

    /// <summary>Human-readable reasons explaining plan resolution decisions.</summary>
    public string[] Reasons { get; init; } = Array.Empty<string>();
}
