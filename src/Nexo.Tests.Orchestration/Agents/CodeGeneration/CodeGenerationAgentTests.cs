using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents.CodeGeneration;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using Xunit;

namespace Nexo.Tests.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Tests for CodeGenerationAgent, including placeholder code generation when model is null.
/// </summary>
public class CodeGenerationAgentTests
{
    [Fact]
    public async Task ExecuteAsync_WhenModelIsNull_ReturnsPlaceholderWithNotImplementedException_ForCSharp()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "codegen-1",
            Domain = "gameplay",
            Goal = "Create a health system"
        };

        var logger = LoggerFactory.Create(b => { }).CreateLogger<CodeGenerationAgent>();
        var agent = new CodeGenerationAgent(spec, logger, model: null);

        await agent.InitializeAsync();
        var result = await agent.ExecuteAsync();

        result.Should().NotBeNull();
        var codeResult = result.Should().BeOfType<CodeGenerationResult>().Subject;
        codeResult.Code.Should().Contain("NotImplementedException");
        codeResult.Code.Should().Contain("Implement: Create a health system");
    }

    [Fact]
    public async Task ExecuteAsync_WhenModelIsNull_ReturnsPlaceholderWithNotImplementedError_ForPython()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "codegen-2",
            Domain = "data",
            Goal = "python data processor"
        };

        var logger = LoggerFactory.Create(b => { }).CreateLogger<CodeGenerationAgent>();
        var agent = new CodeGenerationAgent(spec, logger, model: null);

        await agent.InitializeAsync();
        var result = await agent.ExecuteAsync();

        result.Should().NotBeNull();
        var codeResult = result.Should().BeOfType<CodeGenerationResult>().Subject;
        codeResult.Code.Should().Contain("NotImplementedError");
        codeResult.Code.Should().Contain("Implement:");
    }

    [Fact]
    public async Task ExecuteAsync_WhenModelIsNull_ReturnsPlaceholderWithThrowNewError_ForJavaScript()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "codegen-3",
            Domain = "web",
            Goal = "javascript api client"
        };

        var logger = LoggerFactory.Create(b => { }).CreateLogger<CodeGenerationAgent>();
        var agent = new CodeGenerationAgent(spec, logger, model: null);

        await agent.InitializeAsync();
        var result = await agent.ExecuteAsync();

        result.Should().NotBeNull();
        var codeResult = result.Should().BeOfType<CodeGenerationResult>().Subject;
        codeResult.Code.Should().Contain("throw new Error");
        codeResult.Code.Should().Contain("Implement:");
    }
}
