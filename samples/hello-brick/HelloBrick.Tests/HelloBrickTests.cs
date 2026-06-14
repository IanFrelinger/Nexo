using FluentAssertions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using HelloBrick;
using Xunit;

namespace HelloBrick.Tests;

public sealed class HelloBrickTests
{
    [Fact]
    public async Task ExecuteAsync_returns_expected_message()
    {
        var brick = new HelloBrick();
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["name"] = "Nexo"
        });

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            new TestExecutionContext());

        output.Get<string>("message").Should().Be("Hello, Nexo!");
        output.Get<string>("implementation").Should().Be(nameof(ImplementationType.Deterministic));
        output.Summary.Should().Contain("Nexo");
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string AgentId => "test-agent";
        public string BehaviorId => "test-behavior";
        public bool IsAirGapped => true;
        public bool AuditMode => true;
        public string Provider => "test";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
