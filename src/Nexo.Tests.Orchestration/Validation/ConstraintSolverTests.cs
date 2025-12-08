using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Validation;
using Xunit;

namespace Nexo.Tests.Orchestration.Validation;

public class ConstraintSolverTests
{
    private readonly Mock<ILogger<ConstraintSolver>> _loggerMock;
    private readonly ConstraintSolver _validator;

    public ConstraintSolverTests()
    {
        _loggerMock = new Mock<ILogger<ConstraintSolver>>();
        _validator = new ConstraintSolver(_loggerMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_NoConstraints_ReturnsNoErrors()
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
                }
            },
            OriginalRequest = "Design a combat system"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_ContradictoryConstraints_ReturnsError()
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
                    Constraints = new[]
                    {
                        new AgentConstraint
                        {
                            Type = "Performance",
                            Description = "Must be fast and efficient"
                        },
                        new AgentConstraint
                        {
                            Type = "Performance",
                            Description = "Must be slow and thorough"
                        }
                    }
                }
            },
            OriginalRequest = "Design a combat system"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Constraint");
    }

    [Fact]
    public async Task ValidateAsync_ExcessiveResourceRequirements_ReturnsWarning()
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
                    ResourceRequirements = new ResourceRequirements
                    {
                        EstimatedComputeSeconds = 2000 // More than 1 hour
                    }
                },
                new AgentSpawnSpec
                {
                    AgentId = "agent-2",
                    Domain = "Economy",
                    Goal = "Design economy system",
                    ResourceRequirements = new ResourceRequirements
                    {
                        EstimatedComputeSeconds = 2000
                    }
                }
            },
            OriginalRequest = "Design game systems"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Constraint" && e.Message.Contains("exceeds 1 hour"));
    }

    [Fact]
    public async Task ValidateAsync_ExcessiveMemoryRequirements_ReturnsWarning()
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
                    ResourceRequirements = new ResourceRequirements
                    {
                        RequiredMemoryMB = 10000 // 10GB
                    }
                },
                new AgentSpawnSpec
                {
                    AgentId = "agent-2",
                    Domain = "Economy",
                    Goal = "Design economy system",
                    ResourceRequirements = new ResourceRequirements
                    {
                        RequiredMemoryMB = 10000
                    }
                }
            },
            OriginalRequest = "Design game systems"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Constraint" && e.Message.Contains("exceeds 16GB"));
    }
}

