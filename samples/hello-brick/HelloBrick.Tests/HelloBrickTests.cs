using FluentAssertions;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using HelloBrick;
using Xunit;

namespace HelloBrick.Tests;

/// <summary>Tests for hello brick.</summary>
public sealed class HelloBrickTests
{
    [Fact]
    public async Task ExecuteAsync_returns_expected_message()
    {
        var brick = new HelloBrick();
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["name"] = "Ashlar"
        });

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            new TestExecutionContext());

        output.Get<string>("message").Should().Be("Hello, Ashlar!");
        output.Get<string>("implementation").Should().Be(nameof(ImplementationType.Deterministic));
        output.Summary.Should().Contain("Ashlar");
    }

    /// <summary>Test execution context.</summary>
    private sealed class TestExecutionContext : IExecutionContext
    {
        public string AgentId => "test-agent";
        public string BehaviorId => "test-behavior";
        public bool IsAirGapped => true;
        public bool AuditMode => true;
        public string Provider => "test";
        /// <summary>Variables.</summary>
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
