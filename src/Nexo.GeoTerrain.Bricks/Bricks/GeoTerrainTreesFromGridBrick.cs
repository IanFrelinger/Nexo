using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Places tree instances from an elevation grid deterministically.
/// </summary>
public sealed class GeoTerrainTreesFromGridBrick : Brick
{
    private readonly ILogger<GeoTerrainTreesFromGridBrick> _logger;

    public GeoTerrainTreesFromGridBrick(ILogger<GeoTerrainTreesFromGridBrick> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.trees.from-grid";
        Name = "Trees From Grid";
        Version = "0.1.0";
        Icon = "🌲";
        Category = BrickCategory.Generation;
        Description = "Places tree instances using deterministic sampling and terrain height.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("grid", "ElevationGrid", "Elevation grid"),
                new BrickInputDefinition("bounds", "GeoBounds", "Bounds (for metadata; placement uses grid extents)"),
                new BrickInputDefinition("treesPerSqKm", "float", "Tree density", required: false, defaultValue: 200.0f),
                new BrickInputDefinition("seed", "int", "Deterministic seed", required: false, defaultValue: 1337),
                new BrickInputDefinition("treatNoDataAsZero", "bool", "If true, NaN becomes 0m when sampling height", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("instances", "SceneryInstance[]", "Tree instances")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "scatter",
                Name = "Scatter (Deterministic)",
                Description = "Deterministic scatter over terrain extents",
                Executor = "TreePlacer",
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
        if (implementation != ImplementationType.Deterministic)
            _logger.LogDebug("Trees-from-grid brick forcing deterministic implementation");

        var grid = input.Get<ElevationGrid>("grid");
        var bounds = input.Get<GeoBounds>("bounds");
        bounds.Validate();

        var treesPerSqKm = input.Get("treesPerSqKm", 200.0f);
        var seed = input.Get("seed", 1337);
        var treatNoData = input.Get("treatNoDataAsZero", false);

        var instances = TreePlacer.PlaceTrees(grid, bounds, new TreePlacementOptions
        {
            TreesPerSquareKm = treesPerSqKm,
            Seed = seed,
            TreatNoDataAsZero = treatNoData
        });

        return Task.FromResult(new BrickOutput
        {
            ["instances"] = instances.ToArray(),
            Summary = $"Trees: {instances.Count} instance(s)"
        });
    }
}

