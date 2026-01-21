using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Converts a mesh to OBJ text deterministically (no filesystem I/O).
/// </summary>
public sealed class GeoTerrainObjFromMeshBrick : Brick
{
    private readonly ILogger<GeoTerrainObjFromMeshBrick> _logger;

    public GeoTerrainObjFromMeshBrick(ILogger<GeoTerrainObjFromMeshBrick> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.export.obj-text";
        Name = "OBJ Export (Text)";
        Version = "0.1.0";
        Icon = "📄";
        Category = BrickCategory.Output;
        Description = "Serializes a mesh to OBJ text (caller can write using a tool).";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("mesh", "MeshData", "Mesh to export")
            ],
            Outputs =
            [
                new BrickOutputDefinition("obj", "string", "OBJ text")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "obj-writer",
                Name = "OBJ Writer",
                Description = "Deterministic serializer",
                Executor = "ObjMeshWriter",
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
            _logger.LogDebug("OBJ brick forcing deterministic implementation");

        var mesh = input.Get<MeshData>("mesh");
        var obj = ObjMeshWriter.Write(mesh);

        return Task.FromResult(new BrickOutput
        {
            ["obj"] = obj,
            Summary = $"OBJ bytes={obj.Length}"
        });
    }
}

