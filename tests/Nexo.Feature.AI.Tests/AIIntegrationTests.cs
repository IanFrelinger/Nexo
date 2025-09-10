using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;
using Xunit;

namespace Nexo.Feature.AI.Tests;

/// <summary>
/// Integration tests for AI services with the iteration system
/// </summary>
public class AIIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public AIIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Add AI services with iteration strategy integration
        services.AddAIWithIterationStrategies();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task AIServices_ShouldBeRegistered()
    {
        // Act & Assert
        _serviceProvider.GetService<IModelOrchestrator>().Should().NotBeNull();
        _serviceProvider.GetService<IIterationCodeGenerator>().Should().NotBeNull();
        _serviceProvider.GetService<IModelProvider>().Should().NotBeNull();
    }

    [Fact]
    public async Task IterationCodeGenerator_ShouldGenerateCode_WithRealAI()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new IterationRequirements
            {
                PrioritizeCpu = true
            },
            EnvironmentProfile = new RuntimeEnvironmentProfile
            {
                PlatformType = Nexo.Core.Domain.Entities.Infrastructure.PlatformType.DotNet,
                CpuCores = 4,
                AvailableMemoryMB = 8192
            }
        };
        
        var codeContext = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            ItemName = "item",
            ActionTemplate = "x => ProcessItem(x)"
        };

        // Act
        var result = await generator.GenerateOptimalIterationCodeAsync(context, codeContext);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("foreach");
        result.Should().Contain("items");
    }

    [Fact]
    public async Task ModelOrchestrator_ShouldExecuteRequest_WithMockProvider()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();
        var provider = _serviceProvider.GetRequiredService<IModelProvider>();
        
        // Register the provider with the orchestrator
        await orchestrator.RegisterProviderAsync(provider);
        
        var request = new ModelRequest
        {
            Input = "Generate optimized iteration code for processing a list of items",
            Temperature = 0.7,
            MaxTokens = 1000
        };

        // Act
        var response = await orchestrator.ExecuteAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Response.Should().NotBeNullOrEmpty();
        response.InputTokens.Should().BeGreaterThan(0);
        response.OutputTokens.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MultiPlatformCodeGeneration_ShouldWork()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var context = new IterationContext();
        var platforms = new[] { PlatformTarget.CSharp, PlatformTarget.JavaScript };

        // Act
        var results = await generator.GenerateMultiPlatformCodeAsync(context, platforms);

        // Assert
        results.Should().HaveCount(2);
        results.Should().ContainKey(PlatformTarget.CSharp);
        results.Should().ContainKey(PlatformTarget.JavaScript);
        
        results[PlatformTarget.CSharp].Should().NotBeNullOrEmpty();
        results[PlatformTarget.JavaScript].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CodeEnhancement_ShouldImproveExistingCode()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();
        var provider = _serviceProvider.GetRequiredService<IModelProvider>();
        
        // Register the provider with the orchestrator
        await orchestrator.RegisterProviderAsync(provider);
        
        var existingCode = "foreach (var item in items) { ProcessItem(item); }";
        var context = new IterationContext
        {
            Requirements = new IterationRequirements
            {
                PrioritizeCpu = true
            }
        };

        // Act
        var enhancedCode = await generator.EnhanceIterationCodeAsync(existingCode, context);

        // Assert
        enhancedCode.Should().NotBeNullOrEmpty();
        enhancedCode.Should().NotBe(existingCode); // Should be enhanced
        enhancedCode.Should().Contain("foreach"); // Should still contain iteration logic
    }

    [Fact]
    public async Task ModelProvider_ShouldBeHealthy()
    {
        // Arrange
        var provider = _serviceProvider.GetRequiredService<IModelProvider>();

        // Act
        var healthStatus = await provider.GetHealthStatusAsync();

        // Assert
        healthStatus.Should().NotBeNull();
        healthStatus.IsHealthy.Should().BeTrue();
        healthStatus.Status.Should().NotBeNullOrEmpty();
        healthStatus.ResponseTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ModelProvider_ShouldHaveAvailableModels()
    {
        // Arrange
        var provider = _serviceProvider.GetRequiredService<IModelProvider>();

        // Act
        var models = await provider.GetAvailableModelsAsync();

        // Assert
        models.Should().NotBeNull();
        models.Should().NotBeEmpty();
        
        foreach (var model in models)
        {
            model.Name.Should().NotBeNullOrEmpty();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.IsAvailable.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ModelOrchestrator_ShouldRegisterProviders()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();
        var provider = _serviceProvider.GetRequiredService<IModelProvider>();

        // Act
        await orchestrator.RegisterProviderAsync(provider);

        // Assert
        var bestProvider = await orchestrator.GetBestModelForTaskAsync("test task", Enums.ModelType.TextGeneration);
        bestProvider.Should().NotBeNull();
        bestProvider.Should().Be(provider);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
