#pragma warning disable RS0016

namespace Nexo.Core.Domain.Bricks.Ports;

/// <summary>
/// Tunables for generation/repair bounds. Domain-neutral.
/// </summary>
public sealed class GenerationTunables
{
    /// <summary>Max draft→validate cycles.</summary>
    public int MaxRepairAttempts { get; init; } = 3;

    /// <summary>When true, prefer deterministic drafter before the model path.</summary>
    public bool PreferDeterministic { get; init; } = true;
}

/// <summary>
/// Capability flags for a profile. Opaque to the fixed engine — profiles set
/// what they support; the engine branches on these flags only.
/// </summary>
public sealed class AgentProfileCapabilities
{
    /// <summary>Profile can produce a draft without a model.</summary>
    public bool SupportsDeterministic { get; init; }

    /// <summary>Profile supplies a sandbox provider for verification.</summary>
    public bool SupportsSandbox { get; init; }

    /// <summary>Profile can deploy/install the artifact.</summary>
    public bool SupportsDeployment { get; init; }
}

/// <summary>
/// One registered target profile. Adding a language/domain is registering a
/// new profile — the fixed engine is never edited.
/// </summary>
public sealed class AgentProfile
{
    /// <summary>Stable target id used by <c>GenerativeArtifactBrick</c> input.</summary>
    public string TargetId { get; init; } = "";

    /// <summary>Primary drafter (model or templated).</summary>
    public IArtifactDrafter Drafter { get; init; } = default!;

    /// <summary>Optional deterministic path.</summary>
    public IDeterministicDrafter? DeterministicDrafter { get; init; }

    /// <summary>Post-draft validators (domain-neutral port; typically IPostValidator wrappers).</summary>
    public IReadOnlyList<object> Validators { get; init; } = Array.Empty<object>();

    /// <summary>Optional sandbox provider.</summary>
    public ISandboxProvider? Sandbox { get; init; }

    /// <summary>Optional deployment target.</summary>
    public IDeploymentTarget? Deployment { get; init; }

    /// <summary>Domain knowledge for prompts/rules.</summary>
    public DomainKnowledge Knowledge { get; init; } = new();

    /// <summary>LLM configuration for the agentic path.</summary>
    public LLMConfig Llm { get; init; } = new();

    /// <summary>Repair/generation tunables.</summary>
    public GenerationTunables Tunables { get; init; } = new();

    /// <summary>Capability flags.</summary>
    public AgentProfileCapabilities Capabilities { get; init; } = new();
}
