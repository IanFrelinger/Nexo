using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.GeoWorld;

namespace Nexo.GeoWorld.Bricks;

/// <summary>
/// Emits a world bundle manifest JSON string (deterministic).
/// </summary>
public sealed class GeoWorldManifestJsonBrick : Brick
{
    private readonly ILogger<GeoWorldManifestJsonBrick> _logger;

    public GeoWorldManifestJsonBrick(ILogger<GeoWorldManifestJsonBrick> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoworld.export.manifest-json";
        Name = "World Manifest Export (JSON Text)";
        Version = "0.1.0";
        Icon = "🧾";
        Category = BrickCategory.Output;
        Description = "Serializes a WorldBundleManifest to JSON.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("bounds", "GeoBounds", "World bounds"),
                new BrickInputDefinition("origin", "GeoPoint", "World origin"),
                new BrickInputDefinition("metersPerUnit", "float", "Meters per unit", required: false, defaultValue: 1.0f),
                new BrickInputDefinition("coordinateFrame", "WorldCoordinateFrame", "Coordinate frame/projection info", required: false),
                new BrickInputDefinition("artifacts", "WorldArtifact[]", "Artifacts list"),
                new BrickInputDefinition("providerProvenance", "Dictionary<string,string>", "Provider provenance", required: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("jsonText", "string", "JSON text")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "json",
                Name = "Manifest JSON",
                Description = "Deterministic JSON generator",
                Executor = "WorldBundleWriters",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "< 100ms",
                    ResourceUsage = ResourceUsage.Low
                }
            }
        };

        DefaultImplementation = ImplementationType.Deterministic;
        FallbackChain = [ImplementationType.Deterministic];
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var bounds = input.Get<GeoBounds>("bounds");
        var origin = input.Get<GeoPoint>("origin");
        var mpu = input.Get("metersPerUnit", 1.0f);
        var coordinateFrame = input.Get<WorldCoordinateFrame?>("coordinateFrame", null);
        var artifacts = input.Get<WorldArtifact[]>("artifacts");
        var provenance = input.Get<IReadOnlyDictionary<string, string>?>("providerProvenance", null);

        var manifest = new WorldBundleManifest
        {
            Version = "0.1.0",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Bounds = bounds,
            Origin = origin,
            MetersPerUnit = mpu,
            CoordinateFrame = coordinateFrame,
            Artifacts = artifacts,
            ProviderProvenance = provenance
        };

        var json = WorldBundleWriters.WriteManifest(manifest);
        _logger.LogInformation("manifest.json generated: chars={Chars}", json.Length);

        return Task.FromResult(new BrickOutput
        {
            ["jsonText"] = json,
            Summary = $"manifest.json chars={json.Length}"
        });
    }
}

