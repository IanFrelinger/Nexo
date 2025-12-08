using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination.Conflicts;
using System.Text.Json;
using Xunit;

namespace Nexo.Tests.Orchestration.Coordination;

public class ConflictDetectorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConflictDetector> _logger;

    public ConflictDetectorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<ConflictDetector>>();
    }

    [Fact]
    public void DetectConflicts_ResourceConflict_DetectsExcessiveResources()
    {
        // Arrange
        var detector = new ConflictDetector(_logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var spec1 = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system",
            ResourceRequirements = new ResourceRequirements
            {
                EstimatedComputeSeconds = 2000
            }
        };

        var spec2 = new AgentSpawnSpec
        {
            AgentId = "agent-2",
            Domain = "Economy",
            Goal = "Design economy",
            ResourceRequirements = new ResourceRequirements
            {
                EstimatedComputeSeconds = 2000
            }
        };

        var container1 = factory.CreateContainer(spec1);
        var container2 = factory.CreateContainer(spec2);

        // Act
        var conflicts = detector.DetectConflicts(new[] { container1, container2 });

        // Assert
        conflicts.Should().Contain(c => c.ConflictType == ConflictType.Resource);
    }

    [Fact]
    public void DetectConflicts_ConstraintConflict_DetectsContradictoryConstraints()
    {
        // Arrange
        var detector = new ConflictDetector(_logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var spec1 = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system",
            Constraints = new[]
            {
                new AgentConstraint
                {
                    Type = "Performance",
                    Description = "Must be fast"
                }
            }
        };

        var spec2 = new AgentSpawnSpec
        {
            AgentId = "agent-2",
            Domain = "Economy",
            Goal = "Design economy",
            Constraints = new[]
            {
                new AgentConstraint
                {
                    Type = "Performance",
                    Description = "Must be slow"
                }
            }
        };

        var container1 = factory.CreateContainer(spec1);
        var container2 = factory.CreateContainer(spec2);

        // Act
        var conflicts = detector.DetectConflicts(new[] { container1, container2 });

        // Assert
        conflicts.Should().Contain(c => c.ConflictType == ConflictType.Constraint);
    }

    [Fact]
    public void DetectConflicts_NoConflicts_ReturnsEmpty()
    {
        // Arrange
        var detector = new ConflictDetector(_logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var spec1 = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system"
        };

        var spec2 = new AgentSpawnSpec
        {
            AgentId = "agent-2",
            Domain = "Economy",
            Goal = "Design economy"
        };

        var container1 = factory.CreateContainer(spec1);
        var container2 = factory.CreateContainer(spec2);

        // Act
        var conflicts = detector.DetectConflicts(new[] { container1, container2 });

        // Assert
        conflicts.Should().BeEmpty();
    }
}

