using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.Infrastructure.Execution;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Builds a mesh from raw .hgt bytes (deterministic), with an agentic path that can optionally tune parameters.
/// </summary>
public sealed class GeoTerrainMeshFromHgtBrick : Brick
{
    private readonly IProviderFactory _llm;
    private readonly ILogger<GeoTerrainMeshFromHgtBrick> _logger;

    public GeoTerrainMeshFromHgtBrick(IProviderFactory llm, ILogger<GeoTerrainMeshFromHgtBrick> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.mesh.from-hgt";
        Name = "Mesh From HGT";
        Version = "0.1.0";
        Icon = "⛰️";
        Category = BrickCategory.Generation;
        Description = "Parses SRTM .hgt bytes and generates a triangle mesh.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("tileId", "string", "Canonical tile id like N00E000 (optional)", required: false),
                new BrickInputDefinition("hgtBytes", "byte[]", "Raw .hgt bytes"),
                new BrickInputDefinition("verticalScale", "float", "Vertical scale multiplier", required: false, defaultValue: 1.0f),
                new BrickInputDefinition("treatNoDataAsZero", "bool", "If true, NaN becomes 0m", required: false, defaultValue: false),
                new BrickInputDefinition("forceAgenticFail", "bool", "If true, agentic path throws (for fallback demos)", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("mesh", "MeshData", "Generated mesh"),
                new BrickOutputDefinition("quality", "MeshQualityReport", "Basic quality metrics")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "grid",
                Name = "Grid Mesh (Deterministic)",
                Description = "Deterministic grid triangulation + normals",
                Executor = "GridMeshGenerator",
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
                Id = "tuned-grid",
                Name = "Tuned Grid (Agentic)",
                Description = "Uses an LLM to suggest parameters, then runs deterministic generator",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Given elevation min/max and desired style, output JSON {\"verticalScale\":1.0,\"treatNoDataAsZero\":false}.",
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

        // Same approximation as tools: 1 degree lat ~111_320m; lon scales by cos(lat).
        var midLatRad = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) * 0.5 * (Math.PI / 180.0);
        var metersPerDegLat = 111_320.0;
        var metersPerDegLon = 111_320.0 * Math.Cos(midLatRad);
        var degPerSample = 1.0 / (size - 1);
        var spacing = new GridSpacing(
            metersX: metersPerDegLon * degPerSample,
            metersY: metersPerDegLat * degPerSample);

        var grid = new ElevationGrid(size, size, bounds, spacing, heights);
        var mesh = GridMeshGenerator.Generate(grid, new MeshGenerationOptions
        {
            VerticalScale = verticalScale,
            GenerateNormals = true,
            TreatNoDataAsZero = treatNoData
        });

        var quality = MeshQualityAnalyzer.Analyze(grid, mesh);
        return Task.FromResult(new BrickOutput
        {
            ["mesh"] = mesh,
            ["quality"] = quality,
            Summary = $"Mesh: {quality.VertexCount} verts, {quality.TriangleCount} tris"
        });
    }

    private async Task<BrickOutput> ExecuteAgenticAsync(BrickInput input, IExecutionContext context, CancellationToken ct)
    {
        var forceFail = input.Get("forceAgenticFail", false);
        if (forceFail) throw new InvalidOperationException("Forced agentic failure (demo).");

        var bytes = input.Get<byte[]>("hgtBytes");
        var summary = SrtmHgtParser.Analyze(bytes);

        var verticalScale = input.Get("verticalScale", 1.0f);
        var treatNoData = input.Get("treatNoDataAsZero", false);

        // Best-effort parameter suggestion.
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
                if (doc.RootElement.TryGetProperty("verticalScale", out var vs) && vs.TryGetSingle(out var vsv))
                    verticalScale = vsv;
                if (doc.RootElement.TryGetProperty("treatNoDataAsZero", out var nd) && nd.ValueKind == System.Text.Json.JsonValueKind.True)
                    treatNoData = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LLM tuning failed; falling back to input defaults.");
        }

        // Run deterministic generator with tuned params.
        input.Set("verticalScale", verticalScale);
        input.Set("treatNoDataAsZero", treatNoData);
        return await ExecuteDeterministicAsync(input, ct);
    }
}

