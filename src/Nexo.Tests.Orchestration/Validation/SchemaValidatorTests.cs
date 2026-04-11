using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Validation;
using Xunit;

namespace Nexo.Tests.Orchestration.Validation;

public class SchemaValidatorTests
{
    private readonly Mock<ILogger<SchemaValidator>> _loggerMock;
    private readonly SchemaValidator _validator;

    public SchemaValidatorTests()
    {
        _loggerMock = new Mock<ILogger<SchemaValidator>>();
        _validator = new SchemaValidator(_loggerMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_ValidDecomposition_ReturnsNoErrors()
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
                    Description = "Create weapon mechanics"
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
    public async Task ValidateAsync_MissingAgentId_ReturnsError()
    {
        // Arrange
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "",
                    Domain = "Combat",
                    Goal = "Design weapon system"
                }
            },
            OriginalRequest = "Design a combat system"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Schema" && e.Message.Contains("agentId"));
    }

    [Fact]
    public async Task ValidateAsync_InvalidAgentIdFormat_ReturnsError()
    {
        // Arrange
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "agent with spaces!",
                    Domain = "Combat",
                    Goal = "Design weapon system"
                }
            },
            OriginalRequest = "Design a combat system"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Schema" && e.Message.Contains("invalid characters"));
    }

    [Fact]
    public async Task ValidateAsync_DuplicateAgentIds_ReturnsError()
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
                    AgentId = "agent-1",
                    Domain = "Economy",
                    Goal = "Design economy system"
                }
            },
            OriginalRequest = "Design game systems"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Schema" && e.Message.Contains("Duplicate agent ID"));
    }

    [Fact]
    public async Task ValidateAsync_InvalidDependency_ReturnsError()
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
                    Dependencies = new[] { "non-existent-agent" }
                }
            },
            OriginalRequest = "Design a combat system"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Schema" && e.Message.Contains("non-existent agent"));
    }

    [Fact]
    public async Task ValidateAsync_SelfDependency_ReturnsError()
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
                    Dependencies = new[] { "agent-1" }
                }
            },
            OriginalRequest = "Design a combat system"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Schema" && e.Message.Contains("cannot depend on itself"));
    }

    [Fact]
    public async Task ValidateAsync_NegativeResourceRequirements_ReturnsError()
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
                        EstimatedComputeSeconds = -100
                    }
                }
            },
            OriginalRequest = "Design a combat system"
        };

        // Act
        var errors = await _validator.ValidateAsync(result);

        // Assert
        errors.Should().Contain(e => e.ErrorType == "Schema" && e.Message.Contains("negative"));
    }

    [Fact]
    public async Task ValidateAsync_ReportsToNonExistentAgent_ReturnsError()
    {
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "agent-1",
                    Domain = "Combat",
                    Goal = "Execute mission",
                    ReportsToAgentId = "missing-commander"
                }
            },
            OriginalRequest = "Execute command chain"
        };

        var errors = await _validator.ValidateAsync(result);

        errors.Should().Contain(e => e.ErrorType == "Schema" && e.Message.Contains("reports to non-existent agent"));
    }

    [Fact]
    public async Task ValidateAsync_CommandChainWithDuplicates_ReturnsError()
    {
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "lead-1",
                    Domain = "Strategy",
                    Goal = "Lead mission"
                },
                new AgentSpawnSpec
                {
                    AgentId = "agent-1",
                    Domain = "Combat",
                    Goal = "Execute mission",
                    ReportsToAgentId = "lead-1",
                    CommandChain = new[] { "lead-1", "lead-1" }
                }
            },
            OriginalRequest = "Execute command chain"
        };

        var errors = await _validator.ValidateAsync(result);

        errors.Should().Contain(e => e.ErrorType == "Schema" && e.Message.Contains("command chain contains duplicates"));
    }
}

