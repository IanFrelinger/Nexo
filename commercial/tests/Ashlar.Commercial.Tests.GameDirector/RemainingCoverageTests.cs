using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GameDirector.Agents;
using GameDirector.Bricks;
using GameDirector.Domain;
using GameDirector.Mcp;
using GameDirector.Mcp.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.BackgroundAgents.Trust;
using Ashlar.Client;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for remaining coverage.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class RemainingCoverageTests
{
    [Fact]
    public async Task BalanceBrick_data_jsonelement_object_returns_empty()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = new BrickInput();
        input.Set("sheet_id", "obj");
        input.Set("data", JsonDocument.Parse("{\"key\":\"value\"}").RootElement);

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Get<List<global::GameDirector.Bricks.Schemas.Outlier>>("flagged").Should().BeEmpty();
    }

    [Fact]
    public async Task BalanceBrick_dictionary_stats_covers_all_coerce_double_types()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var rows = new List<object>
        {
            new Dictionary<string, object>
            {
                ["id"] = "assault_rifle",
                ["stats"] = new Dictionary<string, object>
                {
                    ["damage"] = 99.5,
                    ["fire_rate"] = (float)1200.0f,
                    ["ttk_ms"] = 1500,
                    ["range"] = 250L,
                    ["zoom"] = JsonDocument.Parse("3.5").RootElement,
                    ["str"] = "11",
                    ["bogus"] = "abc"
                }
            }
        };
        var input = new BrickInput();
        input.Set("sheet_id", "all-types");
        input.Set("data", rows);

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Get<List<global::GameDirector.Bricks.Schemas.Outlier>>("flagged").Should().NotBeEmpty();
    }

    [Fact]
    public async Task BalanceBrick_ParseRow_skips_non_object_array_element()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = new BrickInput();
        input.Set("sheet_id", "mixed");
        input.Set("data", JsonDocument.Parse(
            "[42, {\"id\":\"assault_rifle\",\"stats\":{\"damage\":99}}]").RootElement);

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Get<List<global::GameDirector.Bricks.Schemas.Outlier>>("flagged").Should().NotBeEmpty();
    }

    [Fact]
    public async Task BalanceBrick_GetString_default_path_for_arbitrary_object()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = new BrickInput();
        input.Set("sheet_id", JsonDocument.Parse("\"je-sheet\"").RootElement);
        input.Set("data", Array.Empty<object>());

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Summary.Should().Contain("je-sheet");
    }

    [Fact]
    public async Task ContentBrick_GetString_default_path_for_int_value()
    {
        var brick = new ContentGenerationBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<ContentGenerationBrick>.Instance,
            new GameDirectorOfflineModel());

        var input = new BrickInput();
        input.Set("session_id", "sess");
        input.Set("prompt", "prompt");
        input.Set("target_type", (object)123);
        input.Set("fast", true);

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());
        output.Summary.Should().Contain("123");
    }

    [Fact]
    public async Task MapBrick_GetString_default_path_for_int_value()
    {
        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance);
        var input = new BrickInput();
        input.Set("map_id", (object)42);
        input.Set("config", JsonSerializer.SerializeToElement(new
        {
            tiles = Array.Empty<object>(),
            spawns = Array.Empty<object>()
        }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Summary.Should().Contain("42");
    }

    [Fact]
    public async Task MapBrick_low_equity_triggers_spawn_rebalance_recommendation()
    {
        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance);
        var input = new BrickInput();
        input.Set("map_id", "skewed");
        input.Set("config", JsonSerializer.SerializeToElement(new
        {
            tiles = new[] { new[] { new { x = 0, y = 0, traversalOptions = 4 } } },
            spawns = new[]
            {
                new { id = "a", x = 0, y = 0 },
                new { id = "b", x = 1, y = 0 },
                new { id = "c", x = 2, y = 0 },
                new { id = "d", x = 3, y = 0 },
                new { id = "e", x = 4, y = 0 },
                new { id = "f", x = 100000, y = 0 }
            }
        }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<double>("spawn_equity").Should().BeLessThan(0.7);
        output.Get<List<string>>("recommendations").Should().Contain(r => r.Contains("spawn distances", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BalanceWatcher_file_without_data_property_does_not_publish_drift()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-no-data-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "empty.json"), "{\"sheet_id\":\"empty\"}");

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var feed = new RecordingFeed();
            var watcher = new BalanceWatcherHostedService(
                NullLogger<BalanceWatcherHostedService>.Instance,
                Options.Create(new GameDirectorOptions { BalanceWatchPath = dir }),
                GameDirectorTestHost.CreateBrickRegistry(audit),
                audit,
                feed);

            await watcher.ScanOnceAsync(CancellationToken.None);
            feed.Entries.Should().NotContain(e => e.EventType == "drift");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task BalanceBrick_skips_stat_when_baseline_near_zero()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = new BrickInput();
        input.Set("sheet_id", "zero");
        input.Set("data", JsonSerializer.SerializeToElement(new[]
        {
            new { id = "unknown_weapon", stats = new { obscure_stat = 0.0001 } }
        }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        output.Get<List<global::GameDirector.Bricks.Schemas.Outlier>>("flagged").Should().BeEmpty();
    }

    [Fact]
    public async Task BalanceWatcher_with_nonzero_startup_delay_eventually_runs_scan()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-bw-startup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "x.json"),
            "{\"sheet_id\":\"x\",\"data\":[{\"id\":\"assault_rifle\",\"stats\":{\"ttk_ms\":1500}}]}");

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var feed = new RecordingFeed();
            var watcher = new BalanceWatcherHostedService(
                NullLogger<BalanceWatcherHostedService>.Instance,
                Options.Create(new GameDirectorOptions
                {
                    BalanceWatchPath = dir,
                    BalanceWatcherStartupDelay = TimeSpan.FromMilliseconds(50),
                    BalanceWatcherInterval = TimeSpan.FromMilliseconds(50)
                }),
                GameDirectorTestHost.CreateBrickRegistry(audit),
                audit,
                feed);

            using var lifetime = new CancellationTokenSource();
            await watcher.StartAsync(lifetime.Token);
            for (var i = 0; i < 40 && feed.Entries.Count == 0; i++)
                await Task.Delay(50, CancellationToken.None);
            lifetime.Cancel();
            await watcher.StopAsync(CancellationToken.None);

            feed.Entries.Should().Contain(e => e.EventType == "drift");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void AuditRecordParser_extracts_sanitization_events_from_classification()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        GameDirectorAudit.LogToolExecution(
            audit,
            "audit-1",
            "analyze_balance",
            "hash",
            "deterministic",
            "internal-only",
            "summary",
            new[] { "redacted_api_key", "redacted_token" });

        var records = AuditRecordParser.Query(
            audit,
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddHours(1));

        records.Should().Contain(r =>
            r.SanitizationEvents.Contains("redacted_api_key") &&
            r.SanitizationEvents.Contains("redacted_token"));
    }

    [Fact]
    public async Task ValidateMapTool_missing_config_throws_argument_exception()
    {
        var registry = GameDirectorTestHost.CreateBrickRegistry();
        var tool = new ValidateMapTool(new McpBrickExecutor(registry));
        var args = JsonSerializer.SerializeToElement(new { map_id = "m" });

        var act = () => tool.ExecuteAsync(args, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AnalyzeBalanceTool_omitted_args_uses_defaults_and_succeeds()
    {
        var registry = GameDirectorTestHost.CreateBrickRegistry();
        var tool = new AnalyzeBalanceTool(new McpBrickExecutor(registry));
        var args = JsonSerializer.SerializeToElement(new { data = Array.Empty<object>() });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GenerateContentTool_omitted_args_uses_defaults_and_succeeds()
    {
        var registry = GameDirectorTestHost.CreateBrickRegistry();
        var tool = new GenerateContentTool(new McpBrickExecutor(registry));
        var args = JsonSerializer.SerializeToElement(new { });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task QueryPatternsTool_omitted_args_uses_defaults()
    {
        var tool = new QueryPatternsTool(new GameDirectorTestHost.StubAshlarClient());
        var result = await tool.ExecuteAsync(JsonSerializer.SerializeToElement(new { }), CancellationToken.None);
        result.GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetAuditTrailTool_omitted_args_uses_defaults()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var tool = new GetAuditTrailTool(audit);
        var result = await tool.ExecuteAsync(JsonSerializer.SerializeToElement(new { }), CancellationToken.None);
        result.GetProperty("total").GetInt32().Should().Be(0);
        result.GetProperty("records").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task MapMcpEndpoint_tools_call_without_arguments_property_succeeds_or_returns_error()
    {
        using var host = await BuildHostAsync();
        var client = host.GetTestClient();

        var json = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 9,
            method = "tools/call",
            @params = new { name = "get_audit_trail" }
        });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/mcp", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var result = body.GetProperty("result");
        result.TryGetProperty("content", out _).Should().BeTrue();
    }

    private static async Task<IHost> BuildHostAsync()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var registry = GameDirectorTestHost.CreateBrickRegistry(audit);

        return await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddSingleton<IDataDecisionAuditLog>(audit);
                    services.AddSingleton<IBrickRegistry>(registry);
                    services.AddSingleton<IAshlarClient>(new GameDirectorTestHost.StubAshlarClient());
                    services.AddGameDirectorMcp();
                    services.AddRouting();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapMcpEndpoint("/mcp"));
                });
            })
            .StartAsync();
    }

    /// <summary>Recording feed.</summary>
    private sealed class RecordingFeed : IActivityFeedPublisher
    {
        public List<(string Source, string EventType, string Summary)> Entries { get; } = [];
        public Task PublishAsync(string source, string eventType, string summary, CancellationToken ct)
        {
            Entries.Add((source, eventType, summary));
            return Task.CompletedTask;
        }
    }
}
