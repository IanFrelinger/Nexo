using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.BackgroundAgents.Objectives;
using Nexo.BackgroundAgents.Observations;
using Xunit;

namespace Nexo.Tests.BackgroundAgents;

/// <summary>
/// Exhaustive gap coverage for <see cref="SelfExtendRunnerAdapter"/>.
/// </summary>
public sealed class SelfExtendRunnerAdapterGapCoverageTests
{
    private static ToolCall Call(string id, object args) =>
        new(id, JsonDocument.Parse(JsonSerializer.Serialize(args)).RootElement);

    private static string ResponseWithCalls(string rationale, params (string id, string argsJson)[] calls)
    {
        var arr = string.Join(",", calls.Select(c => $"{{\"id\":\"{c.id}\",\"arguments\":{c.argsJson}}}"));
        return $"{{\"tool_calls\":[{arr}],\"rationale\":\"{rationale}\"}}";
    }

    private static string ResponseEmpty(string rationale) =>
        JsonSerializer.Serialize(new { tool_calls = Array.Empty<object>(), rationale });

    private static SelfExtendRunnerAdapter CreateAdapter(
        IModel model,
        string repoRoot,
        ObjectiveStore? objectives = null,
        IObservationStore? observations = null,
        IAggressivenessModeStore? modeStore = null)
    {
        return new SelfExtendRunnerAdapter(
            model,
            NullLogger<SelfExtendRunnerAdapter>.Instance,
            NullLoggerFactory.Instance,
            observations: observations ?? new JsonlObservationStore(Path.Combine(repoRoot, "obs.jsonl")),
            objectives: objectives,
            proposals: new ChangeProposalStore(Path.Combine(repoRoot, "forge")),
            modeStore: modeStore ?? new InMemoryAggressivenessModeStore());
    }

    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var actModel = () => new SelfExtendRunnerAdapter(
            null!,
            NullLogger<SelfExtendRunnerAdapter>.Instance,
            NullLoggerFactory.Instance);
        actModel.Should().Throw<ArgumentNullException>().WithParameterName("model");

        var actLogger = () => new SelfExtendRunnerAdapter(
            Mock.Of<IModel>(),
            null!,
            NullLoggerFactory.Instance);
        actLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");

        var actFactory = () => new SelfExtendRunnerAdapter(
            Mock.Of<IModel>(),
            NullLogger<SelfExtendRunnerAdapter>.Instance,
            null!);
        actFactory.Should().Throw<ArgumentNullException>().WithParameterName("loggerFactory");
    }

    [Fact]
    public async Task RunAsync_single_parameter_overload_delegates()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("idle")));

        var adapter = CreateAdapter(model.Object, repo.FullName);
        var result = await adapter.RunAsync(repo.FullName);

        result.Success.Should().BeTrue();
        result.StoppedReason.Should().Be("empty");
    }

    [Fact]
    public async Task RunAsync_objective_only_overload_delegates()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("pinned objective done")));

        var adapter = CreateAdapter(model.Object, repo.FullName);
        var result = await adapter.RunAsync(repo.FullName, objective: "Ship feature X");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_agent_name_overload_delegates()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("named agent done")));

        var adapter = CreateAdapter(model.Object, repo.FullName);
        var result = await adapter.RunAsync(repo.FullName, objective: "Task", agentName: "runtime-planner");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Claims_backlog_objective_when_none_explicit()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var objectives = new ObjectiveStore(Path.Combine(repo.FullName, "objectives"));
        objectives.Add(new ObjectiveDocument
        {
            Id = "backlog-1",
            Title = "Add tests",
            Body = "Cover the adapter.",
            Priority = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("no action")));

        var adapter = CreateAdapter(model.Object, repo.FullName, objectives);
        var result = await adapter.RunAsync(repo.FullName, objective: null, agentName: "planner");

        result.Success.Should().BeTrue();
        objectives.Find("backlog-1")!.Status.Should().Be(ObjectiveStatus.Pending);
    }

    [Fact]
    public async Task Swallows_objective_claim_failures()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var objectives = new Mock<IObjectiveStore>();
        objectives.Setup(o => o.ClaimNext(It.IsAny<string>())).Throws(new InvalidOperationException("store locked"));
        objectives.Setup(o => o.Location).Returns(Path.Combine(repo.FullName, "objectives"));

        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("continue anyway")));

        var adapter = new SelfExtendRunnerAdapter(
            model.Object,
            NullLogger<SelfExtendRunnerAdapter>.Instance,
            NullLoggerFactory.Instance,
            objectives: objectives.Object);

        var result = await adapter.RunAsync(repo.FullName);

        result.Success.Should().BeTrue();
        objectives.Verify(o => o.ClaimNext(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Completing_claimed_objective_skips_release()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var objectives = new ObjectiveStore(Path.Combine(repo.FullName, "objectives"));
        objectives.Add(new ObjectiveDocument
        {
            Id = "finish-me",
            Title = "Finish",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseWithCalls(
                "mark done",
                ("objective.complete", JsonSerializer.Serialize(new { id = "finish-me", note = "shipped" })))));

        var adapter = CreateAdapter(model.Object, repo.FullName, objectives);
        var result = await adapter.RunAsync(repo.FullName);

        result.Success.Should().BeTrue();
        objectives.Find("finish-me")!.Status.Should().Be(ObjectiveStatus.Done);
    }

    [Fact]
    public async Task Mid_cycle_cancellation_auto_blocks_claimed_objective()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Path.Combine(repo.FullName, "src"));
        var objectives = new ObjectiveStore(Path.Combine(repo.FullName, "objectives"));
        objectives.Add(new ObjectiveDocument
        {
            Id = "fragile",
            Title = "Fragile task",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var iteration = 0;
        var secondIterationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .Returns(async (ModelInput _, CancellationToken ct) =>
            {
                iteration++;
                if (iteration == 1)
                {
                    return new ModelOutput(ResponseWithCalls(
                        "write first",
                        ("repo.fs.write", JsonSerializer.Serialize(new
                        {
                            root = repo.FullName,
                            path = "src/Step.cs",
                            content = "namespace Step;",
                        }))));
                }

                secondIterationStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return new ModelOutput(ResponseEmpty("never"));
            });

        using var cts = new CancellationTokenSource();
        var adapter = CreateAdapter(model.Object, repo.FullName, objectives);
        var runTask = adapter.RunAsync(repo.FullName, cancellationToken: cts.Token);
        await secondIterationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();

        var result = await runTask;

        result.Success.Should().BeFalse();
        objectives.Find("fragile")!.Status.Should().Be(ObjectiveStatus.Blocked);
        objectives.Find("fragile")!.BlockedReason.Should().Be("cycle_crashed");
    }

    [Fact]
    public async Task Passive_mode_denied_writes_mark_cycle_unsuccessful()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Path.Combine(repo.FullName, "src"));

        var modeStore = new InMemoryAggressivenessModeStore();
        modeStore.SetMode(BackgroundAgentAggressivenessMode.Passive);

        var model = new Mock<IModel>();
        model.SetupSequence(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseWithCalls(
                "try direct write",
                ("repo.fs.write", JsonSerializer.Serialize(new { root = repo.FullName, path = "src/Direct.cs", content = "// blocked" })))))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("give up")));

        var adapter = CreateAdapter(model.Object, repo.FullName, modeStore: modeStore);
        var result = await adapter.RunAsync(repo.FullName, objective: "Write file");

        result.Success.Should().BeFalse();
        result.ToolCallsDenied.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Successful_write_records_paths_in_scratchpad()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Path.Combine(repo.FullName, "src"));

        var model = new Mock<IModel>();
        model.SetupSequence(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseWithCalls(
                "write coverage file",
                ("repo.fs.write", JsonSerializer.Serialize(new { root = repo.FullName, path = "src/Coverage.cs", content = "namespace X;" })))))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("done")));

        var adapter = CreateAdapter(model.Object, repo.FullName);
        var result = await adapter.RunAsync(repo.FullName, objective: null, agentName: "writer");

        result.Success.Should().BeTrue();
        result.ToolCallsExecuted.Should().BeGreaterThan(0);

        var scratchpad = Path.Combine(repo.FullName, ".nexo", "runtime-studio", "writer-notes.md");
        File.Exists(scratchpad).Should().BeTrue();
        File.ReadAllText(scratchpad).Should().Contain("src/Coverage.cs");
    }

    [Fact]
    public async Task Search_replace_write_path_is_captured_in_scratchpad()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var target = Path.Combine(repo.FullName, "src", "Patch.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, "before");

        var model = new Mock<IModel>();
        model.SetupSequence(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseWithCalls(
                "patch file",
                ("repo.fs.search_replace", JsonSerializer.Serialize(new { root = repo.FullName, path = "src/Patch.cs", find = "before", replace = "after" })))))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("patched")));

        var adapter = CreateAdapter(model.Object, repo.FullName);
        await adapter.RunAsync(repo.FullName, objective: null, agentName: "patcher");

        var scratchpad = Path.Combine(repo.FullName, ".nexo", "runtime-studio", "patcher-notes.md");
        File.ReadAllText(scratchpad).Should().Contain("src/Patch.cs");
    }

    [Fact]
    public async Task Loads_recent_observations_tail_and_repo_overview()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Path.Combine(repo.FullName, "src"));
        Directory.CreateDirectory(Path.Combine(repo.FullName, "bin"));
        File.WriteAllText(Path.Combine(repo.FullName, "README.md"), "# repo");

        var observations = new JsonlObservationStore(Path.Combine(repo.FullName, "obs.jsonl"));
        for (var i = 0; i < 15; i++)
        {
            observations.Append(new RuntimeObservation(
                DateTimeOffset.UtcNow.AddMinutes(-i),
                "tester",
                ObservationKind.Test,
                $"event-{i}"));
        }

        string? capturedPrompt = null;
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .Callback<ModelInput, CancellationToken>((input, _) =>
                capturedPrompt = string.Join('\n', input.Messages.Select(m => m.content)))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("surveyed")));

        var adapter = CreateAdapter(model.Object, repo.FullName, observations: observations);
        await adapter.RunAsync(repo.FullName, objective: "Survey repo");

        capturedPrompt.Should().NotBeNull();
        capturedPrompt.Should().Contain("RecentObservations");
        capturedPrompt.Should().Contain("event-14");
        capturedPrompt.Should().NotContain("event-0");
        capturedPrompt.Should().Contain("README.md");
    }

    [Fact]
    public async Task Swallows_unreadable_observation_store()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var observations = new Mock<IObservationStore>();
        observations.Setup(o => o.ReadSince(
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<ObservationKind?>(),
                It.IsAny<int?>()))
            .Throws(new IOException("unreadable"));
        observations.Setup(o => o.Location).Returns("mock");

        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("no obs")));

        var adapter = CreateAdapter(model.Object, repo.FullName, observations: observations.Object);
        var result = await adapter.RunAsync(repo.FullName);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Reuses_existing_scratchpad_tail_in_snapshot()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var scratchpad = Path.Combine(repo.FullName, ".nexo", "runtime-studio", "notes-agent-notes.md");
        Directory.CreateDirectory(Path.GetDirectoryName(scratchpad)!);
        PlannerScratchpad.Append(scratchpad, new ScratchpadEntry(
            DateTimeOffset.UtcNow.AddHours(-1),
            "notes-agent",
            Iterations: 1,
            ToolsExecuted: 0,
            ToolsDenied: 0,
            StoppedReason: "empty",
            Rationale: "prior cycle",
            WritePaths: Array.Empty<string>()));

        string? capturedPrompt = null;
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .Callback<ModelInput, CancellationToken>((input, _) =>
                capturedPrompt = string.Join('\n', input.Messages.Select(m => m.content)))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("continue")));

        var adapter = CreateAdapter(model.Object, repo.FullName);
        await adapter.RunAsync(repo.FullName, objective: null, agentName: "notes@agent/planner");

        capturedPrompt.Should().Contain("RecentNotes");
        capturedPrompt.Should().Contain("prior cycle");
    }

    [Fact]
    public async Task Default_agent_name_is_self_extend_when_blank()
    {
        var repo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(ResponseEmpty("default agent")));

        var adapter = CreateAdapter(model.Object, repo.FullName);
        await adapter.RunAsync(repo.FullName, objective: "Task", agentName: "   ");

        var scratchpad = Path.Combine(repo.FullName, ".nexo", "runtime-studio", "self-extend-notes.md");
        File.Exists(scratchpad).Should().BeTrue();
    }
}
