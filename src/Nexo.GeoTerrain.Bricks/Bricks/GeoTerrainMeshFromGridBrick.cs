using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.Infrastructure.Execution;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Builds a mesh from an <see cref="ElevationGrid"/> (deterministic), with an optional agentic path that can tune parameters.
/// </summary>
public sealed class GeoTerrainMeshFromGridBrick : Brick
{
    private readonly IProviderFactory _llm;
    private readonly ILogger<GeoTerrainMeshFromGridBrick> _logger;

    public GeoTerrainMeshFromGridBrick(IProviderFactory llm, ILogger<GeoTerrainMeshFromGridBrick> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.mesh.from-grid";
        Name = "Mesh From Grid";
        Version = "0.1.0";
        Icon = "🧬";
        Category = BrickCategory.Generation;
        Description = "Generates a triangle mesh from an elevation grid.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("grid", "ElevationGrid", "Elevation grid"),
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
        var grid = input.Get<ElevationGrid>("grid");
        var verticalScale = input.Get("verticalScale", 1.0f);
        var treatNoData = input.Get("treatNoDataAsZero", false);

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

        var grid = input.Get<ElevationGrid>("grid");

        // Basic min/max scan for tuning context.
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var v = grid.GetHeightMeters(x, y);
                if (float.IsNaN(v)) continue;
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }
        if (float.IsPositiveInfinity(min)) min = 0f;
        if (float.IsNegativeInfinity(max)) max = 0f;

        var verticalScale = input.Get("verticalScale", 1.0f);
        var treatNoData = input.Get("treatNoDataAsZero", false);

        try
        {
            var resp = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                $"Grid {grid.Width}x{grid.Height} min={min} max={max}",
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

        input.Set("verticalScale", verticalScale);
        input.Set("treatNoDataAsZero", treatNoData);
        return await ExecuteDeterministicAsync(input, ct);
    }
}

