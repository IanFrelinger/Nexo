using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.Infrastructure.Execution;

namespace Nexo.GeoTerrain.Bricks;

/// <summary>
/// Generates contour polylines from an <see cref="ElevationGrid"/> (deterministic),
/// with an optional agentic path that can tune parameters.
/// </summary>
public sealed class GeoTerrainContoursFromGridBrick : Brick
{
    private readonly IProviderFactory _llm;
    private readonly ILogger<GeoTerrainContoursFromGridBrick> _logger;

    public GeoTerrainContoursFromGridBrick(IProviderFactory llm, ILogger<GeoTerrainContoursFromGridBrick> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoterrain.contours.from-grid";
        Name = "Contours From Grid";
        Version = "0.1.0";
        Icon = "🗺️";
        Category = BrickCategory.Generation;
        Description = "Extracts contour polylines from an elevation grid (marching squares).";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("grid", "ElevationGrid", "Elevation grid"),
                new BrickInputDefinition("intervalMeters", "double", "Contour interval in meters", required: false, defaultValue: 10.0),
                new BrickInputDefinition("minElevationMeters", "double", "Optional min contour level", required: false),
                new BrickInputDefinition("maxElevationMeters", "double", "Optional max contour level", required: false),
                new BrickInputDefinition("verticalScale", "float", "Vertical scale multiplier", required: false, defaultValue: 1.0f),
                new BrickInputDefinition("treatNoDataAsZero", "bool", "If true, NaN becomes 0m", required: false, defaultValue: false),
                new BrickInputDefinition("forceAgenticFail", "bool", "If true, agentic path throws (for fallback demos)", required: false, defaultValue: false)
            ],
            Outputs =
            [
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
        var grid = input.Get<ElevationGrid>("grid");

        var interval = input.Get("intervalMeters", 10.0);
        var min = input.Get<double?>("minElevationMeters", null);
        var max = input.Get<double?>("maxElevationMeters", null);
        var verticalScale = input.Get("verticalScale", 1.0f);
        var treatNoData = input.Get("treatNoDataAsZero", false);

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
            ["contours"] = contours.ToArray(),
            Summary = $"Contours: {contours.Count} polylines"
        });
    }

    private async Task<BrickOutput> ExecuteAgenticAsync(BrickInput input, IExecutionContext context, CancellationToken ct)
    {
        var forceFail = input.Get("forceAgenticFail", false);
        if (forceFail) throw new InvalidOperationException("Forced agentic failure (demo).");

        var grid = input.Get<ElevationGrid>("grid");

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

        var interval = input.Get("intervalMeters", 10.0);
        double? minLevel = input.Get<double?>("minElevationMeters", null);
        double? maxLevel = input.Get<double?>("maxElevationMeters", null);

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
                if (doc.RootElement.TryGetProperty("intervalMeters", out var im) && im.TryGetDouble(out var v)) interval = v;
                if (doc.RootElement.TryGetProperty("minElevationMeters", out var mn) && mn.ValueKind == System.Text.Json.JsonValueKind.Number && mn.TryGetDouble(out var mnv)) minLevel = mnv;
                if (doc.RootElement.TryGetProperty("maxElevationMeters", out var mx) && mx.ValueKind == System.Text.Json.JsonValueKind.Number && mx.TryGetDouble(out var mxv)) maxLevel = mxv;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LLM tuning failed; falling back to input defaults.");
        }

        input.Set("intervalMeters", interval);
        if (minLevel.HasValue) input.Set("minElevationMeters", minLevel.Value);
        if (maxLevel.HasValue) input.Set("maxElevationMeters", maxLevel.Value);
        return await ExecuteDeterministicAsync(input, ct);
    }
}

