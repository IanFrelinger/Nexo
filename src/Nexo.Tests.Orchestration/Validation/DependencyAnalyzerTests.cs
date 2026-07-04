using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Validation;
using Xunit;

namespace Nexo.Tests.Orchestration.Validation;

/// <summary>Tests for dependency analyzer.</summary>
public class DependencyAnalyzerTests
{
    private readonly Mock<ILogger<DependencyAnalyzer>> _loggerMock;
    private readonly DependencyAnalyzer _validator;

    public DependencyAnalyzerTests()
    {
        _loggerMock = new Mock<ILogger<DependencyAnalyzer>>();
        _validator = new DependencyAnalyzer(_loggerMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_NoDependencies_ReturnsNoErrors()
    {
        // Arrange
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "agent-1",
                    Domain = "Combat",
                    Goal = "Design weapon system"
                },
                new AgentSpawnSpec
                {
                    AgentId = "agent-2",
                    Domain = "Economy",
                    Goal = "Design economy system"
                }
            },
            OriginalRequest = "Design game systems"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_ValidDependencyChain_ReturnsNoErrors()
    {
        // Arrange
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "agent-1",
                    Domain = "Combat",
                    Goal = "Design weapon system"
                },
                new AgentSpawnSpec
                {
                    AgentId = "agent-2",
                    Domain = "Economy",
                    Goal = "Design economy system",
                    Dependencies = new[] { "agent-1" }
                }
            },
            OriginalRequest = "Design game systems"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_CircularDependency_ReturnsError()
    {
        // Arrange
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "agent-1",
                    Domain = "Combat",
                    Goal = "Design weapon system",
                    Dependencies = new[] { "agent-2" }
                },
                new AgentSpawnSpec
                {
                    AgentId = "agent-2",
                    Domain = "Economy",
                    Goal = "Design economy system",
                    Dependencies = new[] { "agent-1" }
                }
            },
            OriginalRequest = "Design game systems"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Dependency" && e.Message.Contains("Circular dependency"));
    }

    [Fact]
    public async Task ValidateAsync_ThreeWayCycle_ReturnsError()
    {
        // Arrange
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "agent-1",
                    Domain = "Combat",
                    Goal = "Design weapon system",
                    Dependencies = new[] { "agent-3" }
                },
                new AgentSpawnSpec
                {
                    AgentId = "agent-2",
                    Domain = "Economy",
                    Goal = "Design economy system",
                    Dependencies = new[] { "agent-1" }
                },
                new AgentSpawnSpec
                {
                    AgentId = "agent-3",
                    Domain = "AI",
                    Goal = "Design AI system",
                    Dependencies = new[] { "agent-2" }
                }
            },
            OriginalRequest = "Design game systems"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Dependency" && e.Message.Contains("Circular dependency"));
    }

    [Fact]
    public async Task ValidateAsync_DeepDependencyChain_ReturnsWarning()
    {
        // Arrange
        var agents = new List<AgentSpawnSpec>();
        for (int i = 0; i < 12; i++)
        {
            agents.Add(new AgentSpawnSpec
            {
                AgentId = $"agent-{i}",
                Domain = "Combat",
                Goal = $"Design system {i}",
                Dependencies = i > 0 ? new[] { $"agent-{i - 1}" } : Array.Empty<string>()
            });
        }

        var result = new DecompositionResult
        {
            Agents = agents,
            OriginalRequest = "Design complex system"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Dependency" && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public async Task ValidateAsync_CommandHierarchyCycle_ReturnsError()
    {
        // Arrange
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "commander-1",
                    Domain = "Strategy",
                    Goal = "Direct mission",
                    ReportsToAgentId = "lead-1"
                },
                new AgentSpawnSpec
                {
                    AgentId = "lead-1",
                    Domain = "Execution",
                    Goal = "Lead squad",
                    ReportsToAgentId = "commander-1"
                }
            },
            OriginalRequest = "Create mission command chain"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Dependency" && e.Message.Contains("Circular dependency"));
    }
}

