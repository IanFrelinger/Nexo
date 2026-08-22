using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.BackgroundAgents.Objectives;
using Ashlar.BackgroundAgents.Observations;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Application.Testing.UseCases.RunTests;
using Ashlar.Core.Domain.Values;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents;

/// <summary>Tests for host runners gap coverage.</summary>
public class HostRunnersGapCoverageTests
{
    /// <summary>Call.</summary>
    /// <param name="id">Id.</param>
    /// <param name="args">Args.</param>
    private static ToolCall Call(string id, object args) =>
        new(id, JsonDocument.Parse(JsonSerializer.Serialize(args)).RootElement);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CodeAnalysisRunnerAdapter_rejects_invalid_path(string? path)
    {
        var adapter = new CodeAnalysisRunnerAdapter(
            Mock.Of<IServiceScopeFactory>(),
            NullLogger<CodeAnalysisRunnerAdapter>.Instance);

        var result = await adapter.RunAsync(path!);

        result.Success.Should().BeFalse();
        result.Summary.Should().Contain("Path");
    }

    [Fact]
    public async Task CodeAnalysisRunnerAdapter_runs_analysis_and_reports_violations()
    {
        var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        var analysis = new Mock<IAnalysisService>();
        analysis.Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AnalysisResult
            {
                HasViolations = true,
                TotalViolations = 2,
                Violations = new[]
                {
                    new Violation
                    {
                        Rule = "R1",
                        Message = "issue",
                        FilePath = "a.cs",
                        Severity = RiskLevel.High,
                    },
                },
            });

        var services = new ServiceCollection();
        services.AddSingleton(analysis.Object);
        await using var provider = services.BuildServiceProvider();
        var adapter = new CodeAnalysisRunnerAdapter(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CodeAnalysisRunnerAdapter>.Instance);

        var result = await adapter.RunAsync(tempDir.FullName);

        result.Success.Should().BeFalse();
        result.ViolationCount.Should().Be(2);
        result.Summary.Should().Contain("violation");
    }

    [Fact]
    public async Task CodeAnalysisRunnerAdapter_handles_analysis_exceptions()
    {
        var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        var analysis = new Mock<IAnalysisService>();
        analysis.Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var services = new ServiceCollection();
        services.AddSingleton(analysis.Object);
        await using var provider = services.BuildServiceProvider();
        var adapter = new CodeAnalysisRunnerAdapter(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CodeAnalysisRunnerAdapter>.Instance);

        var result = await adapter.RunAsync(tempDir.FullName);

        result.Success.Should().BeFalse();
        result.Summary.Should().Contain("Analysis failed");
    }

    [Fact]
    public async Task TestRunRunnerAdapter_maps_mediator_results()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestExecutionResult
            {
                TotalTests = 5,
                PassedTests = 4,
                FailedTests = 1,
                TotalDuration = TimeSpan.FromSeconds(1),
                Results = Array.Empty<TestResult>(),
            });

        var adapter = new TestRunRunnerAdapter(mediator.Object, NullLogger<TestRunRunnerAdapter>.Instance);
        var result = await adapter.RunAsync("cat=Unit");

        result.Success.Should().BeFalse();
        result.TotalTests.Should().Be(5);
        result.PassedTests.Should().Be(4);
        result.FailedTests.Should().Be(1);
    }

    [Fact]
    public async Task TestRunRunnerAdapter_handles_mediator_exceptions()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("runner down"));

        var adapter = new TestRunRunnerAdapter(mediator.Object, NullLogger<TestRunRunnerAdapter>.Instance);
        var result = await adapter.RunAsync(null);

        result.Success.Should().BeFalse();
        result.Summary.Should().Contain("Test run failed");
    }

    [Fact]
    public async Task SelfExtendRunnerAdapter_rejects_missing_repo()
    {
        var adapter = new SelfExtendRunnerAdapter(
            Mock.Of<IModel>(),
            NullLogger<SelfExtendRunnerAdapter>.Instance,
            NullLoggerFactory.Instance);

        var result = await adapter.RunAsync("/does/not/exist");

        result.Success.Should().BeFalse();
        result.Summary.Should().Contain("RepoRoot");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("missing")]
    public void BackgroundAgentAdapterValidation_rejects_bad_directories(string? path)
    {
        BackgroundAgentAdapterValidation.TryResolveDirectory(path, "Path", out var error)
            .Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BackgroundAgentAdapterValidation_accepts_existing_directory()
    {
        var temp = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        BackgroundAgentAdapterValidation.TryResolveDirectory(temp.FullName, "RepoRoot", out var error)
            .Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void RepoFsToolboxFactory_minimal_registers_core_tools()
    {
        var (tools, policies) = RepoFsToolboxFactory.CreateMinimal();
        var ids = tools.Schemas().Select(s => s.Id).ToList();
        ids.Should().Contain("repo.fs.list");
        ids.Should().Contain("repo.fs.read");
        ids.Should().Contain("repo.fs.write");
        // repo.tile_map.render is deliberately NOT built in. It is game-specific and
        // arrives through IToolSource/extraTools from the game package, like any other
        // domain tool. The kernel's default toolbox is domain-agnostic.
        ids.Should().NotContain("repo.tile_map.render");
        policies.Should().NotBeNull();
    }

    [Fact]
    public void RepoFsToolboxFactory_with_build_test_registers_forge_and_budget()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var observations = new JsonlObservationStore(Path.Combine(root, "obs.jsonl"));
        var proposals = new ChangeProposalStore(Path.Combine(root, "forge"));
        var modeStore = new InMemoryAggressivenessModeStore();

        var (tools, policies, budget) = RepoFsToolboxFactory.CreateWithBuildTest(
            observations,
            source: "planner",
            objectiveId: "obj-1",
            proposals,
            modeStore);

        var ids = tools.Schemas().Select(s => s.Id).ToList();
        ids.Should().Contain("dotnet.build");
        ids.Should().Contain("dotnet.test");
        ids.Should().Contain("forge.propose_change");
        ids.Should().Contain("forge.check_pr");
        ids.Should().Contain("forge.build");
        ids.Should().Contain("forge.test");
        policies.Should().NotBeNull();
        budget.Should().NotBeNull();
    }

    [Fact]
    public async Task ObservingTool_emits_build_observation_on_success()
    {
        var inner = new Mock<ITool>();
        inner.Setup(t => t.Id).Returns("dotnet.build");
        inner.Setup(t => t.Schema).Returns(new ToolSchema("dotnet.build", "build", "{}"));
        inner.Setup(t => t.InvokeAsync(It.IsAny<ToolCall>(), It.IsAny<WorldSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResult(new Ashlar.Tools.Dev.Deltas.RepoDelta(), new
            {
                ok = true,
                stdout = "Build succeeded.",
                stderr = "",
            }));

        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new JsonlObservationStore(Path.Combine(root, "obs.jsonl"));
        var projector = ObservingTool.DotnetProjector("agent-1", ObservationKind.Build, "obj-1");
        var tool = new ObservingTool(inner.Object, store, projector);

        var snap = WorldSnapshot.ForRepo(root);
        var call = Call("dotnet.build", new { root });
        var result = await tool.InvokeAsync(call, snap, CancellationToken.None);

        result.Payload.Should().NotBeNull();
        store.ReadSince().Should().ContainSingle(o => o.kind == ObservationKind.Build && o.summary.Contains("succeeded"));
    }

    [Fact]
    public async Task ObservingTool_emits_failure_summary_from_stdout()
    {
        var inner = new Mock<ITool>();
        inner.Setup(t => t.Id).Returns("dotnet.test");
        inner.Setup(t => t.Schema).Returns(new ToolSchema("dotnet.test", "test", "{}"));
        inner.Setup(t => t.InvokeAsync(It.IsAny<ToolCall>(), It.IsAny<WorldSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResult(new Ashlar.Tools.Dev.Deltas.RepoDelta(), new
            {
                ok = false,
                stdout = "Failed!  - Ashlar.Tests.Kernel.SomeTest\n   Assert.True() Failure",
                stderr = "",
            }));

        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new JsonlObservationStore(Path.Combine(root, "obs.jsonl"));
        var tool = new ObservingTool(
            inner.Object,
            store,
            ObservingTool.DotnetProjector("tester", ObservationKind.Test));

        await tool.InvokeAsync(Call("dotnet.test", new { root = root }), WorldSnapshot.ForRepo(root), CancellationToken.None);

        store.ReadSince().Should().ContainSingle(o =>
            o.kind == ObservationKind.Test &&
            o.summary.Contains("failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ObservingTool_swallows_store_failures()
    {
        var inner = new Mock<ITool>();
        inner.Setup(t => t.Id).Returns("dotnet.build");
        inner.Setup(t => t.Schema).Returns(new ToolSchema("dotnet.build", "build", "{}"));
        inner.Setup(t => t.InvokeAsync(It.IsAny<ToolCall>(), It.IsAny<WorldSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResult(new Ashlar.Tools.Dev.Deltas.RepoDelta(), new { ok = true, stdout = "", stderr = "" }));

        var store = new Mock<IObservationStore>();
        store.Setup(s => s.Append(It.IsAny<RuntimeObservation>())).Throws(new IOException("disk full"));
        store.Setup(s => s.Location).Returns("mock");

        var tool = new ObservingTool(
            inner.Object,
            store.Object,
            ObservingTool.DotnetProjector("agent", ObservationKind.Build));

        var act = () => tool.InvokeAsync(
            Call("dotnet.build", new { root = "." }),
            WorldSnapshot.ForRepo("."),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void PlannerScratchpad_loads_and_appends_tail()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "scratch.md");
        PlannerScratchpad.LoadTail(path).Should().BeEmpty();

        PlannerScratchpad.Append(path, new ScratchpadEntry(
            DateTimeOffset.UtcNow,
            "planner",
            Iterations: 2,
            ToolsExecuted: 3,
            ToolsDenied: 1,
            StoppedReason: "budget",
            Rationale: "done",
            WritePaths: new[] { "src/a.cs" }));

        PlannerScratchpad.LoadTail(path).Should().Contain("planner");
    }

    [Fact]
    public async Task ForgeProposeChangeTool_records_proposal()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = new ChangeProposalStore(Path.Combine(root, "forge"));
        var tool = new ForgeProposeChangeTool(store, () => "agent-1", () => "obj-1");
        var snap = WorldSnapshot.ForRepo(root);

        var result = await tool.InvokeAsync(
            Call("forge.propose_change", new
            {
                target_path = "src/New.cs",
                new_content = "namespace X; public sealed class New {}",
                summary = "add file",
                reason = "coverage",
            }),
            snap,
            CancellationToken.None);

        result.Payload.Should().NotBeNull();
        store.List().Should().ContainSingle(p => p.TargetPath == "src/New.cs");
    }

    [Fact]
    public async Task ForgeCheckPrTool_returns_not_found_and_existing_proposal()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new ChangeProposalStore(Path.Combine(root, "forge"));
        var tool = new ForgeCheckPrTool(store);

        var missing = await tool.InvokeAsync(Call("forge.check_pr", new { id = "missing" }), WorldSnapshot.ForRepo(root), CancellationToken.None);
        missing.Payload.Should().NotBeNull();

        var proposal = store.Add(new ChangeProposal
        {
            Id = "prop-1",
            TargetPath = "a.cs",
            NewContent = "x",
            Summary = "s",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var found = await tool.InvokeAsync(Call("forge.check_pr", new { id = proposal.Id }), WorldSnapshot.ForRepo(root), CancellationToken.None);
        found.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task ObjectiveCompleteTool_marks_claimed_objective_done()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new ObjectiveStore(Path.Combine(root, "objectives"));
        store.Add(new ObjectiveDocument
        {
            Id = "obj-1",
            Title = "Improve tests",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        store.ClaimNext("planner");

        var tool = new ObjectiveCompleteTool(store);
        var result = await tool.InvokeAsync(
            Call("objective.complete", new { id = "obj-1", note = "finished" }),
            WorldSnapshot.ForRepo(root),
            CancellationToken.None);

        result.Payload.Should().NotBeNull();
        store.Find("obj-1")!.Status.Should().Be(ObjectiveStatus.Done);
    }

    [Fact]
    public async Task SelfExtendRunnerAdapter_runs_react_cycle_with_mock_model()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("RATIONALE: inspect repo\nNo tool calls this cycle."));

        var objectives = new ObjectiveStore(Path.Combine(repo.FullName, "objectives"));
        objectives.Add(new ObjectiveDocument
        {
            Id = "cov-obj",
            Title = "Improve coverage",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var adapter = new SelfExtendRunnerAdapter(
            model.Object,
            NullLogger<SelfExtendRunnerAdapter>.Instance,
            NullLoggerFactory.Instance,
            observations: new JsonlObservationStore(Path.Combine(repo.FullName, "obs.jsonl")),
            objectives: objectives,
            proposals: new ChangeProposalStore(Path.Combine(repo.FullName, "forge")),
            modeStore: new InMemoryAggressivenessModeStore());

        var result = await adapter.RunAsync(repo.FullName);

        result.Success.Should().BeTrue();
        result.Iterations.Should().BeGreaterThan(0);
        result.StoppedReason.Should().Be("empty");
    }

    [Fact]
    public async Task SelfExtendRunnerAdapter_rejects_missing_repo_root()
    {
        var adapter = new SelfExtendRunnerAdapter(
            Mock.Of<IModel>(),
            NullLogger<SelfExtendRunnerAdapter>.Instance,
            NullLoggerFactory.Instance);

        var result = await adapter.RunAsync("   ");

        result.Success.Should().BeFalse();
        result.Summary.Should().Contain("RepoRoot");
    }

    [Fact]
    public async Task SelfExtendRunnerAdapter_runs_with_explicit_objective_and_agent_name()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("RATIONALE: done\nNo tool calls this cycle."));

        var adapter = new SelfExtendRunnerAdapter(
            model.Object,
            NullLogger<SelfExtendRunnerAdapter>.Instance,
            NullLoggerFactory.Instance,
            observations: new JsonlObservationStore(Path.Combine(repo.FullName, "obs.jsonl")));

        var result = await adapter.RunAsync(
            repo.FullName,
            objective: "Improve test coverage",
            agentName: "coverage-extender",
            modelProvider: "offline",
            modelName: "mock");

        result.Success.Should().BeTrue();
        result.Iterations.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ObjectiveBlockTool_marks_objective_blocked()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new ObjectiveStore(Path.Combine(root, "objectives"));
        store.Add(new ObjectiveDocument
        {
            Id = "obj-2",
            Title = "Blocked work",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var tool = new ObjectiveBlockTool(store);
        var result = await tool.InvokeAsync(
            Call("objective.block", new { id = "obj-2", reason = "needs credentials" }),
            WorldSnapshot.ForRepo(root),
            CancellationToken.None);

        result.Payload.Should().NotBeNull();
        store.Find("obj-2")!.Status.Should().Be(ObjectiveStatus.Blocked);
        store.Find("obj-2")!.BlockedReason.Should().Be("needs credentials");
    }
}
