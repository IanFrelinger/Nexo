using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions.Execution;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Architect.Parsers;
using Nexo.Orchestration.Barriers;
using System.Text.Json;
using Xunit;

namespace Nexo.Tests.Orchestration;

/// <summary>Tests for orchestration architect gap coverage.</summary>
public class OrchestrationArchitectGapCoverageTests
{
    [Fact]
    public void DecompositionJsonParser_parses_markdown_json_with_full_agent_shape()
    {
        var parser = new DecompositionJsonParser(NullLogger<DecompositionJsonParser>.Instance);
        var json = """
            ```json
            {
              "agents": [
                {
                  "agentId": "lead",
                  "name": "Lead Agent",
                  "domain": "Planning",
                  "goal": "Plan game",
                  "goals": ["Plan game", "Coordinate"],
                  "description": "Lead planner",
                  "clusterId": "c1",
                  "chainOfCommand": ["architect"],
                  "commandChain": ["architect", "director"],
                  "ollamaModel": "llama3",
                  "dependencies": ["architect"],
                  "outputSchema": { "type": "object" },
                  "constraints": [
                    { "type": "Security", "description": "No secrets", "isMandatory": true }
                  ],
                  "resourceRequirements": {
                    "estimatedComputeSeconds": 30,
                    "requiredContextTokens": 4096,
                    "requiredMemoryMB": 512
                  },
                  "priority": 2,
                  "requiredCapabilities": ["planning"],
                  "metadata": { "tier": "core" },
                  "executionIsolation": "InProcess"
                }
              ],
              "reasoning": "split by domain",
              "confidence": 0.85
            }
            ```
            """;

        var result = parser.Parse(json, "build RPG");
        result.Should().NotBeNull();
        result!.Agents.Should().ContainSingle();
        var agent = result.Agents[0];
        agent.AgentId.Should().Be("lead");
        agent.Name.Should().Be("Lead Agent");
        agent.ReportsToAgentId.Should().Be("architect");
        agent.CommandChain.Should().Contain("director");
        agent.OllamaModel.Should().Be("llama3");
        agent.ResourceRequirements!.EstimatedComputeSeconds.Should().Be(30);
        agent.ExecutionIsolation.Should().Be(AgentExecutionIsolationLevel.InProcess);
        result.Confidence.Should().Be(0.85);
    }

    [Fact]
    public void DecompositionJsonParser_uses_fallback_for_empty_invalid_and_partial_agents()
    {
        var parser = new DecompositionJsonParser(NullLogger<DecompositionJsonParser>.Instance);

        parser.Parse("", "req")!.Agents.Should().ContainSingle(a => a.AgentId == "fallback-1");
        parser.Parse("{not json", "req")!.Reasoning.Should().Contain("Parser fallback");

        var partial = parser.Parse("""{"agents":[{"agentId":"","domain":"x","goal":"y"}]}""", "req");
        partial!.Agents.Should().BeEmpty();
    }

    [Fact]
    public void DecompositionJsonParser_handles_execution_isolation_variants()
    {
        var parser = new DecompositionJsonParser(NullLogger<DecompositionJsonParser>.Instance);
        var numeric = parser.Parse("""
            {"agents":[{"agentId":"a1","domain":"Test","goal":"g","executionIsolation":2}]}
            """, "req")!.Agents[0].ExecutionIsolation;

        numeric.Should().Be(AgentExecutionIsolationLevel.ContainerPooled);

        var invalid = parser.Parse("""
            {"agents":[{"agentId":"a2","domain":"Test","goal":"g","executionIsolation":"bogus"}]}
            """, "req")!.Agents[0].ExecutionIsolation;
        invalid.Should().Be(AgentExecutionIsolationLevel.InProcess);
    }

    [Fact]
    public void DomainPromptBuilder_includes_constraints_dependencies_and_schema()
    {
        var builder = new DomainPromptBuilder();
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"ok":{"type":"boolean"}}}""").RootElement;
        var prompt = builder.BuildExecutionPrompt(
            new AgentSpawnSpec
            {
                AgentId = "combat-1",
                Domain = "Combat",
                Goal = "Design combat loop",
                Description = "Fast-paced action",
                Constraints = new[] { new AgentConstraint { Type = "Balance", Description = "Keep TTK under 5s", IsMandatory = true } },
                OutputSchema = schema,
            },
            new Dictionary<string, object> { ["planner"] = new { plan = "phase 1" } });

        prompt.Should().Contain("Combat");
        prompt.Should().Contain("MANDATORY");
        prompt.Should().Contain("planner");
        prompt.Should().Contain("Expected Output Schema");
    }

    [Fact]
    public void DomainPromptBuilder_serializes_outputs_when_json_fails()
    {
        var builder = new DomainPromptBuilder();
        var prompt = builder.BuildExecutionPrompt(
            new AgentSpawnSpec { AgentId = "a1", Domain = "Test", Goal = "g" },
            new Dictionary<string, object> { ["dep"] = new Cyclic() });

        prompt.Should().Contain("dep:");
    }

    [Fact]
    public void Barrier_exceptions_expose_metadata()
    {
        var missing = new BarrierContextMissingException("agent-a", "corr-1");
        missing.AgentName.Should().Be("agent-a");
        missing.ErrorCode.Should().Be("BARRIER_CONTEXT_MISSING");

        var elevation = new BarrierElevationException("agent-b", "corr-2", "read", "write");
        elevation.CurrentLevel.Should().Be("read");
        elevation.RequestedLevel.Should().Be("write");
        elevation.ErrorCode.Should().Be("BARRIER_ELEVATION_DENIED");
    }

    /// <summary>Cyclic.</summary>
    private sealed class Cyclic
    {
        public Cyclic Self => this;
    }
}
