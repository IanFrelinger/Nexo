using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.Infrastructure.Execution;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Fetches all SRTM tiles that cover a given <see cref="GeoBounds"/> via <see cref="IElevationProvider"/>.
/// </summary>
public sealed class GeoTerrainFetchSrtmTilesBrick : Brick
{
    private readonly IElevationProvider _provider;
    private readonly IProviderFactory _llm;
    private readonly ILogger<GeoTerrainFetchSrtmTilesBrick> _logger;

    public GeoTerrainFetchSrtmTilesBrick(
        IElevationProvider provider,
        IProviderFactory llm,
        ILogger<GeoTerrainFetchSrtmTilesBrick> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.fetch.srtm-tiles";
        Name = "Fetch SRTM Tiles (Bounds)";
        Version = "0.1.0";
        Icon = "🧩";
        Category = BrickCategory.Input;
        Description = "Fetches all 1x1 degree SRTM tiles that cover the given GeoBounds.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("bounds", "GeoBounds", "Bounds to cover"),
                new BrickInputDefinition("forceAgenticFail", "bool", "If true, agentic path throws (for fallback demos)", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("tiles", "ElevationTile[]", "Fetched tiles"),
                new BrickOutputDefinition("tileCount", "int", "Number of tiles")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "provider",
                Name = "Provider (Deterministic)",
                Description = "Enumerate tile IDs deterministically, then fetch via IElevationProvider",
                Executor = "IElevationProvider+SrtmTileCoverage",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "depends on provider",
                    ResourceUsage = ResourceUsage.Medium
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "provider+llm",
                Name = "Provider + LLM (Agentic)",
                Description = "Optionally uses LLM to validate/tune bounds before selecting tiles",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Given bounds, respond JSON {\"ok\":true}. Do not change bounds.",
                    Temperature = 0.0,
                    MaxTokens = 120
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = false,
                    RequiresNetwork = true,
                    Latency = "< 2s offline; longer on real providers",
                    ResourceUsage = ResourceUsage.Low
                }
            }
        };

        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = [ImplementationType.Agentic, ImplementationType.Deterministic];
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return implementation switch
        {
            ImplementationType.Deterministic => await ExecuteDeterministicAsync(input, cancellationToken),
            ImplementationType.Agentic => await ExecuteAgenticAsync(input, context, cancellationToken),
            _ => throw new ArgumentException($"Unsupported implementation: {implementation}")
        };
    }

    private async Task<BrickOutput> ExecuteDeterministicAsync(BrickInput input, CancellationToken ct)
    {
        var bounds = input.Get<GeoBounds>("bounds");
        bounds.Validate();

        var tileIds = SrtmTileCoverage.TilesCovering(bounds);
        var tiles = new ElevationTile[tileIds.Count];

        for (var i = 0; i < tileIds.Count; i++)
        {
            tiles[i] = await _provider.GetSrtmTileAsync(tileIds[i], ct);
        }

        return new BrickOutput
        {
            ["tiles"] = tiles,
            ["tileCount"] = tiles.Length,
            Summary = $"Fetched tiles={tiles.Length}"
        };
    }

    private async Task<BrickOutput> ExecuteAgenticAsync(BrickInput input, IExecutionContext context, CancellationToken ct)
    {
        var forceFail = input.Get("forceAgenticFail", false);
        if (forceFail) throw new InvalidOperationException("Forced agentic failure (demo).");

        // Best-effort validation (do not modify bounds).
        try
        {
            var bounds = input.Get<GeoBounds>("bounds");
            var resp = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                $"Bounds: minLat={bounds.MinLatitude.Degrees}, minLon={bounds.MinLongitude.Degrees}, maxLat={bounds.MaxLatitude.Degrees}, maxLon={bounds.MaxLongitude.Degrees}",
                Implementations.Agentic.LLMConfig,
                ct);
            _ = resp; // ignored on purpose
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LLM bounds validation failed; proceeding deterministically.");
        }

        return await ExecuteDeterministicAsync(input, ct);
    }
}

