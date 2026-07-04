using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GameDirector.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Client;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.GameDirector;

/// <summary>Tests for map mcp endpoint.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class MapMcpEndpointTests : IAsyncLifetime
{
    private IHost? _host;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var brickRegistry = GameDirectorTestHost.CreateBrickRegistry(audit);

        _host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
                    services.AddSingleton<IDataDecisionAuditLog>(audit);
                    services.AddSingleton<IBrickRegistry>(brickRegistry);
                    services.AddSingleton<INexoClient>(new GameDirectorTestHost.StubNexoClient());
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

        _client = _host.GetTestClient();
    }

    public Task DisposeAsync()
    {
        _host?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<JsonElement> PostAsync(object body)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/mcp", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Initialize_returns_protocol_metadata()
    {
        var result = await PostAsync(new { jsonrpc = "2.0", id = 1, method = "initialize" });
        var info = result.GetProperty("result");
        info.GetProperty("protocolVersion").GetString().Should().Be("2024-11-05");
        info.GetProperty("serverInfo").GetProperty("name").GetString().Should().Be("nexo-game-director");
    }

    [Fact]
    public async Task Tools_list_returns_all_five_tools()
    {
        var result = await PostAsync(new { jsonrpc = "2.0", id = 2, method = "tools/list" });
        var tools = result.GetProperty("result").GetProperty("tools");
        var names = tools.EnumerateArray().Select(t => t.GetProperty("name").GetString()!).ToHashSet();
        names.Should().BeEquivalentTo([
            "analyze_balance", "validate_map", "generate_content", "get_audit_trail", "query_patterns"
        ]);
    }

    [Fact]
    public async Task Tools_call_analyze_balance_returns_text_content()
    {
        var result = await PostAsync(new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "analyze_balance",
                arguments = new
                {
                    sheet_id = "mcp-http",
                    data = new[] { new { id = "battle_rifle", stats = new { damage = 18.0 } } }
                }
            }
        });

        var content = result.GetProperty("result").GetProperty("content");
        content.GetArrayLength().Should().Be(1);
        var text = content[0].GetProperty("text").GetString()!;
        text.Should().Contain("audit_id");
    }

    [Fact]
    public async Task Tools_call_unknown_tool_returns_error_payload()
    {
        var result = await PostAsync(new
        {
            jsonrpc = "2.0",
            id = 4,
            method = "tools/call",
            @params = new { name = "missing", arguments = new { } }
        });

        var res = result.GetProperty("result");
        res.GetProperty("isError").GetBoolean().Should().BeTrue();
        res.GetProperty("content")[0].GetProperty("text").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Ping_returns_empty_result()
    {
        var result = await PostAsync(new { jsonrpc = "2.0", id = 5, method = "ping" });
        result.GetProperty("result").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task Unsupported_method_returns_error_field()
    {
        var result = await PostAsync(new { jsonrpc = "2.0", id = 6, method = "nope" });
        result.GetProperty("result").GetProperty("error").GetString().Should().Contain("unsupported");
    }

    [Fact]
    public async Task Missing_method_returns_400()
    {
        var json = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 7 });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/mcp", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Notification_without_id_returns_null_id()
    {
        var result = await PostAsync(new { jsonrpc = "2.0", method = "ping" });
        result.GetProperty("id").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
