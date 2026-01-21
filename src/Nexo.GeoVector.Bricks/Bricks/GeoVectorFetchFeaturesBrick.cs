using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Infrastructure.Execution;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.GeoVector.Bricks;

/// <summary>
/// Fetch vector features for a region (deterministic) with an optional agentic path for filtering/tuning.
/// </summary>
public sealed class GeoVectorFetchFeaturesBrick : Brick
{
    private readonly IVectorProvider _provider;
    private readonly IProviderFactory _llm;
    private readonly ILogger<GeoVectorFetchFeaturesBrick> _logger;

    public GeoVectorFetchFeaturesBrick(
        IVectorProvider provider,
        IProviderFactory llm,
        ILogger<GeoVectorFetchFeaturesBrick> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geovector.fetch.features";
        Name = "Fetch Vector Features";
        Version = "0.1.0";
        Icon = "🗺️";
        Category = BrickCategory.Input;
        Description = "Fetches vector features (buildings/roads/vegetation) for a GeoBounds.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("bounds", "GeoBounds", "Geo bounds to query"),
                new BrickInputDefinition("kind", "string", "Feature kind (e.g. building/road/vegetation)", required: false, defaultValue: "building"),
                new BrickInputDefinition("forceAgenticFail", "bool", "If true, agentic path throws (for fallback demos)", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("features", "GeoFeatureSet", "Fetched features"),
                new BrickOutputDefinition("kind", "string", "Resolved kind")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "provider",
                Name = "Provider Fetch (Deterministic)",
                Description = "Uses configured provider without LLM",
                Executor = "IVectorProvider",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "< 1s",
                    ResourceUsage = ResourceUsage.Low
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "guided-fetch",
                Name = "Guided Fetch (Agentic)",
                Description = "LLM can normalize kind input and recommend filters; provider fetch remains deterministic",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Normalize a map layer kind string. Output JSON {\"kind\":\"building\"}.",
                    Temperature = 0.0,
                    MaxTokens = 100
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
        var kindText = input.Get("kind", "building") ?? "building";
        var kind = new FeatureKind(kindText);

        var features = await _provider.GetFeaturesAsync(bounds, kind, ct);

        return new BrickOutput
        {
            ["features"] = features,
            ["kind"] = kind.Value,
            Summary = $"Fetched {features.Features.Count} {kind} feature(s)"
        };
    }

    private async Task<BrickOutput> ExecuteAgenticAsync(BrickInput input, IExecutionContext context, CancellationToken ct)
    {
        if (input.Get("forceAgenticFail", false))
        {
            throw new InvalidOperationException("Forced agentic failure (GeoVectorFetchFeaturesBrick).");
        }

        var bounds = input.Get<GeoBounds>("bounds");
        var kindText = input.Get("kind", "building") ?? "building";

        // Use the model in a super constrained way (offline provider will just echo).
        try
        {
            _ = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                $"Input: {kindText}\nReturn JSON {{\"kind\":\"building\"}}.",
                Implementations.Agentic.LLMConfig,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LLM normalization failed; proceeding with raw kind.");
        }

        // Keep actual fetch deterministic for v0.
        var kind = new FeatureKind(kindText);
        var features = await _provider.GetFeaturesAsync(bounds, kind, ct);

        _logger.LogInformation("Fetched {Count} features for kind={Kind}", features.Features.Count, kind.Value);

        return new BrickOutput
        {
            ["features"] = features,
            ["kind"] = kind.Value,
            Summary = $"Fetched {features.Features.Count} {kind} feature(s) (agentic path)"
        };
    }
}

