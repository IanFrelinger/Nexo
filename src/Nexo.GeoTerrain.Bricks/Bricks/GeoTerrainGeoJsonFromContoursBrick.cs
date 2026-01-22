using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Converts contour polylines to GeoJSON text deterministically (no filesystem I/O).
/// </summary>
public sealed class GeoTerrainGeoJsonFromContoursBrick : Brick
{
    private readonly ILogger<GeoTerrainGeoJsonFromContoursBrick> _logger;

    public GeoTerrainGeoJsonFromContoursBrick(ILogger<GeoTerrainGeoJsonFromContoursBrick> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.export.contours-geojson";
        Name = "Contours Export (GeoJSON Text)";
        Version = "0.1.0";
        Icon = "🗺️";
        Category = BrickCategory.Output;
        Description = "Serializes contour polylines to GeoJSON text.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("grid", "ElevationGrid", "Elevation grid (for GeoBounds + mapping)"),
                new BrickInputDefinition("contours", "ContourLine[]", "Contour polylines"),
                new BrickInputDefinition("includeElevation", "bool", "Include elevation as 3rd coordinate", required: false, defaultValue: true)
            ],
            Outputs =
            [
                new BrickOutputDefinition("geojson", "string", "GeoJSON FeatureCollection")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "geojson-writer",
                Name = "GeoJSON Writer",
                Description = "Deterministic serializer",
                Executor = "GeoJsonContoursWriter",
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
            _logger.LogDebug("GeoJSON contours brick forcing deterministic implementation");

        var grid = input.Get<ElevationGrid>("grid");
        var contours = input.Get<ContourLine[]>("contours");
        var includeElevation = input.Get("includeElevation", true);

        var geojson = GeoJsonContoursWriter.Write(grid, contours, includeElevation);

        return Task.FromResult(new BrickOutput
        {
            ["geojson"] = geojson,
            Summary = $"GeoJSON bytes={geojson.Length}"
        });
    }
}

