using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Abstractions.Execution;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Architect.Parsers;
using Xunit;

namespace Nexo.Tests.Orchestration.Architect;

/// <summary>Tests for decomposition json parser execution isolation.</summary>
public sealed class DecompositionJsonParserExecutionIsolationTests
{
    private static DecompositionJsonParser CreateParser()
    {
        var logger = Mock.Of<ILogger<DecompositionJsonParser>>();
        /// <summary>Decomposition json parser.</summary>
        return new DecompositionJsonParser(logger);
    }

    [Theory]
    [InlineData("InProcess", AgentExecutionIsolationLevel.InProcess)]
    [InlineData("inprocess", AgentExecutionIsolationLevel.InProcess)]
    [InlineData("ContainerPerAgent", AgentExecutionIsolationLevel.ContainerPerAgent)]
    [InlineData("container_per_agent", AgentExecutionIsolationLevel.InProcess)] // invalid token -> default
    [InlineData("OutOfProcess", AgentExecutionIsolationLevel.OutOfProcess)]
    [InlineData("CONTAINERPOOLED", AgentExecutionIsolationLevel.ContainerPooled)]
    public void Parse_SetsExecutionIsolation_FromString(string raw, AgentExecutionIsolationLevel expected)
    {
        var json = $$"""
            {
              "agents": [
                {
                  "agentId": "a1",
                  "domain": "General",
                  "goal": "Do work",
                  "executionIsolation": "{{raw}}"
                }
              ]
            }
            """;

        var result = CreateParser().Parse(json, "req");

        result.Should().NotBeNull();
        result!.Agents.Should().ContainSingle();
        result.Agents[0].ExecutionIsolation.Should().Be(expected);
    }

    [Fact]
    public void Parse_SetsExecutionIsolation_FromNumericEnumValue()
    {
        var json = """
            {
              "agents": [
                {
                  "agentId": "a1",
                  "domain": "General",
                  "goal": "Do work",
                  "executionIsolation": 3
                }
              ]
            }
            """;

        var result = CreateParser().Parse(json, "req");

        result.Should().NotBeNull();
        result!.Agents.Should().ContainSingle();
        result.Agents[0].ExecutionIsolation.Should().Be(AgentExecutionIsolationLevel.ContainerPerAgent);
    }

    [Fact]
    public void Parse_WhenExecutionIsolationOmitted_UsesInProcess()
    {
        var json = """
            {
              "agents": [
                {
                  "agentId": "a1",
                  "domain": "General",
                  "goal": "Do work"
                }
              ]
            }
            """;

        var result = CreateParser().Parse(json, "req");

        result.Should().NotBeNull();
        result!.Agents.Should().ContainSingle();
        result.Agents[0].ExecutionIsolation.Should().Be(AgentExecutionIsolationLevel.InProcess);
    }

    [Fact]
    public void Parse_WhenExecutionIsolationNumericInvalid_UsesInProcess()
    {
        var json = """
            {
              "agents": [
                {
                  "agentId": "a1",
                  "domain": "General",
                  "goal": "Do work",
                  "executionIsolation": 99
                }
              ]
            }
            """;

        var result = CreateParser().Parse(json, "req");

        result.Should().NotBeNull();
        result!.Agents.Should().ContainSingle();
        result.Agents[0].ExecutionIsolation.Should().Be(AgentExecutionIsolationLevel.InProcess);
    }
}
