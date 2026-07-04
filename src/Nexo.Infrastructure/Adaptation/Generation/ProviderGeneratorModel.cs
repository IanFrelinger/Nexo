using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Infrastructure.Execution;

namespace Nexo.Infrastructure.Adaptation.Generation;

/// <summary>
/// Real model implementation behind the sealed seam. Marked isolation-enforced; not used in hermetic tests.
/// </summary>
public sealed class ProviderGeneratorModel : IGeneratorModel
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<ProviderGeneratorModel>? _logger;
    private readonly string _provider;

    /// <summary>Initializes a new provider generator model.</summary>
    public ProviderGeneratorModel(
        IProviderFactory providerFactory,
        string provider = "ollama",
        ILogger<ProviderGeneratorModel>? logger = null)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _provider = provider;
        _logger = logger;
    }

    /// <summary>Generate asynchronously.</summary>
    public async Task<GeneratedBrickSource> GenerateAsync(
        IntentSpec intent,
        WitnessSignature witnessSignature,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = """
You generate deterministic C# brick implementations for Nexo.
Rules:
- Target net8, inherit DomainBrick, override ExecuteAsync only.
- Use only: using Nexo.Core.Domain.Bricks; using Nexo.Core.Domain.Execution;
- Namespace Nexo.Certified.DamageResolver, class name ends with Brick.
- No DateTime.Now, Random, or nondeterministic APIs.
- Return ONLY the C# source file contents.
""";

        var userPrompt = $"""
Intent: {intent.Description}
BrickId: {witnessSignature.BrickId}
Inputs: {string.Join(", ", witnessSignature.Inputs.Select(i => $"{i.Name}:{i.Type}"))}
Outputs: {string.Join(", ", witnessSignature.Outputs.Select(o => $"{o.Name}:{o.Type}"))}
""";

        _logger?.LogInformation(
            "ProviderGeneratorModel generating for intent {IntentId} via {Provider}",
            intent.IntentId,
            _provider);

        var response = await _providerFactory.ExecuteLLMAsync(
            _provider,
            systemPrompt,
            userPrompt,
            new { temperature = 0 },
            cancellationToken).ConfigureAwait(false);

        var source = ExtractSource(response);
        return new GeneratedBrickSource(
            source,
            Provenance: $"model:{_provider}:isolation-enforced",
            ClassName: InferClassName(source),
            Namespace: InferNamespace(source));
    }

    private static string ExtractSource(string response)
    {
        const string fence = "```";
        var start = response.IndexOf(fence, StringComparison.Ordinal);
        if (start >= 0)
        {
            var afterFence = response.IndexOf('\n', start);
            var end = response.IndexOf(fence, afterFence + 1, StringComparison.Ordinal);
            if (afterFence >= 0 && end > afterFence)
                return response[(afterFence + 1)..end].Trim();
        }

        return response.Trim();
    }

    private static string InferClassName(string source)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            source,
            @"public\s+sealed\s+class\s+(\w+)");
        return match.Success ? match.Groups[1].Value : "GeneratedBrick";
    }

    private static string InferNamespace(string source)
    {
        var match = System.Text.RegularExpressions.Regex.Match(source, @"namespace\s+([\w.]+)");
        return match.Success ? match.Groups[1].Value : "Nexo.Certified.DamageResolver";
    }
}
