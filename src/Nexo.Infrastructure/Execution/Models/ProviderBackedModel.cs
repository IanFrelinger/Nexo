using Microsoft.Extensions.Logging;
using Nexo.Abstractions;

namespace Nexo.Infrastructure.Execution.Models;

/// <summary>
/// IModel adapter that routes ModelInput messages through IProviderFactory (chat/completions),
/// using the configured provider.
/// </summary>
public sealed class ProviderBackedModel : IModel
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<ProviderBackedModel> _logger;

    public ProviderBackedModel(IProviderFactory providerFactory, ILogger<ProviderBackedModel> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
    {
        var (provider, systemPrompt, userPrompt) = Parse(input);
        if (string.IsNullOrWhiteSpace(provider)) provider = "offline";

        if (!_providerFactory.IsProviderAvailable(provider))
        {
            throw new InvalidOperationException($"Provider '{provider}' not available");
        }

        var text = await _providerFactory.ExecuteLLMAsync(
            provider,
            systemPrompt,
            userPrompt,
            config: new { },
            cancellationToken: ct);

        return new ModelOutput(text);
    }

    private static (string? provider, string systemPrompt, string userPrompt) Parse(ModelInput input)
    {
        var provider = (string?)null;
        var systemParts = new List<string>();
        var userParts = new List<string>();

        foreach (var (role, content) in input.Messages)
        {
            if (string.Equals(role, "system", StringComparison.OrdinalIgnoreCase))
            {
                // Optional inline runtime config:
                // nexo.model.provider=<provider>
                foreach (var line in (content ?? "").Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("nexo.model.provider=", StringComparison.OrdinalIgnoreCase))
                    {
                        provider = trimmed["nexo.model.provider=".Length..].Trim();
                        continue;
                    }
                    systemParts.Add(line);
                }
            }
            else
            {
                userParts.Add($"[{role}] {content}");
            }
        }

        return (provider, string.Join('\n', systemParts).Trim(), string.Join('\n', userParts).Trim());
    }
}

