using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Infrastructure.Tests.Commands;
using System;
using System.Threading.Tasks;

namespace Nexo.Infrastructure.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Infrastructure layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class InfrastructurePipelineTests
{
    private readonly ILogger<InfrastructurePipelineTests> _logger;

    public InfrastructurePipelineTests()
    {
        _logger = NullLogger<InfrastructurePipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void CommandOutputType_Enum_ValuesAreDefined()
    {
        _logger.LogInformation("Starting enum validation test");
        
        var command = new EnumValidationCommand(NullLogger<EnumValidationCommand>.Instance);
        var result = command.ValidateCommandOutputTypeEnum(timeoutMs: 5000);
        
        Assert.True(result, "Enum validation should pass");
        _logger.LogInformation("Enum validation test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void MemoryCache_Constructor_WorksCorrectly()
    {
        _logger.LogInformation("Starting MemoryCache constructor test");
        
        using var command = new MemoryCacheCommand(NullLogger<MemoryCacheCommand>.Instance);
        var result = command.TestConstructor(timeoutMs: 3000);
        
        Assert.True(result, "MemoryCache constructor should work correctly");
        _logger.LogInformation("MemoryCache constructor test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void MemoryCache_SetAndGet_WorksCorrectly()
    {
        _logger.LogInformation("Starting MemoryCache set/get test");
        
        using var command = new MemoryCacheCommand(NullLogger<MemoryCacheCommand>.Instance);
        var result = command.TestSetAndGetAsync(timeoutMs: 5000);
        
        Assert.True(result, "MemoryCache set/get operations should work correctly");
        _logger.LogInformation("MemoryCache set/get test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void MemoryCache_Remove_WorksCorrectly()
    {
        _logger.LogInformation("Starting MemoryCache remove test");
        
        using var command = new MemoryCacheCommand(NullLogger<MemoryCacheCommand>.Instance);
        var result = command.TestRemoveAsync(timeoutMs: 5000);
        
        Assert.True(result, "MemoryCache remove operation should work correctly");
        _logger.LogInformation("MemoryCache remove test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void MemoryCache_Statistics_WorksCorrectly()
    {
        _logger.LogInformation("Starting MemoryCache statistics test");
        
        using var command = new MemoryCacheCommand(NullLogger<MemoryCacheCommand>.Instance);
        var result = command.TestStatisticsAsync(timeoutMs: 5000);
        
        Assert.True(result, "MemoryCache statistics should work correctly");
        _logger.LogInformation("MemoryCache statistics test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AIConfiguration_Constructor_WorksCorrectly()
    {
        _logger.LogInformation("Starting AI Configuration constructor test");
        
        var command = new AIConfigurationCommand(NullLogger<AIConfigurationCommand>.Instance);
        var result = command.TestConstructor(timeoutMs: 3000);
        
        Assert.True(result, "AI Configuration constructor should work correctly");
        _logger.LogInformation("AI Configuration constructor test completed successfully");
    }

    [Fact(Timeout = 15000)]
    public async Task AIConfiguration_GetConfiguration_WorksCorrectly()
    {
        _logger.LogInformation("Starting AI Configuration get test");
        
        var command = new AIConfigurationCommand(NullLogger<AIConfigurationCommand>.Instance);
        var result = await command.TestGetConfigurationAsync(timeoutMs: 10000);
        
        Assert.True(result, "AI Configuration get should work correctly");
        _logger.LogInformation("AI Configuration get test completed successfully");
    }

    [Fact(Timeout = 15000)]
    public async Task AIConfiguration_LoadForMode_WorksCorrectly()
    {
        _logger.LogInformation("Starting AI Configuration load for mode test");
        
        var command = new AIConfigurationCommand(NullLogger<AIConfigurationCommand>.Instance);
        var result = await command.TestLoadForModeAsync(timeoutMs: 10000);
        
        Assert.True(result, "AI Configuration load for mode should work correctly");
        _logger.LogInformation("AI Configuration load for mode test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AIConfiguration_GetDefaultConfiguration_WorksCorrectly()
    {
        _logger.LogInformation("Starting AI Configuration default configuration test");
        
        var command = new AIConfigurationCommand(NullLogger<AIConfigurationCommand>.Instance);
        var result = command.TestGetDefaultConfiguration(timeoutMs: 3000);
        
        Assert.True(result, "AI Configuration default configuration should work correctly");
        _logger.LogInformation("AI Configuration default configuration test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ProcessCommandExecutor_Constructor_WorksCorrectly()
    {
        _logger.LogInformation("Starting ProcessCommandExecutor constructor test");
        
        var command = new ProcessCommandExecutorCommand(NullLogger<ProcessCommandExecutorCommand>.Instance);
        var result = command.TestConstructor(timeoutMs: 3000);
        
        Assert.True(result, "ProcessCommandExecutor constructor should work correctly");
        _logger.LogInformation("ProcessCommandExecutor constructor test completed successfully");
    }

    [Fact(Timeout = 20000)]
    public async Task ProcessCommandExecutor_Execute_WorksCorrectly()
    {
        _logger.LogInformation("Starting ProcessCommandExecutor execute test");
        
        var command = new ProcessCommandExecutorCommand(NullLogger<ProcessCommandExecutorCommand>.Instance);
        var result = await command.TestExecuteAsync(timeoutMs: 15000);
        
        Assert.True(result, "ProcessCommandExecutor execute should work correctly");
        _logger.LogInformation("ProcessCommandExecutor execute test completed successfully");
    }

    [Fact(Timeout = 15000)]
    public async Task ProcessCommandExecutor_IsCommandAvailable_WorksCorrectly()
    {
        _logger.LogInformation("Starting ProcessCommandExecutor command availability test");
        
        var command = new ProcessCommandExecutorCommand(NullLogger<ProcessCommandExecutorCommand>.Instance);
        var result = await command.TestIsCommandAvailableAsync(timeoutMs: 10000);
        
        Assert.True(result, "ProcessCommandExecutor command availability should work correctly");
        _logger.LogInformation("ProcessCommandExecutor command availability test completed successfully");
    }

    [Fact(Timeout = 5000)]
    public void Enums_Values_AreDefined()
    {
        _logger.LogInformation("Starting enum values test");
        
        // Test that enum values are accessible
        Assert.True(Enum.IsDefined(typeof(Nexo.Core.Application.Interfaces.Caching.CacheItemPriority), Nexo.Core.Application.Interfaces.Caching.CacheItemPriority.Low));
        Assert.True(Enum.IsDefined(typeof(Nexo.Core.Application.Interfaces.Caching.CacheItemPriority), Nexo.Core.Application.Interfaces.Caching.CacheItemPriority.Normal));
        Assert.True(Enum.IsDefined(typeof(Nexo.Core.Application.Interfaces.Caching.CacheItemPriority), Nexo.Core.Application.Interfaces.Caching.CacheItemPriority.High));
        Assert.True(Enum.IsDefined(typeof(Nexo.Core.Application.Interfaces.Caching.CacheItemPriority), Nexo.Core.Application.Interfaces.Caching.CacheItemPriority.NeverRemove));
        
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourceType), Nexo.Shared.Interfaces.Resource.ResourceType.CPU));
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourceType), Nexo.Shared.Interfaces.Resource.ResourceType.Memory));
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourceType), Nexo.Shared.Interfaces.Resource.ResourceType.GPU));
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourceType), Nexo.Shared.Interfaces.Resource.ResourceType.Storage));
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourceType), Nexo.Shared.Interfaces.Resource.ResourceType.Network));
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourceType), Nexo.Shared.Interfaces.Resource.ResourceType.AIModel));
        
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourcePriority), Nexo.Shared.Interfaces.Resource.ResourcePriority.Low));
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourcePriority), Nexo.Shared.Interfaces.Resource.ResourcePriority.Normal));
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourcePriority), Nexo.Shared.Interfaces.Resource.ResourcePriority.High));
        Assert.True(Enum.IsDefined(typeof(Nexo.Shared.Interfaces.Resource.ResourcePriority), Nexo.Shared.Interfaces.Resource.ResourcePriority.Critical));
        
        _logger.LogInformation("Enum values test completed successfully");
    }

    [Fact(Timeout = 5000)]
    public void InterfaceImplementations_AreAccessible()
    {
        _logger.LogInformation("Starting interface implementations test");
        
        // Test that interfaces can be referenced (compilation test)
        Assert.NotNull(typeof(Nexo.Core.Application.Interfaces.Caching.IDistributedCache));
        Assert.NotNull(typeof(Nexo.Core.Application.Interfaces.Caching.ICacheSerializer));
        Assert.NotNull(typeof(Nexo.Core.Application.Interfaces.Caching.ICacheEvictionPolicy));
        Assert.NotNull(typeof(Nexo.Shared.Interfaces.Resource.IResourceManager));
        Assert.NotNull(typeof(Nexo.Shared.Interfaces.ICommandExecutor));
        
        _logger.LogInformation("Interface implementations test completed successfully");
    }
} 