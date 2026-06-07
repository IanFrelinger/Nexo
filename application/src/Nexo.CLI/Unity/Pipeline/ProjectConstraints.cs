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
    public CodeConstraints Code { get; init; } = new();
    public WeaponConstraints Weapons { get; init; } = new();
    public MovementConstraints Movement { get; init; } = new();
    public NetworkingConstraints Networking { get; init; } = new();
    public AudioConstraints Audio { get; init; } = new();
    public AnimationConstraints Animation { get; init; } = new();
    public AestheticConstraints Aesthetics { get; init; } = new();

    public static ProjectConstraints LoadFromFile(string projectRoot)
    {
        var path = Path.Combine(projectRoot, ".nexo", "constraints.json");
        if (!File.Exists(path))
            return new ProjectConstraints();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ProjectConstraints>(json, SerializerOptions) ?? new ProjectConstraints();
    }

    public void SaveToFile(string projectRoot)
    {
        var dir = Path.Combine(projectRoot, ".nexo");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "constraints.json");
        File.WriteAllText(path, JsonSerializer.Serialize(this, SerializerOptions));
    }

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

public sealed record CodeConstraints
{
    public string? NamespacePrefix { get; init; }
    public int MaxFileLines { get; init; }
    public bool RequireInterfaces { get; init; }
    public bool RequireSerializeField { get; init; } = true;
    public IReadOnlyList<string> BannedPatterns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredUsings { get; init; } = Array.Empty<string>();
    public string? TestCoverage { get; init; }
}

public sealed record WeaponConstraints
{
    public double[]? DamageRange { get; init; }
    public double[]? FireRateRange { get; init; }
    public int MaxMagazineSize { get; init; }
    public bool RequireReloadMechanic { get; init; }
}

public sealed record MovementConstraints
{
    public double MaxSpeed { get; init; }
    public bool RequireGroundCheck { get; init; }
    public bool RequireCoyoteTime { get; init; }
}

public sealed record NetworkingConstraints
{
    public string? Authority { get; init; }
    public int MaxSyncedVariablesPerObject { get; init; }
    public bool RequireServerRpc { get; init; }
}

public sealed record AudioConstraints
{
    public bool RequireVariations { get; init; }
    public int MinVariationsPerEvent { get; init; } = 2;
    public bool RequireSpatialBlend { get; init; }
    public int MaxSimultaneous { get; init; } = 32;
}

public sealed record AnimationConstraints
{
    public double MaxTransitionDuration { get; init; } = 0.3;
    public bool RequireExitTime { get; init; }
}

public sealed record AestheticConstraints
{
    public string? DefaultPack { get; init; }
    public int MaxTriBudgetPerObject { get; init; }
    public int MaxDrawCalls { get; init; }
}
