using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Serializes scenery instances to JSON deterministically (no filesystem I/O).
/// </summary>
public sealed class GeoTerrainInstancesJsonBrick : Brick
{
    private readonly ILogger<GeoTerrainInstancesJsonBrick> _logger;

    public GeoTerrainInstancesJsonBrick(ILogger<GeoTerrainInstancesJsonBrick> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.export.instances-json";
        Name = "Instances Export (JSON Text)";
        Version = "0.1.0";
        Icon = "📦";
        Category = BrickCategory.Output;
        Description = "Serializes scenery instances to JSON text.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("instances", "SceneryInstance[]", "Instances")
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
                Name = "JSON Writer",
                Description = "Deterministic System.Text.Json serializer",
                Executor = "System.Text.Json",
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
        if (implementation != ImplementationType.Deterministic)
            _logger.LogDebug("Instances-json brick forcing deterministic implementation");

        var instances = input.Get<SceneryInstance[]>("instances");

        var payload = new
        {
            kind = "sceneryInstances",
            units = "meters",
            instances = instances.Select(i => new
            {
                kind = i.Kind,
                position = new { x = i.Position.X, y = i.Position.Y, z = i.Position.Z },
                yawDegrees = i.YawDegrees,
                uniformScale = i.UniformScale
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(payload);
        return Task.FromResult(new BrickOutput
        {
            ["jsonText"] = json,
            Summary = $"JSON bytes={json.Length}"
        });
    }
}

