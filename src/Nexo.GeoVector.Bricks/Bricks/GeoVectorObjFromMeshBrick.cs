using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;

namespace Nexo.GeoVector.Bricks;

/// <summary>
/// Exports OBJ text from a mesh (deterministic).
/// </summary>
public sealed class GeoVectorObjFromMeshBrick : Brick
{
    private readonly ILogger<GeoVectorObjFromMeshBrick> _logger;

    public GeoVectorObjFromMeshBrick(ILogger<GeoVectorObjFromMeshBrick> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geovector.export.obj-text";
        Name = "OBJ From Mesh";
        Version = "0.1.0";
        Icon = "📦";
        Category = BrickCategory.Output;
        Description = "Writes OBJ text for a mesh (positions + normals).";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("mesh", "MeshData", "Input mesh")
            ],
            Outputs =
            [
                new BrickOutputDefinition("objText", "string", "OBJ text")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "obj",
                Name = "OBJ Writer",
                Description = "Deterministic in-memory OBJ writer",
                Executor = "ObjMeshWriter",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "< 1s",
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
        var mesh = input.Get<MeshData>("mesh");
        var obj = ObjMeshWriter.Write(mesh);
        _logger.LogInformation("OBJ generated: chars={Chars}", obj.Length);

        return Task.FromResult(new BrickOutput
        {
            ["objText"] = obj,
            Summary = $"OBJ chars={obj.Length}"
        });
    }
}

