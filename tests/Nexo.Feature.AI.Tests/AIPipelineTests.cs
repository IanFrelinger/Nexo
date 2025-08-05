using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.AI.Tests.Commands;
using Nexo.Feature.AI.Enums;
using System;

namespace Nexo.Feature.AI.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Feature.AI layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class AIPipelineTests
{
    private readonly ILogger<AIPipelineTests> _logger;

    public AIPipelineTests()
    {
        _logger = NullLogger<AIPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void AI_Configuration_WorksCorrectly()
    {
        _logger.LogInformation("Starting AI configuration test");
        
        var command = new AIValidationCommand(NullLogger<AIValidationCommand>.Instance);
        var result = command.ValidateAIConfiguration(timeoutMs: 5000);
        
        Assert.True(result, "AI configuration should work correctly");
        _logger.LogInformation("AI configuration test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AI_ModelConfiguration_WorksCorrectly()
    {
        _logger.LogInformation("Starting AI model configuration test");
        
        var command = new AIValidationCommand(NullLogger<AIValidationCommand>.Instance);
        var result = command.ValidateAIModelConfiguration(timeoutMs: 5000);
        
        Assert.True(result, "AI model configuration should work correctly");
        _logger.LogInformation("AI model configuration test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AI_CachingConfiguration_WorksCorrectly()
    {
        _logger.LogInformation("Starting AI caching configuration test");
        
        var command = new AIValidationCommand(NullLogger<AIValidationCommand>.Instance);
        var result = command.ValidateAICachingConfiguration(timeoutMs: 5000);
        
        Assert.True(result, "AI caching configuration should work correctly");
        _logger.LogInformation("AI caching configuration test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AI_Enums_WorkCorrectly()
    {
        _logger.LogInformation("Starting AI enums test");
        
        var command = new AIValidationCommand(NullLogger<AIValidationCommand>.Instance);
        var result = command.ValidateAIEnums(timeoutMs: 5000);
        
        Assert.True(result, "AI enums should work correctly");
        _logger.LogInformation("AI enums test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AIMode_EnumValues_AreDefined()
    {
        _logger.LogInformation("Starting AIMode enum validation");
        
        Assert.True(Enum.IsDefined(typeof(AiMode), AiMode.Development));
        Assert.True(Enum.IsDefined(typeof(AiMode), AiMode.Production));
        Assert.True(Enum.IsDefined(typeof(AiMode), AiMode.AiHeavy));
        
        _logger.LogInformation("AIMode enum validation completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ModelType_EnumValues_AreDefined()
    {
        _logger.LogInformation("Starting ModelType enum validation");
        
        Assert.True(Enum.IsDefined(typeof(ModelType), ModelType.TextGeneration));
        Assert.True(Enum.IsDefined(typeof(ModelType), ModelType.CodeGeneration));
        Assert.True(Enum.IsDefined(typeof(ModelType), ModelType.ImageGeneration));
        Assert.True(Enum.IsDefined(typeof(ModelType), ModelType.Multimodal));
        
        _logger.LogInformation("ModelType enum validation completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void CacheEvictionPolicy_EnumValues_AreDefined()
    {
        _logger.LogInformation("Starting CacheEvictionPolicy enum validation");
        
        Assert.True(Enum.IsDefined(typeof(CacheEvictionPolicy), CacheEvictionPolicy.LeastRecentlyUsed));
        Assert.True(Enum.IsDefined(typeof(CacheEvictionPolicy), CacheEvictionPolicy.LeastFrequentlyUsed));
        
        _logger.LogInformation("CacheEvictionPolicy enum validation completed successfully");
    }
} 