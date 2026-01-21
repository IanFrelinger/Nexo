using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.Infrastructure.Execution;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Fetches an SRTM tile via <see cref="IElevationProvider"/>.
/// Demonstrates deterministic vs agentic paths (agentic can be forced to fail to prove fallback).
/// </summary>
public sealed class GeoTerrainFetchSrtmTileBrick : Brick
{
    private readonly IElevationProvider _provider;
    private readonly IProviderFactory _llm;
    private readonly ILogger<GeoTerrainFetchSrtmTileBrick> _logger;

    public GeoTerrainFetchSrtmTileBrick(
        IElevationProvider provider,
        IProviderFactory llm,
        ILogger<GeoTerrainFetchSrtmTileBrick> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.fetch.srtm-tile";
        Name = "Fetch SRTM Tile";
        Version = "0.1.0";
        Icon = "🗺️";
        Category = BrickCategory.Input;
        Description = "Fetches a 1x1 degree SRTM .hgt tile and returns raw bytes + summary metadata.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("tile", "string", "Tile id like N37W122 or a filename like N37W122.hgt"),
                new BrickInputDefinition("forceAgenticFail", "bool", "If true, agentic path throws (for fallback demos)", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("tileId", "string", "Canonical tile id"),
                new BrickOutputDefinition("hgtBytes", "byte[]", "Raw .hgt bytes"),
                new BrickOutputDefinition("size", "int", "Tile dimension (e.g. 1201/3601)"),
                new BrickOutputDefinition("minMeters", "short", "Minimum elevation sample"),
                new BrickOutputDefinition("maxMeters", "short", "Maximum elevation sample"),
                new BrickOutputDefinition("noDataSamples", "int", "NoData sample count")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "provider",
                Name = "Provider (Deterministic)",
                Description = "Fetch via configured IElevationProvider",
                Executor = "IElevationProvider",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "depends on provider",
                    ResourceUsage = ResourceUsage.Low
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "provider+llm",
                Name = "Provider + LLM (Agentic)",
                Description = "Optionally uses LLM to normalize tile hints before fetching",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Normalize SRTM tile identifiers. Output JSON {\"tile\":\"N00E000\"}.",
                    Temperature = 0.0,
                    MaxTokens = 200
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
        var tileText = input.Get<string>("tile");
        if (!SrtmTileId.TryParse(tileText, out var tileId))
            throw new InvalidOperationException($"Could not parse SRTM tile id from '{tileText}'.");

        var tile = await _provider.GetSrtmTileAsync(tileId, ct);
        return new BrickOutput
        {
            ["tileId"] = tile.TileId.ToString(),
            ["hgtBytes"] = tile.HgtBytes,
            ["size"] = tile.Size,
            ["minMeters"] = tile.MinMeters,
            ["maxMeters"] = tile.MaxMeters,
            ["noDataSamples"] = tile.NoDataSamples,
            Summary = $"Fetched {tile.TileId} ({tile.Size}x{tile.Size})"
        };
    }

    private async Task<BrickOutput> ExecuteAgenticAsync(BrickInput input, IExecutionContext context, CancellationToken ct)
    {
        var tileText = input.Get<string>("tile");
        var forceFail = input.Get("forceAgenticFail", false);
        if (forceFail)
            throw new InvalidOperationException("Forced agentic failure (demo).");

        // Optional: ask the LLM to normalize a tile hint. Offline providers return {} so we treat it as best-effort.
        var normalized = tileText;
        try
        {
            var resp = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                $"Input: {tileText}\nReturn canonical SRTM tile id JSON.",
                Implementations.Agentic.LLMConfig,
                ct);

            if (!string.IsNullOrWhiteSpace(resp) && resp.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(resp);
                if (doc.RootElement.TryGetProperty("tile", out var tileProp))
                {
                    var t = tileProp.GetString();
                    if (!string.IsNullOrWhiteSpace(t)) normalized = t!;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LLM normalization failed; proceeding with raw tile input.");
        }

        if (!SrtmTileId.TryParse(normalized, out var tileId))
        {
            // Fall back to original input parse attempt.
            if (!SrtmTileId.TryParse(tileText, out tileId))
                throw new InvalidOperationException($"Could not parse SRTM tile id from '{tileText}'.");
        }

        var tile = await _provider.GetSrtmTileAsync(tileId, ct);
        return new BrickOutput
        {
            ["tileId"] = tile.TileId.ToString(),
            ["hgtBytes"] = tile.HgtBytes,
            ["size"] = tile.Size,
            ["minMeters"] = tile.MinMeters,
            ["maxMeters"] = tile.MaxMeters,
            ["noDataSamples"] = tile.NoDataSamples,
            Summary = $"Fetched {tile.TileId} (agentic path)"
        };
    }
}

