using FluentAssertions;
using Moq;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Observation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Observation;

public sealed class ObservationContextBrickTests
{
    [Fact]
    public void Interface_HasExpectedInputsAndOutputs()
    {
        var mockAssembler = new Mock<IContextAssembler>();
        var brick = new ObservationContextBrick(mockAssembler.Object);

        brick.Interface.Inputs.Should().HaveCount(2);
        brick.Interface.Inputs.Should().Contain(i => i.Name == "projectPath");
        brick.Interface.Inputs.Should().Contain(i => i.Name == "maxPatterns");
        brick.Interface.Outputs.Should().ContainSingle(o => o.Name == "context");
    }

    [Fact]
    public async Task ExecuteAsync_WithAssembler_ReturnsContext()
    {
        var expectedContext = new ObservationContext
        {
            Patterns = [],
            RecentEvents = [],
            Summary = "Test summary",
        };

        var mockAssembler = new Mock<IContextAssembler>();
        mockAssembler
            .Setup(a => a.AssembleContextAsync(It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContext);

        var brick = new ObservationContextBrick(mockAssembler.Object);
        var input = new BrickInput();
        input.Set("maxPatterns", 50);

        var mockContext = new Mock<IExecutionContext>();
        mockContext.Setup(c => c.AgentId).Returns("test");
        mockContext.Setup(c => c.BehaviorId).Returns("test-behavior");
        mockContext.Setup(c => c.IsAirGapped).Returns(false);
        mockContext.Setup(c => c.AuditMode).Returns(false);
        mockContext.Setup(c => c.Provider).Returns("test");
        mockContext.Setup(c => c.Variables).Returns(new Dictionary<string, object>());

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, mockContext.Object, CancellationToken.None);

        output.Should().NotBeNull();
        output.ToDictionary().Should().ContainKey("context");
        output.ToDictionary()["context"].Should().Be(expectedContext);
        output.Summary.Should().Contain("0 pattern");
    }
}
