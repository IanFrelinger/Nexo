using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Tests.Commands;
using Nexo.Shared.Interfaces.Resource;
using Nexo.Core.Application.Interfaces.Caching;
using System;

namespace Nexo.Core.Application.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Core.Application layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class CoreApplicationPipelineTests
{
    private readonly ILogger<CoreApplicationPipelineTests> _logger;

    public CoreApplicationPipelineTests()
    {
        _logger = NullLogger<CoreApplicationPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void ModelResponse_Properties_WorkCorrectly()
    {
        _logger.LogInformation("Starting ModelResponse test");
        
        var command = new ModelValidationCommand(NullLogger<ModelValidationCommand>.Instance);
        var result = command.ValidateTestModel(timeoutMs: 5000);
        
        Assert.True(result, "ModelResponse properties should work correctly");
        _logger.LogInformation("ModelResponse test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ModelValidationResult_Properties_WorkCorrectly()
    {
        _logger.LogInformation("Starting ModelValidationResult test");
        
        var command = new ModelValidationCommand(NullLogger<ModelValidationCommand>.Instance);
        var result = command.ValidateTestValidationResult(timeoutMs: 5000);
        
        Assert.True(result, "ModelValidationResult properties should work correctly");
        _logger.LogInformation("ModelValidationResult test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ModelHealthStatus_Properties_WorkCorrectly()
    {
        _logger.LogInformation("Starting ModelHealthStatus test");
        
        var command = new ModelValidationCommand(NullLogger<ModelValidationCommand>.Instance);
        var result = command.ValidateTestHealthStatus(timeoutMs: 5000);
        
        Assert.True(result, "ModelHealthStatus properties should work correctly");
        _logger.LogInformation("ModelHealthStatus test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ModelOptimizationResult_Properties_WorkCorrectly()
    {
        _logger.LogInformation("Starting ModelOptimizationResult test");
        
        var command = new ModelValidationCommand(NullLogger<ModelValidationCommand>.Instance);
        var result = command.ValidateTestOptimizationResult(timeoutMs: 5000);
        
        Assert.True(result, "ModelOptimizationResult properties should work correctly");
        _logger.LogInformation("ModelOptimizationResult test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ResourceType_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting ResourceType enum test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateResourceType(timeoutMs: 5000);
        
        Assert.True(result, "ResourceType enum values should be defined");
        _logger.LogInformation("ResourceType enum test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ResourcePriority_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting ResourcePriority enum test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateResourcePriority(timeoutMs: 5000);
        
        Assert.True(result, "ResourcePriority enum values should be defined");
        _logger.LogInformation("ResourcePriority enum test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ResourceAlertType_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting ResourceAlertType enum test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateResourceAlertType(timeoutMs: 5000);
        
        Assert.True(result, "ResourceAlertType enum values should be defined");
        _logger.LogInformation("ResourceAlertType enum test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ResourceAlertSeverity_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting ResourceAlertSeverity enum test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateResourceAlertSeverity(timeoutMs: 5000);
        
        Assert.True(result, "ResourceAlertSeverity enum values should be defined");
        _logger.LogInformation("ResourceAlertSeverity enum test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ResourceHealth_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting ResourceHealth enum test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateResourceHealth(timeoutMs: 5000);
        
        Assert.True(result, "ResourceHealth enum values should be defined");
        _logger.LogInformation("ResourceHealth enum test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void CacheItemPriority_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting CacheItemPriority enum test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateCacheItemPriority(timeoutMs: 5000);
        
        Assert.True(result, "CacheItemPriority enum values should be defined");
        _logger.LogInformation("CacheItemPriority enum test completed successfully");
    }

    [Fact(Timeout = 5000)]
    public void Enums_Values_AreDefined()
    {
        _logger.LogInformation("Starting enum values test");
        
        // Test that enum values are accessible
        Assert.True(Enum.IsDefined(typeof(ResourceType), ResourceType.CPU));
        Assert.True(Enum.IsDefined(typeof(ResourceType), ResourceType.Memory));
        Assert.True(Enum.IsDefined(typeof(ResourceType), ResourceType.GPU));
        Assert.True(Enum.IsDefined(typeof(ResourceType), ResourceType.Storage));
        Assert.True(Enum.IsDefined(typeof(ResourceType), ResourceType.Network));
        Assert.True(Enum.IsDefined(typeof(ResourceType), ResourceType.AIModel));
        
        Assert.True(Enum.IsDefined(typeof(ResourcePriority), ResourcePriority.Low));
        Assert.True(Enum.IsDefined(typeof(ResourcePriority), ResourcePriority.Normal));
        Assert.True(Enum.IsDefined(typeof(ResourcePriority), ResourcePriority.High));
        Assert.True(Enum.IsDefined(typeof(ResourcePriority), ResourcePriority.Critical));
        
        Assert.True(Enum.IsDefined(typeof(ResourceAlertType), ResourceAlertType.HighUtilization));
        Assert.True(Enum.IsDefined(typeof(ResourceAlertType), ResourceAlertType.ResourceExhaustion));
        Assert.True(Enum.IsDefined(typeof(ResourceAlertType), ResourceAlertType.AllocationFailure));
        Assert.True(Enum.IsDefined(typeof(ResourceAlertType), ResourceAlertType.ProviderHealth));
        
        Assert.True(Enum.IsDefined(typeof(ResourceAlertSeverity), ResourceAlertSeverity.Information));
        Assert.True(Enum.IsDefined(typeof(ResourceAlertSeverity), ResourceAlertSeverity.Warning));
        Assert.True(Enum.IsDefined(typeof(ResourceAlertSeverity), ResourceAlertSeverity.Error));
        Assert.True(Enum.IsDefined(typeof(ResourceAlertSeverity), ResourceAlertSeverity.Critical));
        
        Assert.True(Enum.IsDefined(typeof(ResourceHealth), ResourceHealth.Healthy));
        Assert.True(Enum.IsDefined(typeof(ResourceHealth), ResourceHealth.Degraded));
        Assert.True(Enum.IsDefined(typeof(ResourceHealth), ResourceHealth.Unhealthy));
        Assert.True(Enum.IsDefined(typeof(ResourceHealth), ResourceHealth.Unknown));
        
        Assert.True(Enum.IsDefined(typeof(CacheItemPriority), CacheItemPriority.Low));
        Assert.True(Enum.IsDefined(typeof(CacheItemPriority), CacheItemPriority.Normal));
        Assert.True(Enum.IsDefined(typeof(CacheItemPriority), CacheItemPriority.High));
        Assert.True(Enum.IsDefined(typeof(CacheItemPriority), CacheItemPriority.NeverRemove));
        
        _logger.LogInformation("Enum values test completed successfully");
    }

    [Fact(Timeout = 5000)]
    public void InterfaceImplementations_AreAccessible()
    {
        _logger.LogInformation("Starting interface implementations test");
        
        // Test that interfaces can be referenced (compilation test)
        Assert.NotNull(typeof(Nexo.Shared.Interfaces.Resource.IResourceManager));
        Assert.NotNull(typeof(Nexo.Core.Application.Interfaces.Caching.IDistributedCache));
        
        _logger.LogInformation("Interface implementations test completed successfully");
    }
} 