using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Export;
using Nexo.Infrastructure.Execution;

namespace Nexo.Infrastructure.Export;

/// <summary>
/// Generates content using AI for export.
/// </summary>
public class ContentGenerator : IContentGenerator
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<ContentGenerator> _logger;
    
    public ContentGenerator(
        IProviderFactory providerFactory,
        ILogger<ContentGenerator> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }
    
    public async Task<GeneratedContent> GenerateAsync(
        Brick brick,
        GenerationConfig config,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {VariationsPerItem} variations for brick {BrickId}", 
            config.VariationsPerItem, brick.Id);
        
        var variations = new List<GeneratedVariation>();
        
        // Generate variations
        for (int i = 0; i < config.VariationsPerItem; i++)
        {
            var prompt = BuildGenerationPrompt(brick, i);
            
            var content = await _providerFactory.ExecuteLLMAsync(
                config.Provider,
                $"You are a content generator for {brick.Category} bricks.",
                prompt,
                new { model = config.Model },
                ct);
            
            variations.Add(new GeneratedVariation
            {
                Content = content,
                Metadata = new Dictionary<string, object>
                {
                    ["index"] = i,
                    ["brickId"] = brick.Id,
                    ["generatedAt"] = DateTime.UtcNow
                }
            });
        }
        
        return new GeneratedContent
        {
            Variations = variations
        };
    }
    
    private static string BuildGenerationPrompt(Brick brick, int variationIndex)
    {
        return $@"Generate variation {variationIndex + 1} for brick: {brick.Name}

Description: {brick.Description}
Category: {brick.Category}

Domain Knowledge:
{string.Join("\n", brick.DomainKnowledge.Standards.Select(s => $"- {s}"))}

Generate appropriate content for this brick's purpose.";
    }
}

