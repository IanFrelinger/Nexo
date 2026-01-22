using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.Infrastructure.Execution;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Generates contour polylines from raw .hgt bytes (deterministic),
/// with an optional agentic path that can tune parameters.
/// </summary>
public sealed class GeoTerrainContoursFromHgtBrick : Brick
{
    private readonly IProviderFactory _llm;
    private readonly ILogger<GeoTerrainContoursFromHgtBrick> _logger;

    public GeoTerrainContoursFromHgtBrick(IProviderFactory llm, ILogger<GeoTerrainContoursFromHgtBrick> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.contours.from-hgt";
        Name = "Contours From HGT";
        Version = "0.1.0";
        Icon = "🧭";
        Category = BrickCategory.Generation;
        Description = "Parses SRTM .hgt bytes and generates contour polylines (marching squares).";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("tileId", "string", "Canonical tile id like N00E000 (optional)", required: false),
                new BrickInputDefinition("hgtBytes", "byte[]", "Raw .hgt bytes"),
                new BrickInputDefinition("intervalMeters", "double", "Contour interval in meters", required: false, defaultValue: 10.0),
                new BrickInputDefinition("minElevationMeters", "double", "Optional min contour level", required: false),
                new BrickInputDefinition("maxElevationMeters", "double", "Optional max contour level", required: false),
                new BrickInputDefinition("verticalScale", "float", "Vertical scale multiplier", required: false, defaultValue: 1.0f),
                new BrickInputDefinition("treatNoDataAsZero", "bool", "If true, NaN becomes 0m", required: false, defaultValue: false),
                new BrickInputDefinition("forceAgenticFail", "bool", "If true, agentic path throws (for fallback demos)", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("grid", "ElevationGrid", "Parsed elevation grid"),
                new BrickOutputDefinition("contours", "ContourLine[]", "Generated contour polylines")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "marching-squares",
                Name = "Marching Squares (Deterministic)",
                Description = "Deterministic contour extraction",
                Executor = "ContourGenerator",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "< 1s",
                    ResourceUsage = ResourceUsage.Medium
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "tuned-marching-squares",
                Name = "Tuned Contours (Agentic)",
                Description = "Uses an LLM to suggest interval/min/max, then runs deterministic generator",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Given elevation min/max, output JSON {\"intervalMeters\":10.0,\"minElevationMeters\":null,\"maxElevationMeters\":null}.",
                    Temperature = 0.0,
                    MaxTokens = 200
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = false,
                    RequiresNetwork = true,
                    Latency = "< 2s offline; longer on real providers",
                    ResourceUsage = ResourceUsage.Medium
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

    private Task<BrickOutput> ExecuteDeterministicAsync(BrickInput input, CancellationToken ct)
    {
        var bytes = input.Get<byte[]>("hgtBytes");
        var tileIdText = input.Get<string?>("tileId", null);

        var interval = input.Get("intervalMeters", 10.0);
        var min = input.Get<double?>("minElevationMeters", null);
        var max = input.Get<double?>("maxElevationMeters", null);
        var verticalScale = input.Get("verticalScale", 1.0f);
        var treatNoData = input.Get("treatNoDataAsZero", false);

        var heights = SrtmHgtParser.ParseHeightsMeters(bytes, out var size);

        GeoBounds bounds;
        if (!string.IsNullOrWhiteSpace(tileIdText) && SrtmTileId.TryParse(tileIdText, out var tile))
        {
            bounds = tile.ToBounds();
        }
        else
        {
            bounds = new GeoBounds
            {
                MinLatitude = new Latitude(0),
                MaxLatitude = new Latitude(1),
                MinLongitude = new Longitude(0),
                MaxLongitude = new Longitude(1)
            };
        }

        // Same approximation as mesh/tooling: 1 degree lat ~111_320m; lon scales by cos(lat).
        var midLatRad = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) * 0.5 * (Math.PI / 180.0);
        var metersPerDegLat = 111_320.0;
        var metersPerDegLon = 111_320.0 * Math.Cos(midLatRad);
        var degPerSample = 1.0 / (size - 1);
        var spacing = new GridSpacing(
            metersX: metersPerDegLon * degPerSample,
            metersY: metersPerDegLat * degPerSample);

        var grid = new ElevationGrid(size, size, bounds, spacing, heights);
        var contours = ContourGenerator.Generate(grid, new ContourGenerationOptions
        {
            IntervalMeters = interval,
            MinElevationMeters = min,
            MaxElevationMeters = max,
            VerticalScale = verticalScale,
            TreatNoDataAsZero = treatNoData
        });

        return Task.FromResult(new BrickOutput
        {
            ["grid"] = grid,
            ["contours"] = contours.ToArray(),
            Summary = $"Contours: {contours.Count} polylines"
        });
    }

    private async Task<BrickOutput> ExecuteAgenticAsync(BrickInput input, IExecutionContext context, CancellationToken ct)
    {
        var forceFail = input.Get("forceAgenticFail", false);
        if (forceFail) throw new InvalidOperationException("Forced agentic failure (demo).");

        var bytes = input.Get<byte[]>("hgtBytes");
        var summary = SrtmHgtParser.Analyze(bytes);

        var interval = input.Get("intervalMeters", 10.0);
        var min = input.Get<double?>("minElevationMeters", null);
        var max = input.Get<double?>("maxElevationMeters", null);

        try
        {
            var resp = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                $"Tile size={summary.Size} min={summary.MinMeters} max={summary.MaxMeters} nodata={summary.NoDataSamples}",
                Implementations.Agentic.LLMConfig,
                ct);

            if (!string.IsNullOrWhiteSpace(resp) && resp.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(resp);
                if (doc.RootElement.TryGetProperty("intervalMeters", out var im) && im.TryGetDouble(out var v)) interval = v;
                if (doc.RootElement.TryGetProperty("minElevationMeters", out var mn) && mn.ValueKind == System.Text.Json.JsonValueKind.Number && mn.TryGetDouble(out var mnv)) min = mnv;
                if (doc.RootElement.TryGetProperty("maxElevationMeters", out var mx) && mx.ValueKind == System.Text.Json.JsonValueKind.Number && mx.TryGetDouble(out var mxv)) max = mxv;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LLM tuning failed; falling back to input defaults.");
        }

        input.Set("intervalMeters", interval);
        if (min.HasValue) input.Set("minElevationMeters", min.Value);
        if (max.HasValue) input.Set("maxElevationMeters", max.Value);
        return await ExecuteDeterministicAsync(input, ct);
    }
}

