using FluentAssertions;
using Ashlar.Abstractions.Execution;
using Xunit;

namespace Ashlar.Tests.Orchestration.Execution;

/// <summary>
/// Unit tests for <see cref="AgentExecutionIsolation"/> metadata helpers (wire contract for transports / hosts).
/// </summary>
public sealed class AgentExecutionIsolationTests
{
    public static TheoryData<AgentExecutionIsolationLevel> AllLevels() =>
        new()
        {
            AgentExecutionIsolationLevel.InProcess,
            AgentExecutionIsolationLevel.OutOfProcess,
            AgentExecutionIsolationLevel.ContainerPooled,
            AgentExecutionIsolationLevel.ContainerPerAgent,
        };

    [Theory]
    [MemberData(nameof(AllLevels))]
    public void Format_ReturnsEnumName(AgentExecutionIsolationLevel level)
    {
        AgentExecutionIsolation.Format(level).Should().Be(level.ToString("G"));
    }

    [Fact]
    public void TryParse_WhenMetadataNull_ReturnsFalse_AndOutIsInProcess()
    {
        var ok = AgentExecutionIsolation.TryParse(null, out var level);
        ok.Should().BeFalse();
        level.Should().Be(AgentExecutionIsolationLevel.InProcess);
    }

    [Fact]
    public void TryParse_WhenKeyMissing_ReturnsFalse()
    {
        var ok = AgentExecutionIsolation.TryParse(
            new Dictionary<string, string> { ["other"] = "x" },
            out var level);
        ok.Should().BeFalse();
        level.Should().Be(AgentExecutionIsolationLevel.InProcess);
    }

    [Fact]
    public void TryParse_WhenValueWhitespace_ReturnsFalse()
    {
        var ok = AgentExecutionIsolation.TryParse(
            new Dictionary<string, string> { [AgentExecutionIsolation.MetadataKey] = "  " },
            out _);
        ok.Should().BeFalse();
    }

    [Theory]
    [InlineData("ContainerPerAgent", AgentExecutionIsolationLevel.ContainerPerAgent)]
    [InlineData("containerperagent", AgentExecutionIsolationLevel.ContainerPerAgent)]
    [InlineData("OUTOFPROCESS", AgentExecutionIsolationLevel.OutOfProcess)]
    public void TryParse_WhenValid_ReturnsTrue(string raw, AgentExecutionIsolationLevel expected)
    {
        var ok = AgentExecutionIsolation.TryParse(
            new Dictionary<string, string> { [AgentExecutionIsolation.MetadataKey] = raw },
            out var level);
        ok.Should().BeTrue();
        level.Should().Be(expected);
    }

    [Fact]
    public void TryParse_WhenUnknownString_ReturnsFalse()
    {
        var ok = AgentExecutionIsolation.TryParse(
            new Dictionary<string, string> { [AgentExecutionIsolation.MetadataKey] = "NotAnIsolation" },
            out var level);
        ok.Should().BeFalse();
        level.Should().Be(AgentExecutionIsolationLevel.InProcess);
    }

    [Fact]
    public void GetEffective_WhenMissingKey_UsesFallback()
    {
        AgentExecutionIsolation
            .GetEffective(new Dictionary<string, string>(), AgentExecutionIsolationLevel.ContainerPooled)
            .Should()
            .Be(AgentExecutionIsolationLevel.ContainerPooled);
    }

    [Fact]
    public void GetEffective_WhenKeyPresent_OverridesFallback()
    {
        AgentExecutionIsolation
            .GetEffective(
                new Dictionary<string, string> { [AgentExecutionIsolation.MetadataKey] = "OutOfProcess" },
                AgentExecutionIsolationLevel.ContainerPerAgent)
            .Should()
            .Be(AgentExecutionIsolationLevel.OutOfProcess);
    }
}
