using Microsoft.Extensions.Logging;
using Nexo.Abstractions;

namespace Nexo.Infrastructure.Execution.Models;

/// <summary>
/// A model wrapper that can hot-swap between an agentic provider-backed model and a deterministic fallback.
/// Selection rules (in priority order):
/// - If system messages contain "nexo.model.prefer=deterministic" -> deterministic.
/// - Else if system messages contain "nexo.model.provider=&lt;name&gt;" -> try provider-backed with that provider.
/// - Else if env var NEXO_MODEL_PROVIDER is set -> try provider-backed with that provider.
/// - Else -> deterministic.
///
/// If provider-backed execution fails, it falls back to deterministic.
/// </summary>
public sealed class HotSwappableModel : IModel
{
    private readonly ProviderBackedModel _agentic;
    private readonly IModel _deterministic;
    private readonly ILogger<HotSwappableModel> _logger;

    /// <summary>Initializes a new hot swappable model.</summary>
    public HotSwappableModel(
        ProviderBackedModel agentic,
        ILogger<HotSwappableModel> logger)
    {
        _agentic = agentic;
        _deterministic = new EchoDeterministicModel();
        _logger = logger;
    }

    /// <summary>Complete asynchronously.</summary>
    public async Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
    {
        var (prefer, providerOverride) = ParseRuntime(input);

        // Attach provider override via system directive for ProviderBackedModel.
        var provider = providerOverride;
        if (string.IsNullOrWhiteSpace(provider))
        {
            provider = Environment.GetEnvironmentVariable("NEXO_MODEL_PROVIDER");
        }

        // Deterministic preference:
        // - if a provider is specified (e.g., "offline"/"mock-json"), treat it as a deterministic-safe path
        //   and try provider-backed first (still no network for offline providers), with fallback to echo.
        // - otherwise, use deterministic echo.
        if (string.Equals(prefer, "deterministic", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(provider))
            {
                var routed = InjectProviderDirective(input, provider);
                try
                {
                    return await _agentic.CompleteAsync(routed, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Deterministic(provider) model failed; falling back to deterministic echo");
                    return await _deterministic.CompleteAsync(input, ct);
                }
            }

            return await _deterministic.CompleteAsync(input, ct);
        }

        if (!string.IsNullOrWhiteSpace(provider))
        {
            var routed = InjectProviderDirective(input, provider);
            try
            {
                return await _agentic.CompleteAsync(routed, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agentic model failed; falling back to deterministic");
                return await _deterministic.CompleteAsync(input, ct);
            }
        }

        return await _deterministic.CompleteAsync(input, ct);
    }

    private static (string? prefer, string? provider) ParseRuntime(ModelInput input)
    {
        string? prefer = null;
        string? provider = null;

        foreach (var (role, content) in input.Messages)
        {
            if (!string.Equals(role, "system", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var line in (content ?? "").Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("nexo.model.prefer=", StringComparison.OrdinalIgnoreCase))
                {
                    prefer = trimmed["nexo.model.prefer=".Length..].Trim();
                }
                else if (trimmed.StartsWith("nexo.model.provider=", StringComparison.OrdinalIgnoreCase))
                {
                    provider = trimmed["nexo.model.provider=".Length..].Trim();
                }
            }
        }

        return (prefer, provider);
    }

    private static ModelInput InjectProviderDirective(ModelInput input, string provider)
    {
        var msgs = input.Messages.ToList();
        var directive = $"nexo.model.provider={provider}";

        for (var i = 0; i < msgs.Count; i++)
        {
            var (role, content) = msgs[i];
            if (!string.Equals(role, "system", StringComparison.OrdinalIgnoreCase)) continue;
            msgs[i] = (role, directive + "\n" + (content ?? ""));
            return new ModelInput(msgs);
        }

        // No system message; prepend one.
        msgs.Insert(0, ("system", directive));
        return new ModelInput(msgs);
    }

    private sealed class EchoDeterministicModel : IModel
    {
        /// <summary>Complete asynchronously.</summary>
        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
        {
            var text = input.Messages.LastOrDefault().content ?? "";
            return Task.FromResult(new ModelOutput(text));
        }
    }
}

