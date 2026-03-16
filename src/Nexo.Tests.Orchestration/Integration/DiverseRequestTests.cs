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

/// <summary>
/// Tests the Architect Agent with diverse request types to ensure it handles various scenarios.
/// </summary>
public class DiverseRequestTests
{
    private readonly Mock<IModel> _modelMock;
    private readonly Mock<ICacheStrategy> _cacheMock;
    private readonly Mock<IEndpointRouter> _routerMock;
    private readonly Mock<ILogger<ArchitectAgent>> _agentLoggerMock;
    private readonly Mock<ILogger<DomainRecognizer>> _domainLoggerMock;
    private readonly Mock<ILogger<DecompositionRetriever>> _retrieverLoggerMock;
    private readonly Mock<ILogger<DecompositionJsonParser>> _parserLoggerMock;

    public DiverseRequestTests()
    {
        _modelMock = new Mock<IModel>();
        _cacheMock = new Mock<ICacheStrategy>();
        _routerMock = new Mock<IEndpointRouter>();
        _agentLoggerMock = new Mock<ILogger<ArchitectAgent>>();
        _domainLoggerMock = new Mock<ILogger<DomainRecognizer>>();
        _retrieverLoggerMock = new Mock<ILogger<DecompositionRetriever>>();
        _parserLoggerMock = new Mock<ILogger<DecompositionJsonParser>>();
        _routerMock
            .Setup(r => r.ResolveAsync(It.IsAny<EndpointRoutingContext>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<string?>(null));
    }

    private ArchitectAgent CreateArchitectAgent()
    {
        var domainRecognizer = new DomainRecognizer(_domainLoggerMock.Object);
        var retriever = new DecompositionRetriever(_cacheMock.Object, domainRecognizer, _retrieverLoggerMock.Object);
        var promptBuilder = new DecompositionPromptBuilder();
        var parser = new DecompositionJsonParser(_parserLoggerMock.Object);
        var validators = new List<IValidator>
        {
            new SchemaValidator(Mock.Of<ILogger<SchemaValidator>>()),
            new DependencyAnalyzer(Mock.Of<ILogger<DependencyAnalyzer>>()),
            new CoverageChecker(Mock.Of<ILogger<CoverageChecker>>()),
            new ConstraintSolver(Mock.Of<ILogger<ConstraintSolver>>())
        };

        return new ArchitectAgent(
            _modelMock.Object,
            retriever,
            domainRecognizer,
            validators,
            promptBuilder,
            parser,
            _routerMock.Object,
            _agentLoggerMock.Object);
    }

    private void SetupModelResponse(string jsonResponse)
    {
        _modelMock
            .Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(jsonResponse));

        _cacheMock
            .Setup(c => c.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);
    }

    [Theory]
    [InlineData("Design an extraction shooter game with PvPvE combat, loot system, and AI enemies",
        "combat", "economy", "ai")]
    [InlineData("Build an e-commerce platform with secure payments and user authentication",
        "infrastructure", "security")]
    [InlineData("Create an AI-driven game with intelligent NPCs and quest system",
        "ai", "gameplay")]
    [InlineData("Implement authentication and authorization system",
        "security")]
    [InlineData("Set up cloud infrastructure with auto-scaling",
        "infrastructure")]
    [InlineData("Design combat system with weapons and damage",
        "combat")]
    [InlineData("Create in-game economy with currency and trading",
        "economy")]
    [InlineData("Implement multiplayer matchmaking and lobby system",
        "gameplay")]
    [InlineData("Design quest and mission system",
        "gameplay")]
    [InlineData("Create inventory management system",
        "gameplay")]
    [InlineData("Design crafting and recipe system",
        "gameplay")]
    [InlineData("Implement social features: friends, chat, and guilds",
        "gameplay")]
    public async Task DecomposeAsync_DiverseRequests_ReturnsValidDecomposition(
        string request, params string[] expectedDomains)
    {
        // Arrange
        var agents = expectedDomains.Select((domain, idx) => new
        {
            agentId = $"{domain.ToLowerInvariant()}-{idx + 1}",
            domain = char.ToUpperInvariant(domain[0]) + domain.Substring(1),
            goal = $"Design {domain} system"
        }).ToList();

        var jsonResponse = $@"{{
              ""agents"": [
                {string.Join(",", agents.Select(a => $@"{{
                  ""agentId"": ""{a.agentId}"",
                  ""domain"": ""{a.domain}"",
                  ""goal"": ""{a.goal}"",
                  ""dependencies"": [],
                  ""priority"": 0
                }}"))}
              ],
              ""reasoning"": ""Decomposed request into {agents.Count} agents"",
              ""confidence"": 0.9
            }}";

        SetupModelResponse(jsonResponse);
        var agent = CreateArchitectAgent();

        // Act
        var result = await agent.DecomposeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Agents.Should().NotBeEmpty();
        result.OriginalRequest.Should().Be(request);
        
        // Verify expected domains are present
        var resultDomains = result.Agents.Select(a => a.Domain.ToLowerInvariant()).ToList();
        foreach (var expectedDomain in expectedDomains)
        {
            resultDomains.Should().Contain(expectedDomain.ToLowerInvariant());
        }
    }

    [Fact]
    public async Task DecomposeAsync_ComplexMultiDomainRequest_HandlesDependencies()
    {
        // Arrange
        var request = "Design an extraction shooter with combat, economy, and AI systems";
        var jsonResponse = """
            {
              "agents": [
                {
                  "agentId": "combat-1",
                  "domain": "Combat",
                  "goal": "Design weapon and damage system",
                  "dependencies": [],
                  "priority": 0
                },
                {
                  "agentId": "economy-1",
                  "domain": "Economy",
                  "goal": "Design loot and extraction system",
                  "dependencies": ["combat-1"],
                  "priority": 0
                },
                {
                  "agentId": "ai-1",
                  "domain": "AI",
                  "goal": "Design enemy AI behaviors",
                  "dependencies": ["combat-1"],
                  "priority": 0
                }
              ],
              "reasoning": "Combat is foundational, economy and AI depend on it",
              "confidence": 0.9
            }
            """;

        SetupModelResponse(jsonResponse);
        var agent = CreateArchitectAgent();

        // Act
        var result = await agent.DecomposeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Agents.Should().HaveCount(3);
        // Validation may produce warnings, but should not have critical schema or dependency errors for this valid structure
        var criticalErrors = result.ValidationErrors.Where(e => e.Severity == ValidationSeverity.Error && 
            (e.ErrorType == "Schema" || e.ErrorType == "Dependency")).ToList();
        criticalErrors.Should().BeEmpty();
        
        var economyAgent = result.Agents.First(a => a.AgentId == "economy-1");
        economyAgent.Dependencies.Should().Contain("combat-1");
    }

    [Fact]
    public async Task DecomposeAsync_SimpleSingleDomainRequest_ReturnsSingleAgent()
    {
        // Arrange
        var request = "Design a combat system";
        var jsonResponse = """
            {
              "agents": [
                {
                  "agentId": "combat-1",
                  "domain": "Combat",
                  "goal": "Design combat system",
                  "dependencies": [],
                  "priority": 0
                }
              ],
              "reasoning": "Single domain request",
              "confidence": 0.95
            }
            """;

        SetupModelResponse(jsonResponse);
        var agent = CreateArchitectAgent();

        // Act
        var result = await agent.DecomposeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Agents.Should().HaveCount(1);
        result.Agents[0].Domain.Should().Be("Combat");
        result.Confidence.Should().BeGreaterThan(0.9);
    }
}

