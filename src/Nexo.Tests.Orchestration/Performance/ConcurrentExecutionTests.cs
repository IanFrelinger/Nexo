using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination;
using Xunit;

namespace Nexo.Tests.Orchestration.Performance;

/// <summary>
/// Performance tests for concurrent agent execution.
/// </summary>
public class ConcurrentExecutionTests
{
    private readonly IServiceProvider _serviceProvider;

    public ConcurrentExecutionTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task DependencyResolver_ConcurrentRegistrations_HandlesCorrectly()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<DependencyResolver>>();
        var resolver = new DependencyResolver(logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var containers = Enumerable.Range(1, 10)
            .Select(i => factory.CreateContainer(new AgentSpawnSpec
            {
                AgentId = $"agent-{i}",
                Domain = "Combat",
                Goal = $"Task {i}",
                Dependencies = i > 1 ? new[] { $"agent-{i - 1}" } : Array.Empty<string>()
            }))
            .ToList();

        // Act - Register concurrently
        var registrationTasks = containers.Select(c => Task.Run(() =>
        {
            resolver.RegisterAgent(c);
        }));

        await Task.WhenAll(registrationTasks);

        // Assert
        var executionOrder = resolver.GetExecutionOrder();
        executionOrder.Should().HaveCount(10);
        
        // Verify dependency order
        var orderList = executionOrder.ToList();
        for (int i = 1; i < 10; i++)
        {
            var index1 = orderList.IndexOf($"agent-{i - 1}");
            var index2 = orderList.IndexOf($"agent-{i}");
            index1.Should().BeLessThan(index2);
        }
    }

    [Fact]
    public async Task ResourceAllocator_ConcurrentAllocations_HandlesCorrectly()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ResourceAllocator>>();
        var budget = new ResourceBudget
        {
            MaxComputeSeconds = 10000
        };
        var allocator = new ResourceAllocator(logger, budget);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var containers = Enumerable.Range(1, 20)
            .Select(i => factory.CreateContainer(new AgentSpawnSpec
            {
                AgentId = $"agent-{i}",
                Domain = "Combat",
                Goal = $"Task {i}",
                ResourceRequirements = new ResourceRequirements
                {
                    EstimatedComputeSeconds = 100
                }
            }))
            .ToList();

        // Act - Allocate concurrently
        var allocationTasks = containers.Select(c => Task.Run(() =>
        {
            allocator.TryAllocate(c, out _);
        }));

        await Task.WhenAll(allocationTasks);

        // Assert
        var report = allocator.GetUsageReport();
        report.TotalAllocatedComputeSeconds.Should().Be(2000); // 20 * 100
        report.UtilizationCompute.Should().BeLessOrEqualTo(1.0);
    }

    [Fact]
    public async Task ProgressTracker_ConcurrentUpdates_HandlesCorrectly()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<ProgressTracker>>();
        var tracker = new ProgressTracker(logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var containers = Enumerable.Range(1, 50)
            .Select(i => factory.CreateContainer(new AgentSpawnSpec
            {
                AgentId = $"agent-{i}",
                Domain = "Combat",
                Goal = $"Task {i}"
            }))
            .ToList();

        // Act - Update progress concurrently
        var updateTasks = containers.Select(c => Task.Run(() =>
        {
            tracker.RecordProgress(c);
        }));

        await Task.WhenAll(updateTasks);

        // Assert
        var summary = tracker.GetSummary(containers);
        summary.TotalAgents.Should().Be(50);
    }
}

