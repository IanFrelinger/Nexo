namespace Nexo.CLI.Unity.Pipeline;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Project-level constraints that govern all generation. Loaded from
/// .nexo/constraints.json in the project root. Constraints are injected
/// into the LLM system prompt so generated code and assets satisfy them.
/// </summary>
public sealed record ProjectConstraints
{
    /// <summary>C# coding standards for generated scripts.</summary>
    public CodeConstraints Code { get; init; } = new();

    /// <summary>Combat tuning bounds for generated weapons.</summary>
    public WeaponConstraints Weapons { get; init; } = new();

    /// <summary>Character movement limits for generated controllers.</summary>
    public MovementConstraints Movement { get; init; } = new();

    /// <summary>Multiplayer synchronization rules.</summary>
    public NetworkingConstraints Networking { get; init; } = new();

    /// <summary>Audio variation and spatialization rules.</summary>
    public AudioConstraints Audio { get; init; } = new();

    /// <summary>Animator and state-machine rules.</summary>
    public AnimationConstraints Animation { get; init; } = new();

    /// <summary>Visual and performance budgets for generated assets.</summary>
    public AestheticConstraints Aesthetics { get; init; } = new();

    /// <summary>Loads constraints from <c>.nexo/constraints.json</c> under the project root.</summary>
    public static ProjectConstraints LoadFromFile(string projectRoot)
    {
        var path = Path.Combine(projectRoot, ".nexo", "constraints.json");
        if (!File.Exists(path))
            return new ProjectConstraints();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ProjectConstraints>(json, SerializerOptions) ?? new ProjectConstraints();
    }

    /// <summary>Persists constraints to <c>.nexo/constraints.json</c> under the project root.</summary>
    public void SaveToFile(string projectRoot)
    {
        var dir = Path.Combine(projectRoot, ".nexo");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "constraints.json");
        File.WriteAllText(path, JsonSerializer.Serialize(this, SerializerOptions));
    }

    /// <summary>Builds a bullet-list fragment suitable for LLM system prompts.</summary>
    public string ToPromptFragment()
    {
        var parts = new List<string>();
        if (Code.NamespacePrefix != null) parts.Add($"Use namespace prefix: {Code.NamespacePrefix}");
        if (Code.MaxFileLines > 0) parts.Add($"Max {Code.MaxFileLines} lines per file");
        if (Code.RequireInterfaces) parts.Add("Create interfaces for all public contracts");
        if (Code.BannedPatterns.Count > 0) parts.Add($"Never use: {string.Join(", ", Code.BannedPatterns)}");
        if (Code.TestCoverage != null) parts.Add($"Test coverage: {Code.TestCoverage}");
        if (Weapons.DamageRange is { Length: 2 }) parts.Add($"Weapon damage must be between {Weapons.DamageRange[0]} and {Weapons.DamageRange[1]}");
        if (Weapons.MaxMagazineSize > 0) parts.Add($"Max magazine size: {Weapons.MaxMagazineSize}");
        if (Movement.MaxSpeed > 0) parts.Add($"Max movement speed: {Movement.MaxSpeed}");
        if (Networking.Authority != null) parts.Add($"Network authority model: {Networking.Authority}");
        if (Audio.RequireVariations) parts.Add($"Audio: require at least {Audio.MinVariationsPerEvent} variations per event");
        if (Aesthetics.MaxTriBudgetPerObject > 0) parts.Add($"Max {Aesthetics.MaxTriBudgetPerObject} triangles per object");
        return parts.Count > 0 ? "\nProject constraints:\n- " + string.Join("\n- ", parts) : "";
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
