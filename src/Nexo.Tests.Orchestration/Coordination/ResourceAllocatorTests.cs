using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination;
using Xunit;

namespace Nexo.Tests.Orchestration.Coordination;

public class ResourceAllocatorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResourceAllocator> _logger;

    public ResourceAllocatorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<ResourceAllocator>>();
    }

    [Fact]
    public void TryAllocate_WithinBudget_AllocatesResources()
    {
        // Arrange
        var budget = new ResourceBudget
        {
            MaxComputeSeconds = 1000
        };
        var allocator = new ResourceAllocator(_logger, budget);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var spec = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system",
            ResourceRequirements = new ResourceRequirements
            {
                EstimatedComputeSeconds = 500
            }
        };

        var container = factory.CreateContainer(spec);

        // Act
        var success = allocator.TryAllocate(container, out var allocation);

        // Assert
        success.Should().BeTrue();
        allocation.Should().NotBeNull();
        allocation!.AllocatedComputeSeconds.Should().Be(500);
    }

    [Fact]
    public void TryAllocate_ExceedsBudget_ReturnsFalse()
    {
        // Arrange
        var budget = new ResourceBudget
        {
            MaxComputeSeconds = 1000
        };
        var allocator = new ResourceAllocator(_logger, budget);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var spec = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system",
            ResourceRequirements = new ResourceRequirements
            {
                EstimatedComputeSeconds = 2000
            }
        };

        var container = factory.CreateContainer(spec);

        // Act
        var success = allocator.TryAllocate(container, out var allocation);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public void ReleaseResources_FreesUpResources()
    {
        // Arrange
        var allocator = new ResourceAllocator(_logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var spec = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system",
            ResourceRequirements = new ResourceRequirements
            {
                EstimatedComputeSeconds = 500
            }
        };

        var container = factory.CreateContainer(spec);
        allocator.TryAllocate(container, out _);

        // Act
        allocator.ReleaseResources("agent-1");

        // Assert
        var report = allocator.GetUsageReport();
        report.TotalAllocatedComputeSeconds.Should().Be(0);
    }
}

