using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Xunit;

namespace Nexo.Feature.AI.Tests;

/// <summary>
/// Tests for the MockModelProvider service
/// </summary>
public class MockModelProviderTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IModelProvider _provider;

    public MockModelProviderTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTransient<IModelProvider, Services.MockModelProvider>();
        
        _serviceProvider = services.BuildServiceProvider();
        _provider = _serviceProvider.GetRequiredService<IModelProvider>();
    }

    [Fact]
    public void ProviderId_ShouldReturnCorrectId()
    {
        // Act & Assert
        _provider.ProviderId.Should().Be("mock-provider");
    }

    [Fact]
    public void DisplayName_ShouldReturnCorrectName()
    {
        // Act & Assert
        _provider.DisplayName.Should().Be("Mock Model Provider");
    }

    [Fact]
    public void Name_ShouldReturnCorrectName()
    {
        // Act & Assert
        _provider.Name.Should().Be("MockProvider");
    }

    [Fact]
    public void SupportedModelTypes_ShouldReturnCorrectTypes()
    {
        // Act
        var types = _provider.SupportedModelTypes.ToList();

        // Assert
        types.Should().HaveCount(2);
        types.Should().Contain(Enums.ModelType.TextGeneration);
        types.Should().Contain(Enums.ModelType.CodeGeneration);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_ShouldReturnModels()
    {
        // Act
        var models = await _provider.GetAvailableModelsAsync();

        // Assert
        models.Should().NotBeNull();
        models.Should().HaveCount(2);
        
        var modelList = models.ToList();
        modelList.Should().Contain(m => m.Name == "mock-text-model");
        modelList.Should().Contain(m => m.Name == "mock-code-model");
    }

    [Fact]
    public async Task LoadModelAsync_ShouldReturnModel_WhenModelExists()
    {
        // Act
        var model = await _provider.LoadModelAsync("mock-text-model");

        // Assert
        model.Should().NotBeNull();
        model.ModelId.Should().Be("mock-text-model");
        model.Name.Should().Be("Mock Text Generation Model");
        model.ModelType.Should().Be(Enums.ModelType.TextGeneration);
    }

    [Fact]
    public async Task LoadModelAsync_ShouldThrow_WhenModelDoesNotExist()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.LoadModelAsync("non-existent-model"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResponse()
    {
        // Arrange
        var request = new ModelRequest
        {
            Input = "Generate iteration code for processing items",
            Temperature = 0.7,
            MaxTokens = 1000
        };

        // Act
        var response = await _provider.ExecuteAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Response.Should().NotBeNullOrEmpty();
        response.InputTokens.Should().BeGreaterThan(0);
        response.OutputTokens.Should().BeGreaterThan(0);
        response.ProcessingTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnIterationCode_WhenInputContainsIterationKeywords()
    {
        // Arrange
        var request = new ModelRequest
        {
            Input = "Generate optimized foreach loop for processing items"
        };

        // Act
        var response = await _provider.ExecuteAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Response.Should().Contain("foreach");
        response.Response.Should().Contain("items");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnCodeResponse_WhenInputContainsCodeKeywords()
    {
        // Arrange
        var request = new ModelRequest
        {
            Input = "Generate C# code for processing data"
        };

        // Act
        var response = await _provider.ExecuteAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Response.Should().Contain("class");
        response.Response.Should().Contain("public");
    }

    [Fact]
    public async Task GetHealthStatusAsync_ShouldReturnHealthyStatus()
    {
        // Act
        var status = await _provider.GetHealthStatusAsync();

        // Assert
        status.Should().NotBeNull();
        status.IsHealthy.Should().BeTrue();
        status.Status.Should().Be("Mock provider is healthy");
        status.ResponseTimeMs.Should().Be(50);
        status.ErrorRate.Should().Be(0.0);
        status.Metrics.Should().NotBeNull();
        status.Metrics.Should().ContainKey("mock_metric");
    }

    [Fact]
    public async Task Model_LoadAsync_ShouldSetIsLoadedToTrue()
    {
        // Arrange
        var model = await _provider.LoadModelAsync("mock-text-model");

        // Act
        await model.LoadAsync();

        // Assert
        model.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task Model_UnloadAsync_ShouldSetIsLoadedToFalse()
    {
        // Arrange
        var model = await _provider.LoadModelAsync("mock-text-model");
        await model.LoadAsync();

        // Act
        await model.UnloadAsync();

        // Assert
        model.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task Model_ProcessAsync_ShouldThrow_WhenNotLoaded()
    {
        // Arrange
        var model = await _provider.LoadModelAsync("mock-text-model");
        var request = new ModelRequest { Input = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => model.ProcessAsync(request));
    }

    [Fact]
    public async Task Model_ProcessAsync_ShouldReturnResponse_WhenLoaded()
    {
        // Arrange
        var model = await _provider.LoadModelAsync("mock-text-model");
        await model.LoadAsync();
        var request = new ModelRequest { Input = "Test input" };

        // Act
        var response = await model.ProcessAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Response.Should().Contain("Mock response");
        response.Response.Should().Contain("Test input");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
