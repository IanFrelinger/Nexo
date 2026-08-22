using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents.CodeGeneration;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using Xunit;

namespace Ashlar.Tests.Orchestration;

/// <summary>Tests for orchestration code generation gap coverage.</summary>
public class OrchestrationCodeGenerationGapCoverageTests
{
    [Fact]
    public async Task CodeGenerationAgent_generates_placeholder_for_infrastructure_domain()
    {
        var agent = CreateAgent(new AgentSpawnSpec
        {
            AgentId = "infra-gen",
            Domain = "Infrastructure",
            Goal = "Deploy service",
            Description = "yaml pipeline",
        });

        await agent.InitializeAsync();
        var result = (CodeGenerationResult)await agent.ExecuteAsync();

        result.Language.Should().Be("yaml");
        result.Code.Should().Contain("Deploy service");
    }

    [Fact]
    public async Task CodeGenerationAgent_uses_model_and_optimizer_when_issues_present()
    {
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("```csharp\nvar password = \"secret\";\nThread.Sleep(1000);\n```"));

        var agent = CreateAgent(
            new AgentSpawnSpec
            {
                AgentId = "secure-gen",
                Domain = "Security",
                Goal = "Build auth singleton in csharp",
                Description = "Use singleton pattern and optimize performance",
                Constraints = new[]
                {
                    new AgentConstraint { Type = "performance", Description = "optimize latency", IsMandatory = false },
                },
            },
            model.Object,
            new CodeAnalyzer(NullLogger<CodeAnalyzer>.Instance),
            new CodeOptimizer(NullLogger<CodeOptimizer>.Instance));

        await agent.InitializeAsync();
        var result = (CodeGenerationResult)await agent.ExecuteAsync(new Dictionary<string, object>
        {
            ["upstream"] = JsonSerializer.SerializeToElement(new { code = "existing" }),
        });

        result.Code.Should().NotContain("Thread.Sleep");
        result.Analysis.Should().NotBeNull();
        result.Analysis!.Issues.Should().NotBeEmpty();
        result.Metadata["optimizedCodeLength"].Should().NotBeNull();
    }

    [Fact]
    public async Task CodeGenerationAgent_throws_when_security_constraint_violated()
    {
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("var api_key = \"leaked\";"));

        var agent = CreateAgent(
            new AgentSpawnSpec
            {
                AgentId = "blocked-gen",
                Domain = "Security",
                Goal = "Write csharp helper",
                Constraints = new[]
                {
                    new AgentConstraint { Type = "security", Description = "No secrets", IsMandatory = true },
                },
            },
            model.Object,
            new CodeAnalyzer(NullLogger<CodeAnalyzer>.Instance));

        await agent.InitializeAsync();
        var act = () => agent.ExecuteAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Security constraint violated*");
    }

    [Fact]
    public async Task CodeAnalyzer_detects_python_and_long_code_issues()
    {
        var analyzer = new CodeAnalyzer(NullLogger<CodeAnalyzer>.Instance);
        var longCode = string.Join('\n', Enumerable.Repeat("if True: pass", 520));
        var analysis = await analyzer.AnalyzeAsync(
            longCode + "\nexec('bad')\ntime.sleep(1)",
            "python");

        analysis.Issues.Should().Contain(i => i.Category == "Security");
        analysis.Issues.Should().Contain(i => i.Category == "Performance");
        analysis.Issues.Should().Contain(i => i.Category == "Quality");
        analysis.Complexity.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task CodeOptimizer_fixes_python_sleep_and_trims_blank_lines()
    {
        var analyzer = new CodeAnalyzer(NullLogger<CodeAnalyzer>.Instance);
        var code = "import time\n\ntime.sleep(1)\n\n\n\nprint('ok')";
        var analysis = await analyzer.AnalyzeAsync(code, "python");
        var optimized = await new CodeOptimizer(NullLogger<CodeOptimizer>.Instance)
            .OptimizeAsync(code, "python", analysis, CancellationToken.None);

        optimized.Should().Contain("asyncio.sleep");
        optimized.Should().NotContain("\n\n\n\n");
    }

    private static CodeGenerationAgent CreateAgent(
        AgentSpawnSpec spec,
        IModel? model = null,
        CodeAnalyzer? analyzer = null,
        CodeOptimizer? optimizer = null) =>
        new(
            spec,
            NullLogger<Ashlar.Orchestration.Agents.BaseAgent>.Instance,
            model,
            analyzer,
            optimizer);
}
