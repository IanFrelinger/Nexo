using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
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
using Nexo.Abstractions;
using Nexo.Client;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GameDomain.Session;
using Xunit;

namespace Nexo.Tests.GameDirector;

[Trait("Category", "GameDirectorApplication")]
public sealed class FinalBranchTests
{
    [Fact]
    public async Task BalanceBrick_with_trust_pack_includes_pack_id_in_audit()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var brick = new BalanceAnalysisBrick(
            audit,
            NullLogger<BalanceAnalysisBrick>.Instance,
            model: null,
            trustPacks: new StubTrustPackRegistry("internal-only"));

        var input = new BrickInput();
        input.Set("sheet_id", "tp");
        input.Set("data", Array.Empty<object>());

        await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        var records = AuditRecordParser.Query(audit, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        records.Should().Contain(r => r.TrustPolicy == "internal-only");
    }

    [Fact]
    public async Task MapBrick_with_trust_pack_includes_pack_id_in_audit()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var brick = new MapFlowBrick(
            audit,
            NullLogger<MapFlowBrick>.Instance,
            model: null,
            trustPacks: new StubTrustPackRegistry("studio-strict"));

        var input = new BrickInput();
        input.Set("map_id", "m");
        input.Set("config", JsonSerializer.SerializeToElement(new { tiles = Array.Empty<object>(), spawns = Array.Empty<object>() }));

        await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        var records = AuditRecordParser.Query(audit, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        records.Should().Contain(r => r.TrustPolicy == "studio-strict");
    }

    [Fact]
    public async Task ContentBrick_with_trust_pack_includes_pack_id_in_audit()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var brick = new ContentGenerationBrick(
            audit,
            NullLogger<ContentGenerationBrick>.Instance,
            new GameDirectorOfflineModel(),
            sessions: null,
            trustPacks: new StubTrustPackRegistry("studio-strict"));

        var input = new BrickInput();
        input.Set("session_id", "tp");
        input.Set("prompt", "p");
        input.Set("target_type", "flavor");
        input.Set("fast", true);

        await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());

        var records = AuditRecordParser.Query(audit, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        records.Should().Contain(r => r.TrustPolicy == "studio-strict");
    }

    [Fact]
    public async Task ContentBrick_non_fast_uses_context_provider_when_set()
    {
        var brick = new ContentGenerationBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<ContentGenerationBrick>.Instance,
            new GameDirectorOfflineModel());

        var input = new BrickInput();
        input.Set("session_id", "s");
        input.Set("prompt", "p");
        input.Set("target_type", "flavor");
        input.Set("fast", false);

        var ctx = new Nexo.Infrastructure.Execution.ExecutionContext { AgentId = "test", Provider = "custom-provider" };
        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, ctx);

        output.Get<string>("model_used").Should().Be("custom-provider");
    }

    [Fact]
    public async Task ContentBrick_model_returns_null_text_yields_empty_content()
    {
        var brick = new ContentGenerationBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<ContentGenerationBrick>.Instance,
            new NullTextModel());

        var input = new BrickInput();
        input.Set("session_id", "s");
        input.Set("prompt", "p");
        input.Set("target_type", "flavor");
        input.Set("fast", true);

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());
        output.Get<string>("content").Should().BeEmpty();
    }

    [Fact]
    public async Task BalanceBrick_hybrid_with_null_model_text_skips_rationale_replacement()
    {
        var brick = new BalanceAnalysisBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<BalanceAnalysisBrick>.Instance,
            new NullTextModel());

        var input = new BrickInput();
        input.Set("sheet_id", "h");
        input.Set("data", JsonSerializer.SerializeToElement(new[]
        {
            new { id = "assault_rifle", stats = new { ttk_ms = 1500 } }
        }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());
        output.Get<List<global::GameDirector.Bricks.Schemas.Outlier>>("flagged").Should().NotBeEmpty();
    }

    [Fact]
    public async Task GameDirectorOfflineModel_handles_empty_messages_array()
    {
        var model = new GameDirectorOfflineModel();
        var result = await model.CompleteAsync(new ModelInput([]), CancellationToken.None);
        result.Text.Should().Contain("[offline]");
    }

    [Fact]
    public void GameProjectContext_partial_metadata_only_populates_present_keys()
    {
        var session = new SessionState
        {
            SessionId = "s",
            Metadata = new Dictionary<string, string>
            {
                ["gameDirector.balanceSheets"] = "[]",
                ["gameDirector.generationHistory"] = "[]"
            }
        };

        var ctx = GameProjectContext.FromSession(session);

        ctx.BalanceSheets.Should().BeEmpty();
        ctx.MapConfigs.Should().BeEmpty();
        ctx.GenerationHistory.Should().BeEmpty();
    }

    [Fact]
    public void AuditRecord_handles_null_jsonelements_in_classification()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        audit.LogClassification("game-director", "analyze_balance", JsonSerializer.Serialize(new
        {
            audit_id = (string?)null,
            tool = (string?)null,
            input_hash = (string?)null,
            model_id = (string?)null,
            trust_policy = (string?)null,
            output_summary = (string?)null,
            sanitization_events = new object?[] { null, "ok", "" }
        }));

        var records = AuditRecordParser.Query(audit, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));

        records.Should().Contain(r => r.SanitizationEvents.Contains("ok"));
    }

    [Fact]
    public async Task MapMcpEndpoint_method_field_null_in_json_returns_unsupported()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
        using var host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddSingleton<IDataDecisionAuditLog>(audit);
                    services.AddSingleton<IBrickRegistry>(registry);
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

        var client = host.GetTestClient();
        var json = "{\"jsonrpc\":\"2.0\",\"id\":10,\"method\":null}";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/mcp", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("result").GetProperty("error").GetString().Should().Contain("unsupported");
    }

    private sealed class StubTrustPackRegistry : ITrustPolicyPackRegistry
    {
        private readonly string _id;
        public StubTrustPackRegistry(string id) => _id = id;
        public IReadOnlyList<TrustPolicyPackInfo> ListPacks() => Array.Empty<TrustPolicyPackInfo>();
        public TrustPolicyPack? GetById(string id) => null;
        public ActiveTrustPolicyPack? GetActivePack() => new(_id, "1.0", DateTimeOffset.UtcNow);
        public Task<ActiveTrustPolicyPack> ActivateAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ActiveTrustPolicyPack(id, "1.0", DateTimeOffset.UtcNow));
    }

    private sealed class NullTextModel : IModel
    {
        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct) =>
            Task.FromResult(new ModelOutput(null!));
    }
}
