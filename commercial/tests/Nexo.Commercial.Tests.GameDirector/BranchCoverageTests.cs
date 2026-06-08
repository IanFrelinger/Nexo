using System.Text.Json;
using FluentAssertions;
using GameDirector.Bricks;
using GameDirector.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.GameDirector;

[Trait("Category", "GameDirectorApplication")]
public sealed class BranchCoverageTests
{
    [Fact]
    public async Task BalanceBrick_hybrid_implementation_with_throwing_model_falls_back_to_deterministic()
    {
        var brick = new BalanceAnalysisBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<BalanceAnalysisBrick>.Instance,
            new ThrowingModel());

        var input = new BrickInput();
        input.Set("sheet_id", "hybrid-fail");
        input.Set("data", JsonSerializer.SerializeToElement(new[]
        {
            new { id = "assault_rifle", stats = new { ttk_ms = 1500 } }
        }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());

        var suggestions = output.Get<List<global::GameDirector.Bricks.Schemas.Delta>>("suggestions");
        suggestions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BalanceBrick_ParseRow_handles_jsonelement_stats_in_dictionary()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var statsJe = JsonDocument.Parse("{\"damage\":99,\"misc\":\"abc\"}").RootElement;
        var rows = new List<object>
        {
            new Dictionary<string, object>
            {
                ["id"] = "assault_rifle",
                ["stats"] = statsJe
            }
        };
        var input = new BrickInput();
        input.Set("sheet_id", "je-stats");
        input.Set("data", rows);

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Get<List<global::GameDirector.Bricks.Schemas.Outlier>>("flagged").Should().NotBeEmpty();
    }

    [Fact]
    public async Task BalanceBrick_GetString_handles_int_value_path()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = new BrickInput();
        input.Set("sheet_id", (object)12345);
        input.Set("data", Array.Empty<object>());

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Summary.Should().Contain("12345");
    }

    [Fact]
    public async Task ContentBrick_missing_required_key_throws_keynotfound()
    {
        var brick = new ContentGenerationBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<ContentGenerationBrick>.Instance,
            new GameDirectorOfflineModel());

        var input = new BrickInput();
        input.Set("prompt", "p");
        input.Set("target_type", "flavor");

        var act = () => brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ContentBrick_GetString_accepts_jsonelement_string()
    {
        var brick = new ContentGenerationBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<ContentGenerationBrick>.Instance,
            new GameDirectorOfflineModel());

        var input = new BrickInput();
        input.Set("session_id", JsonDocument.Parse("\"sess-je\"").RootElement);
        input.Set("prompt", JsonDocument.Parse("\"prompt-je\"").RootElement);
        input.Set("target_type", JsonDocument.Parse("\"flavor\"").RootElement);
        input.Set("fast", true);

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());
        output.Get<string>("content").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MapBrick_model_throwing_falls_back_to_heuristic_report()
    {
        var brick = new MapFlowBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<MapFlowBrick>.Instance,
            new ThrowingModel());

        var input = new BrickInput();
        input.Set("map_id", "throw");
        input.Set("config", JsonSerializer.SerializeToElement(new
        {
            tiles = new[] { new[] { new { x = 0, y = 0, traversalOptions = 1 } } },
            spawns = new[] { new { id = "s", x = 0, y = 0 } }
        }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Get<string>("sightline_report").Should().Contain("throw");
    }

    [Fact]
    public async Task MapBrick_GetString_handles_jsonelement_and_int()
    {
        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance);
        var input = new BrickInput();
        input.Set("map_id", JsonDocument.Parse("\"je-map\"").RootElement);
        input.Set("config", JsonSerializer.SerializeToElement(new { tiles = Array.Empty<object>(), spawns = Array.Empty<object>() }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Summary.Should().Contain("je-map");
    }

    [Fact]
    public async Task MapBrick_missing_map_id_throws()
    {
        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance);
        var input = new BrickInput();
        input.Set("config", JsonSerializer.SerializeToElement(new { tiles = Array.Empty<object>(), spawns = Array.Empty<object>() }));
        var act = () => brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task BalanceWatcher_publishes_error_event_when_scan_throws()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-err-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "bad.json"), "{\"sheet_id\":\"err\",\"data\":[{\"id\":\"x\"}]}");

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var feed = new RecordingFeed();
            var watcher = new global::GameDirector.Agents.BalanceWatcherHostedService(
                NullLogger<global::GameDirector.Agents.BalanceWatcherHostedService>.Instance,
                Microsoft.Extensions.Options.Options.Create(new GameDirectorOptions
                {
                    BalanceWatchPath = dir,
                    BalanceWatcherStartupDelay = TimeSpan.Zero,
                    BalanceWatcherInterval = TimeSpan.FromMilliseconds(20)
                }),
                new ThrowingBrickRegistry(),
                audit,
                feed);

            using var lifetime = new CancellationTokenSource();
            await watcher.StartAsync(lifetime.Token);
            for (var i = 0; i < 30 && !feed.Entries.Any(e => e.EventType == "error"); i++)
                await Task.Delay(50, CancellationToken.None);
            lifetime.Cancel();
            await watcher.StopAsync(CancellationToken.None);

            feed.Entries.Should().Contain(e => e.EventType == "error");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class ThrowingModel : IModel
    {
        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct) =>
            throw new InvalidOperationException("simulated");
    }

    private sealed class RecordingFeed : global::GameDirector.Agents.IActivityFeedPublisher
    {
        public List<(string Source, string EventType, string Summary)> Entries { get; } = [];
        public Task PublishAsync(string source, string eventType, string summary, CancellationToken ct)
        {
            Entries.Add((source, eventType, summary));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingBrickRegistry : Nexo.Core.Domain.Execution.IBrickRegistry
    {
        public Brick? GetBrick(string id) => throw new InvalidOperationException("registry broken");
        public IReadOnlyList<Brick> GetAllBricks() => [];
        public void Register(Brick brick) { }
    }
}
