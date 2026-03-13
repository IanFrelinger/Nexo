namespace Nexo.CLI.Runtime;

/// <summary>
/// Optional user/runtime context that can steer adaptive execution.
/// </summary>
public sealed record AdaptiveRuntimeManifest
{
    /// <summary>
    /// Domain packs relevant to the user objective (e.g. personal, unity, ui).
    /// </summary>
    public string[] DomainPacks { get; init; } = Array.Empty<string>();

    /// <summary>
    /// User preferences that may influence generated behavior and UX.
    /// </summary>
    public Dictionary<string, string> Preferences { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// UI/runtime capabilities expected by the user or target surface.
    /// </summary>
    public string[] UiCapabilities { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Optional QA policy profile override: demo | prod | research.
    /// </summary>
    public string? QaPolicyProfile { get; init; }

    public static AdaptiveRuntimeManifest Default() => new();
}

public sealed record AdaptiveRuntimeExecutionPlan
{
    public string BootstrapProfile { get; init; } = "self-extend-functional";
    public string QaPolicyProfile { get; init; } = "demo";
    public string Focus { get; init; } = "functional";
    public int MaxIterations { get; init; } = 2;
    public bool StopOnFirstPass { get; init; } = true;
    public bool RunFunctionalQa { get; init; } = true;
    public bool RunAestheticQa { get; init; } = false;
    public bool RunVisualQa { get; init; } = false;
    public string VisualQaFallbackPolicy { get; init; } = "degrade";
    public bool RequirePreflight { get; init; } = true;
    public string[] AgentPhases { get; init; } = ["planner", "builder", "qa-functional"];
    public string[] Reasons { get; init; } = Array.Empty<string>();
}
