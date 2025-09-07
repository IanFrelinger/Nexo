using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Xunit;

namespace Nexo.Feature.AI.Tests;

/// <summary>
/// Tests for the ModelOrchestrator service
/// </summary>
public class ModelOrchestratorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IModelProvider> _mockProvider;

    public ModelOrchestratorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTransient<IModelOrchestrator, Services.ModelOrchestrator>();
        
        _mockProvider = new Mock<IModelProvider>();
        _mockProvider.Setup(x => x.ProviderId).Returns("test-provider");
        _mockProvider.Setup(x => x.DisplayName).Returns("Test Provider");
        _mockProvider.Setup(x => x.Name).Returns("TestProvider");
        _mockProvider.Setup(x => x.SupportedModelTypes).Returns(new[] { Enums.ModelType.TextGeneration });
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResponse_WhenProviderIsAvailable()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();
        var request = new ModelRequest { Input = "Test input" };
        
        var expectedResponse = new ModelResponse
        {
            Response = "Test response",
            Success = true,
            InputTokens = 10,
            OutputTokens = 5
        };

        _mockProvider
            .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _mockProvider
            .Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelHealthStatus { IsHealthy = true });

        await orchestrator.RegisterProviderAsync(_mockProvider.Object);

        // Act
        var result = await orchestrator.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Response.Should().Be("Test response");
        
        _mockProvider.Verify(
            x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenNoProvidersAvailable()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();
        var request = new ModelRequest { Input = "Test input" };

        // Act
        var result = await orchestrator.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No suitable model provider available");
    }

    [Fact]
    public async Task RegisterProviderAsync_ShouldAddProvider()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();

        // Act
        await orchestrator.RegisterProviderAsync(_mockProvider.Object);

        // Assert
        _mockProvider.Verify(x => x.DisplayName, Times.AtLeastOnce);
    }

    [Fact]
    public async Task RegisterProviderAsync_ShouldThrow_WhenProviderIsNull()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => orchestrator.RegisterProviderAsync(null!));
    }

    [Fact]
    public async Task GetBestModelForTaskAsync_ShouldReturnProvider_WhenSuitableProviderExists()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();
        await orchestrator.RegisterProviderAsync(_mockProvider.Object);

        // Act
        var result = await orchestrator.GetBestModelForTaskAsync("test task", Enums.ModelType.TextGeneration);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(_mockProvider.Object);
    }

    [Fact]
    public async Task GetBestModelForTaskAsync_ShouldReturnNull_WhenNoSuitableProviderExists()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();
        await orchestrator.RegisterProviderAsync(_mockProvider.Object);

        // Act
        var result = await orchestrator.GetBestModelForTaskAsync("test task", Enums.ModelType.CodeGeneration);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleProviderException()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();
        var request = new ModelRequest { Input = "Test input" };

        _mockProvider
            .Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelHealthStatus { IsHealthy = true });

        _mockProvider
            .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider error"));

        await orchestrator.RegisterProviderAsync(_mockProvider.Object);

        // Act
        var result = await orchestrator.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Provider error");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipUnhealthyProviders()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<IModelOrchestrator>();
        var request = new ModelRequest { Input = "Test input" };

        _mockProvider
            .Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelHealthStatus { IsHealthy = false });

        await orchestrator.RegisterProviderAsync(_mockProvider.Object);

        // Act
        var result = await orchestrator.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No suitable model provider available");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
