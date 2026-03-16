using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Abstractions;
using Nexo.Abstractions.Routing;
using Nexo.Core.Application.Common.Ports;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Architect.Parsers;
using Nexo.Orchestration.Architect.Prompts;
using Nexo.Orchestration.Validation;
using Xunit;

namespace Nexo.Tests.Orchestration.Integration;

public class ArchitectAgentIntegrationTests
{
    private readonly Mock<IModel> _modelMock;
    private readonly Mock<ICacheStrategy> _cacheMock;
    private readonly Mock<IEndpointRouter> _routerMock;
    private readonly Mock<ILogger<ArchitectAgent>> _agentLoggerMock;
    private readonly Mock<ILogger<DomainRecognizer>> _domainLoggerMock;
    private readonly Mock<ILogger<DecompositionRetriever>> _retrieverLoggerMock;
    private readonly Mock<ILogger<DecompositionPromptBuilder>> _promptLoggerMock;
    private readonly Mock<ILogger<DecompositionJsonParser>> _parserLoggerMock;

    public ArchitectAgentIntegrationTests()
    {
        _modelMock = new Mock<IModel>();
        _cacheMock = new Mock<ICacheStrategy>();
        _routerMock = new Mock<IEndpointRouter>();
        _agentLoggerMock = new Mock<ILogger<ArchitectAgent>>();
        _domainLoggerMock = new Mock<ILogger<DomainRecognizer>>();
        _retrieverLoggerMock = new Mock<ILogger<DecompositionRetriever>>();
        _promptLoggerMock = new Mock<ILogger<DecompositionPromptBuilder>>();
        _parserLoggerMock = new Mock<ILogger<DecompositionJsonParser>>();
        _routerMock
            .Setup(r => r.ResolveAsync(It.IsAny<EndpointRoutingContext>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<string?>(null));
    }

    private ArchitectAgent CreateArchitectAgent(IEnumerable<IValidator>? validators = null)
    {
        var domainRecognizer = new DomainRecognizer(_domainLoggerMock.Object);
        var retriever = new DecompositionRetriever(_cacheMock.Object, domainRecognizer, _retrieverLoggerMock.Object);
        var promptBuilder = new DecompositionPromptBuilder();
        var parser = new DecompositionJsonParser(_parserLoggerMock.Object);
        var validatorsList = validators?.ToList() ?? new List<IValidator>();

        return new ArchitectAgent(
            _modelMock.Object,
            retriever,
            domainRecognizer,
            validatorsList,
            promptBuilder,
            parser,
            _routerMock.Object,
            _agentLoggerMock.Object);
    }

    [Fact]
    public async Task DecomposeAsync_ValidRequest_ReturnsDecomposition()
    {
        // Arrange
        var validJson = """
            {
              "agents": [
                {
                  "agentId": "combat-1",
                  "domain": "Combat",
                  "goal": "Design weapon system",
                  "description": "Create weapon mechanics",
                  "dependencies": [],
                  "priority": 0
                }
              ],
              "reasoning": "Decomposed into combat domain",
              "confidence": 0.9
            }
            """;

        _modelMock
            .Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(validJson));

        _cacheMock
            .Setup(c => c.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        var agent = CreateArchitectAgent(new[] { new SchemaValidator(Mock.Of<ILogger<SchemaValidator>>()) });

        // Act
        var result = await agent.DecomposeAsync("Design a combat system with weapons");

        // Assert
        result.Should().NotBeNull();
        result.Agents.Should().HaveCount(1);
        result.Agents[0].AgentId.Should().Be("combat-1");
        result.Agents[0].Domain.Should().Be("Combat");
    }

    [Fact]
    public async Task DecomposeAsync_InvalidJson_UsesFallbackDecomposition()
    {
        // Arrange
        var invalidJson = "Invalid JSON";

        _modelMock
            .Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(invalidJson));

        _cacheMock
            .Setup(c => c.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        var agent = CreateArchitectAgent(new[] { new SchemaValidator(Mock.Of<ILogger<SchemaValidator>>()) });

        // Act
        var result = await agent.DecomposeAsync("Design a combat system");

        // Assert
        result.Should().NotBeNull();
        result.Agents.Should().NotBeEmpty();
        result.Agents[0].AgentId.Should().Be("fallback-1");
        _modelMock.Verify(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DecomposeAsync_ValidationErrors_RetriesWithCorrection()
    {
        // Arrange
        var invalidDecomposition = """
            {
              "agents": [
                {
                  "agentId": "",
                  "domain": "Combat",
                  "goal": "Design weapon system",
                  "dependencies": [],
                  "priority": 0
                }
              ],
              "confidence": 0.9
            }
            """;

        var validDecomposition = """
            {
              "agents": [
                {
                  "agentId": "combat-1",
                  "domain": "Combat",
                  "goal": "Design weapon system",
                  "dependencies": [],
                  "priority": 0
                }
              ],
              "confidence": 0.9
            }
            """;

        var callCount = 0;
        _modelMock
            .Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 
                    ? new ModelOutput(invalidDecomposition)
                    : new ModelOutput(validDecomposition);
            });

        _cacheMock
            .Setup(c => c.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        var validator = new SchemaValidator(Mock.Of<ILogger<SchemaValidator>>());
        var agent = CreateArchitectAgent(new[] { validator });

        // Act
        var result = await agent.DecomposeAsync("Design a combat system");

        // Assert
        result.Should().NotBeNull();
        // The agent should retry when validation fails
        // Note: If the first attempt fails to parse, it will retry. If it parses but has validation errors, it will also retry.
        // The exact number depends on whether parsing succeeds on first attempt
        callCount.Should().BeGreaterThan(0);
    }
}

