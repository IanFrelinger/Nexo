namespace Ashlar.Manifest;

/// <summary>
/// The project contract — <c>ashlar.yaml</c>. Describes what the application IS.
///
/// <para>Agents may propose changes to this document. That is precisely why it carries no
/// sandbox root, no self-extension mode, and no never-list: anything an application can
/// propose an edit to cannot also be the thing that constrains it. Those live in
/// <see cref="AshlarPolicy"/>, which the application cannot read or reach.</para>
/// </summary>
public sealed record AshlarManifest
{
    /// <summary>Schema version. Only <c>ashlar/v1</c> is accepted.</summary>
    public string ApiVersion { get; init; } = string.Empty;

    /// <summary>Document kind. Must be <c>Application</c>.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Project identity.</summary>
    public ManifestMetadata Metadata { get; init; } = new();

    /// <summary>Agents composing this application.</summary>
    public List<ManifestAgent> Agents { get; init; } = [];

    /// <summary>Certified bricks the application depends on.</summary>
    public List<ManifestBrick> Bricks { get; init; } = [];

    /// <summary>Deployment targets, each naming a platform and profile.</summary>
    public List<ManifestTarget> Targets { get; init; } = [];
}

/// <summary>Project identity.</summary>
public sealed record ManifestMetadata
{
    /// <summary>Project name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Project version.</summary>
    public string Version { get; init; } = string.Empty;
}

/// <summary>The model an agent runs on.</summary>
public sealed record ManifestModel
{
    /// <summary>Provider name, e.g. <c>ollama</c>, <c>openai</c>, <c>azure</c>, or
    /// <c>mock</c> (runs offline with canned responses — the zero-setup default).</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>Model identifier within the provider, e.g. <c>llama3</c>. Optional; the
    /// provider's default is used when absent.</summary>
    public string? Id { get; init; }
}

/// <summary>
/// An agent in the composition.
/// </summary>
public sealed record ManifestAgent
{
    /// <summary>Agent identifier, unique within the manifest.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The model this agent runs on. Optional; <c>ashlar run</c> falls back to the
    /// zero-setup <c>mock</c> provider when absent, and says so.</summary>
    public ManifestModel? Model { get; init; }

    /// <summary>
    /// Tools this agent may call, BY NAME ONLY. An agent references a tool it has been
    /// granted; it cannot define one, and it never carries a filesystem root — the sandbox
    /// root comes from the policy, resolved by the host.
    /// </summary>
    public List<string> Tools { get; init; } = [];

    /// <summary>Gates this agent's output must clear.</summary>
    public List<string> Gates { get; init; } = [];
}

/// <summary>
/// A brick dependency.
/// </summary>
public sealed record ManifestBrick
{
    /// <summary>Brick identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Brick version.</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// The signature the gate RESOLVED when it last verified this brick — a record of a
    /// finding, not an assertion of trust. Verification re-derives it, so a value written
    /// here by an agent changes nothing.
    /// </summary>
    public string? Certified { get; init; }
}

/// <summary>A deployment target.</summary>
public sealed record ManifestTarget
{
    /// <summary>Target name, referenced by <c>ashlar deploy &lt;name&gt;</c>.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Platform identifier, e.g. <c>aws.fargate</c> or <c>native.edge</c>.</summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>Composition profile, e.g. <c>full</c> or <c>air-gapped</c>.</summary>
    public string Profile { get; init; } = string.Empty;
}
