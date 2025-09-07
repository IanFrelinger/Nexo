using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Models.Iteration;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Xunit;

namespace Nexo.Feature.AI.Tests;

/// <summary>
/// Tests for the IterationCodeGenerator service
/// </summary>
public class IterationCodeGeneratorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IModelOrchestrator> _mockOrchestrator;

    public IterationCodeGeneratorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddIterationStrategies();
        
        _mockOrchestrator = new Mock<IModelOrchestrator>();
        services.AddTransient<IModelOrchestrator>(_ => _mockOrchestrator.Object);
        services.AddTransient<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator, Services.IterationCodeGenerator>();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task GenerateOptimalIterationCodeAsync_ShouldReturnGeneratedCode()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator>();
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new IterationRequirements
            {
                PrioritizeCpu = true
            },
            EnvironmentProfile = new RuntimeEnvironmentProfile
            {
                PlatformType = PlatformCompatibility.DotNet,
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

        var expectedResponse = new ModelResponse
        {
            Response = "foreach (var item in items) { ProcessItem(item); }",
            Success = true,
            InputTokens = 100,
            OutputTokens = 20
        };

        _mockOrchestrator
            .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await generator.GenerateOptimalIterationCodeAsync(context, codeContext);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("foreach");
        result.Should().Contain("ProcessItem");
        
        _mockOrchestrator.Verify(
            x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateOptimalIterationCodeAsync_ShouldReturnFallbackCode_WhenAIRequestFails()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator>();
        var context = new IterationContext();
        var codeContext = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            ItemName = "item"
        };

        var failedResponse = new ModelResponse
        {
            Success = false,
            ErrorMessage = "AI service unavailable"
        };

        _mockOrchestrator
            .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        // Act
        var result = await generator.GenerateOptimalIterationCodeAsync(context, codeContext);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("foreach");
        result.Should().Contain("items");
    }

    [Fact]
    public async Task GenerateMultiPlatformCodeAsync_ShouldReturnCodeForEachPlatform()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator>();
        var context = new IterationContext();
        var platforms = new[] { PlatformTarget.CSharp, PlatformTarget.JavaScript };

        var expectedResponse = new ModelResponse
        {
            Response = "Generated code for platform",
            Success = true
        };

        _mockOrchestrator
            .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await generator.GenerateMultiPlatformCodeAsync(context, platforms);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey(PlatformTarget.CSharp);
        result.Should().ContainKey(PlatformTarget.JavaScript);
        result[PlatformTarget.CSharp].Should().NotBeNullOrEmpty();
        result[PlatformTarget.JavaScript].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EnhanceIterationCodeAsync_ShouldReturnEnhancedCode()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator>();
        var existingCode = "foreach (var item in items) { ProcessItem(item); }";
        var context = new IterationContext();

        var expectedResponse = new ModelResponse
        {
            Response = "// Enhanced code with error handling\nforeach (var item in items) {\n    if (item != null) {\n        ProcessItem(item);\n    }\n}",
            Success = true
        };

        _mockOrchestrator
            .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await generator.EnhanceIterationCodeAsync(existingCode, context);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Enhanced");
        result.Should().Contain("error handling");
        
        _mockOrchestrator.Verify(
            x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnhanceIterationCodeAsync_ShouldReturnOriginalCode_WhenEnhancementFails()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator>();
        var existingCode = "foreach (var item in items) { ProcessItem(item); }";
        var context = new IterationContext();

        var failedResponse = new ModelResponse
        {
            Success = false,
            ErrorMessage = "Enhancement failed"
        };

        _mockOrchestrator
            .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        // Act
        var result = await generator.EnhanceIterationCodeAsync(existingCode, context);

        // Assert
        result.Should().Be(existingCode);
    }

    [Fact]
    public async Task GenerateOptimalIterationCodeAsync_ShouldHandleException()
    {
        // Arrange
        var generator = _serviceProvider.GetRequiredService<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator>();
        var context = new IterationContext();
        var codeContext = new CodeGenerationContext();

        _mockOrchestrator
            .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await generator.GenerateOptimalIterationCodeAsync(context, codeContext);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("foreach"); // Fallback code contains foreach
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}