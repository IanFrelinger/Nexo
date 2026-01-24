using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Projection;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Stitches multiple SRTM tiles into a single <see cref="ElevationGrid"/> deterministically.
/// </summary>
public sealed class GeoTerrainGridFromTilesBrick : Brick
{
    private readonly ILogger<GeoTerrainGridFromTilesBrick> _logger;
    private readonly ICoordinateProjector _projector;

    public GeoTerrainGridFromTilesBrick(ILogger<GeoTerrainGridFromTilesBrick> logger)
        : this(EquirectangularProjector.Instance, logger)
    {
    }

    public GeoTerrainGridFromTilesBrick(ICoordinateProjector projector, ILogger<GeoTerrainGridFromTilesBrick> logger)
    {
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.grid.from-tiles";
        Name = "Grid From Tiles";
        Version = "0.1.0";
        Icon = "🧱";
        Category = BrickCategory.Transform;
        Description = "Stitches multiple SRTM tiles into a single elevation grid.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("tiles", "ElevationTile[]", "Tiles to stitch")
            ],
            Outputs =
            [
                new BrickOutputDefinition("grid", "ElevationGrid", "Stitched grid")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "mosaic",
                Name = "Mosaic Builder",
                Description = "Deterministic stitch into a single grid",
                Executor = "SrtmMosaicBuilder",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "< 1s",
                    ResourceUsage = ResourceUsage.Medium
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
            _logger.LogDebug("Grid-from-tiles brick forcing deterministic implementation");

        var tiles = input.Get<ElevationTile[]>("tiles");
        if (tiles.Length == 0) throw new InvalidOperationException("No tiles were provided.");

        var dict = new Dictionary<SrtmTileId, byte[]>(tiles.Length);
        for (var i = 0; i < tiles.Length; i++)
        {
            dict[tiles[i].TileId] = tiles[i].HgtBytes;
        }

        var grid = SrtmMosaicBuilder.Build(dict, _projector);
        return Task.FromResult(new BrickOutput
        {
            ["grid"] = grid,
            Summary = $"Grid: {grid.Width}x{grid.Height}"
        });
    }
}

