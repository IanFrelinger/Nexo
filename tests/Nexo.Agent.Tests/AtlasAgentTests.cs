using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Agent.Contracts;
using Nexo.Agent.Implementations;
using Nexo.Agent.Models;
using Xunit;

namespace Nexo.Agent.Tests;

public class AtlasAgentTests
{
    private readonly Mock<ILogger<AtlasAgent>> _mockLogger;
    private readonly Mock<IAgentPlanner> _mockPlanner;
    private readonly Mock<IToolBroker> _mockToolBroker;
    private readonly Mock<IToolRegistry> _mockToolRegistry;
    private readonly Mock<IToolFactory> _mockToolFactory;
    private readonly AtlasAgent _agent;

    public AtlasAgentTests()
    {
        _mockLogger = new Mock<ILogger<AtlasAgent>>();
        _mockPlanner = new Mock<IAgentPlanner>();
        _mockToolBroker = new Mock<IToolBroker>();
        _mockToolRegistry = new Mock<IToolRegistry>();
        _mockToolFactory = new Mock<IToolFactory>();

        _agent = new AtlasAgent(
            _mockLogger.Object,
            _mockPlanner.Object,
            _mockToolBroker.Object,
            _mockToolRegistry.Object,
            _mockToolFactory.Object
        );
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act & Assert
        _agent.Status.Should().Be(AgentStatus.Idle);
        _agent.Mode.Should().Be(AgentMode.Off);
    }

    [Fact]
    public void SetMode_ShouldChangeMode()
    {
        // Act
        _agent.SetMode(AgentMode.Hybrid);

        // Assert
        _agent.Mode.Should().Be(AgentMode.Hybrid);
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithValidGoal_ShouldReturnSuccess()
    {
        // Arrange
        var goal = "Read a file and summarize it";
        var availableTools = new List<ToolManifest>
        {
            ToolManifest.Create("tool.file.read", "File Read", "Read files"),
            ToolManifest.Create("tool.text.summarize", "Summarize", "Summarize text")
        };

        var plan = Plan.Create(goal, null, new List<ToolInvocation>
        {
            ToolInvocation.Create(1, "tool.file.read", "{}", "Read file"),
            ToolInvocation.Create(2, "tool.text.summarize", "{}", "Summarize content", new[] { 1 })
        });

        _mockToolRegistry.Setup(x => x.GetAllTools()).Returns(availableTools);
        _mockPlanner.Setup(x => x.CreatePlanAsync(goal, null, availableTools, AgentMode.Off, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockPlanner.Setup(x => x.IdentifyMissingTools(plan, availableTools))
            .Returns(Array.Empty<ToolRequest>());

        _mockToolBroker.Setup(x => x.ExecuteToolAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ToolContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolExecutionResult(true, "Success", null, TimeSpan.FromMilliseconds(100), "tool"));

        // Act
        var result = await _agent.ExecuteTaskAsync(goal);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");
        result.Plan.Should().NotBeNull();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithMissingTools_ShouldReturnFailure()
    {
        // Arrange
        var goal = "Create a ZIP file";
        var availableTools = new List<ToolManifest>();
        var plan = Plan.Create(goal, null, new List<ToolInvocation>
        {
            ToolInvocation.Create(1, "tool.archive.zip", "{}", "Create ZIP")
        });

        var missingTools = new List<ToolRequest>
        {
            ToolRequest.Create("tool.archive.zip", "Archive ZIP", "Create ZIP files")
        };

        _mockToolRegistry.Setup(x => x.GetAllTools()).Returns(availableTools);
        _mockPlanner.Setup(x => x.CreatePlanAsync(goal, null, availableTools, AgentMode.Off, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockPlanner.Setup(x => x.IdentifyMissingTools(plan, availableTools))
            .Returns(missingTools);

        // Act
        var result = await _agent.ExecuteTaskAsync(goal);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Missing required tools");
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithEmptyPlan_ShouldReturnFailure()
    {
        // Arrange
        var goal = "Do something impossible";
        var availableTools = new List<ToolManifest>();
        var plan = Plan.Create(goal, null);

        _mockToolRegistry.Setup(x => x.GetAllTools()).Returns(availableTools);
        _mockPlanner.Setup(x => x.CreatePlanAsync(goal, null, availableTools, AgentMode.Off, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var result = await _agent.ExecuteTaskAsync(goal);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No steps could be planned");
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithException_ShouldReturnFailure()
    {
        // Arrange
        var goal = "Test exception handling";
        var availableTools = new List<ToolManifest>();

        _mockToolRegistry.Setup(x => x.GetAllTools()).Throws(new Exception("Registry error"));

        // Act
        var result = await _agent.ExecuteTaskAsync(goal);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Error:");
    }

    private void Dispose()
    {
        _agent.Dispose();
    }
}
