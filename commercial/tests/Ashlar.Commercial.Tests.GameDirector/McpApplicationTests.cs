using System.Text.Json;
using FluentAssertions;
using GameDirector.Domain;
using GameDirector.Mcp;
using GameDirector.Mcp.Tools;
using Ashlar.Core.Domain.Bricks;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for mcp application.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class McpApplicationTests
{
    [Fact]
    public void McpToolRegistry_lists_all_five_tools()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
        var mcp = GameDirectorTestHost.CreateMcpRegistry(registry, audit);

        mcp.ListTools().Select(t => t.Name).Should().BeEquivalentTo([
            "analyze_balance",
            "validate_map",
            "generate_content",
            "get_audit_trail",
            "query_patterns"
        ]);
    }

    [Fact]
    public async Task McpBrickExecutor_unknown_brick_throws()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
        var executor = new McpBrickExecutor(registry);

        var act = () => executor.ExecuteAsync("nonexistent.brick", new Dictionary<string, object?>(), ImplementationType.Deterministic, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AnalyzeBalanceTool_returns_success_dto()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
        var tool = new AnalyzeBalanceTool(new McpBrickExecutor(registry));
        var args = JsonSerializer.SerializeToElement(new
        {
            sheet_id = "mcp",
            data = new[] { new { id = "battle_rifle", stats = new { damage = 40.0 } } }
        });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("output").GetProperty("audit_id").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ValidateMapTool_returns_choke_and_equity()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
        var tool = new ValidateMapTool(new McpBrickExecutor(registry));
        var args = JsonSerializer.SerializeToElement(new
        {
            map_id = "mcp_map",
            config = new
            {
                tiles = new[]
                {
                    new[] { new { x = 0, y = 0, traversalOptions = 1 }, new { x = 1, y = 0, traversalOptions = 1 } }
                },
                spawns = new[] { new { id = "s1", x = 0, y = 0 }, new { id = "s2", x = 5, y = 5 } }
            }
        });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        result.GetProperty("output").GetProperty("choke_density").GetDouble().Should().BeGreaterThan(0);
        result.GetProperty("output").GetProperty("spawn_equity").GetDouble().Should().BeInRange(0, 1);
    }

    [Fact]
    public async Task GetAuditTrailTool_returns_records_after_brick_run()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
        var executor = new McpBrickExecutor(registry);
        var balanceArgs = JsonSerializer.SerializeToElement(new
        {
            sheet_id = "trail",
            data = new[] { new { id = "smg", stats = new { ttk_ms = 2000.0 } } }
        });
        await executor.ExecuteAsync("balance.analysis", new Dictionary<string, object?>
        {
            ["sheet_id"] = "trail",
            ["data"] = balanceArgs.GetProperty("data")
        }, Ashlar.Core.Domain.Bricks.ImplementationType.Deterministic, CancellationToken.None);

        var tool = new GetAuditTrailTool(audit);
        var from = DateTimeOffset.UtcNow.AddHours(-1).ToString("O");
        var to = DateTimeOffset.UtcNow.AddHours(1).ToString("O");
        var args = JsonSerializer.SerializeToElement(new { from, to, tool = "analyze_balance" });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        result.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
        result.GetProperty("records").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task McpRegistry_call_analyze_balance_end_to_end()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var mcp = GameDirectorTestHost.CreateMcpRegistry(GameDirectorTestHost.CreateBrickRegistry(audit), audit);

        var result = await mcp.CallAsync("analyze_balance", JsonSerializer.SerializeToElement(new
        {
            sheet_id = "registry",
            data = new[] { new { id = "assault_rifle", stats = new { ttk_ms = 1500.0 } } }
        }), CancellationToken.None);

        result.GetProperty("output").GetProperty("flagged").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task McpRegistry_unknown_tool_throws()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var mcp = GameDirectorTestHost.CreateMcpRegistry(GameDirectorTestHost.CreateBrickRegistry(audit), audit);

        var act = () => mcp.CallAsync("not_a_tool", null, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateContentTool_returns_offline_content()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
        var tool = new GenerateContentTool(new McpBrickExecutor(registry));
        var args = JsonSerializer.SerializeToElement(new
        {
            session_id = "forge-1",
            prompt = "Arena announcer line",
            target_type = "flavor",
            fast = true
        });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("output").GetProperty("content").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task QueryPatternsTool_returns_knowledge_payload()
    {
        var tool = new QueryPatternsTool(new GameDirectorTestHost.StubAshlarClient());
        var args = JsonSerializer.SerializeToElement(new { context = "balance", asset_type = "balance", limit = 5 });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        result.GetProperty("total").GetInt32().Should().Be(0);
        result.GetProperty("entries").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task McpRegistry_call_generate_content_and_validate_map()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var mcp = GameDirectorTestHost.CreateMcpRegistry(GameDirectorTestHost.CreateBrickRegistry(audit), audit);

        var content = await mcp.CallAsync("generate_content", JsonSerializer.SerializeToElement(new
        {
            session_id = "s",
            prompt = "test",
            target_type = "item"
        }), CancellationToken.None);
        content.GetProperty("output").GetProperty("audit_id").GetString().Should().NotBeNullOrWhiteSpace();

        var map = await mcp.CallAsync("validate_map", JsonSerializer.SerializeToElement(new
        {
            map_id = "reg_map",
            config = new
            {
                tiles = new[] { new[] { new { x = 0, y = 0, traversalOptions = 1 } } },
                spawns = new[] { new { id = "s1", x = 0, y = 0 }, new { id = "s2", x = 1, y = 1 } }
            }
        }), CancellationToken.None);
        map.GetProperty("output").GetProperty("spawn_equity").GetDouble().Should().BeInRange(0, 1);
    }
}
