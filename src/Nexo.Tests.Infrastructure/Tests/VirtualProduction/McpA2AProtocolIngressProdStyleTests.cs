using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions.Transport;
using Nexo.Tests.Infrastructure.Helpers.VirtualProduction;
using Nexo.Transport.A2A.Server;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// ProdStyle coverage for the MCP + A2A protocol surfaces on the real Nexo.API pipeline:
/// dark-by-default, all-verbs auth, allowlisted exposure, and card-anonymity opt-in. Runs the
/// production Program with the full middleware chain. The air-gapped enable refusal is in
/// <see cref="AirGappedProfileApiHostProdStyleTests"/> because it sets a process env var.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class McpA2AProtocolIngressProdStyleTests
{
    private const string ApiKey = "prodstyle-key-123";
    private const string InitializeBody =
        """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"prodstyle","version":"1"}}}""";

    private sealed class TestAgentCatalog : INexoA2AAgentCatalog
    {
        public IReadOnlyList<NexoA2AAgentDescriptor> GetAgents() =>
        [
            new NexoA2AAgentDescriptor(
                Id: "echo-agent",
                Name: "Echo Agent",
                Description: "ProdStyle test agent",
                Version: "1.0.0",
                Domain: "testing",
                Behaviors: ["echo"],
                CoordinationProtocols: [],
                MaxExecutionTime: TimeSpan.FromSeconds(20)),
        ];
    }

    private sealed class EchoAgentTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(
                Success: true,
                Output: new Dictionary<string, object?> { ["echoed"] = request.Payload?["message"] },
                CorrelationId: request.CorrelationId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "echo"));
    }

    private static WebApplicationFactoryScope CreateFactory(
        IDictionary<string, string?>? settings = null,
        bool withTestAgent = false)
    {
        var factory = new NexoApiWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            if (settings is not null)
            {
                foreach (var pair in settings)
                {
                    builder.UseSetting(pair.Key, pair.Value);
                }
            }

            if (withTestAgent)
            {
                builder.ConfigureServices(services =>
                {
                    // Later registrations win for plain resolutions: the fake catalog replaces
                    // the registry-backed one, and the echo transport replaces the kernel's
                    // routing transport for deterministic agent execution.
                    services.AddSingleton<INexoA2AAgentCatalog, TestAgentCatalog>();
                    services.AddSingleton<IAgentTransport, EchoAgentTransport>();
                });
            }
        });
        return new WebApplicationFactoryScope(factory);
    }

    private sealed class WebApplicationFactoryScope : IDisposable
    {
        private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _factory;

        public WebApplicationFactoryScope(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory)
            => _factory = factory;

        public HttpClient CreateClient() => _factory.CreateClient();

        public void Dispose() => _factory.Dispose();
    }

    private static Dictionary<string, string?> ApiKeyAuthSettings(params (string Key, string? Value)[] extra)
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Nexo:Security:AuthorizationMode"] = "ApiKey",
            ["Nexo:Security:ApiKey"] = ApiKey,
        };
        foreach (var (key, value) in extra)
        {
            settings[key] = value;
        }

        return settings;
    }

    private static HttpRequestMessage McpInitializeRequest(string? apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/mcp")
        {
            Content = new StringContent(InitializeBody, Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (apiKey is not null)
        {
            request.Headers.Add("X-Nexo-Api-Key", apiKey);
        }

        return request;
    }

    [Fact(Timeout = 60000)]
    public async Task Protocol_surfaces_are_dark_by_default()
    {
        using var scope = CreateFactory();
        using var client = scope.CreateClient();

        var mcp = await client.SendAsync(McpInitializeRequest(apiKey: null));
        var card = await client.GetAsync("/.well-known/agent-card.json");
        var a2a = await client.PostAsync("/api/a2a/echo-agent", new StringContent("{}", Encoding.UTF8, "application/json"));

        // Unmapped POSTs fall through to the SPA fallback route, which only accepts GET.
        mcp.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
        card.StatusCode.Should().Be(HttpStatusCode.NotFound);
        a2a.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact(Timeout = 60000)]
    public async Task Mcp_requires_credentials_on_every_verb_when_auth_is_configured()
    {
        using var scope = CreateFactory(ApiKeyAuthSettings(
            ("Nexo:Mcp:Server:Enabled", "true"),
            ("Nexo:Mcp:Server:ExposedToolIds:0", "repo.fs.read")));
        using var client = scope.CreateClient();

        var withoutKey = await client.SendAsync(McpInitializeRequest(apiKey: null));
        var getWithoutKey = await client.GetAsync("/api/mcp");

        withoutKey.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "POST /api/mcp is protocol traffic and must be credentialed");
        getWithoutKey.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "the MCP SSE listen channel is a GET the mutating-verb scope would have ignored");
    }

    [Fact(Timeout = 60000)]
    public async Task Mcp_initialize_succeeds_with_credentials_and_advertises_the_server()
    {
        using var scope = CreateFactory(ApiKeyAuthSettings(
            ("Nexo:Mcp:Server:Enabled", "true"),
            ("Nexo:Mcp:Server:ExposedToolIds:0", "repo.fs.read")));
        using var client = scope.CreateClient();

        var response = await client.SendAsync(McpInitializeRequest(ApiKey));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"protocolVersion\"").And.Contain("nexo");
    }

    [Fact(Timeout = 60000)]
    public async Task A2a_agent_card_is_credentialed_by_default_and_anonymous_only_by_opt_in()
    {
        var settings = ApiKeyAuthSettings(
            ("Nexo:A2A:Server:Enabled", "true"),
            ("Nexo:A2A:Server:PublicBaseUrl", "http://localhost"),
            ("Nexo:A2A:Server:ExposedAgentIds:0", "echo-agent"));

        using (var scope = CreateFactory(settings, withTestAgent: true))
        using (var client = scope.CreateClient())
        {
            var anonymous = await client.GetAsync("/.well-known/agent-card.json");
            anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "cards are credentialed by default");

            using var withKey = new HttpRequestMessage(HttpMethod.Get, "/.well-known/agent-card.json");
            withKey.Headers.Add("X-Nexo-Api-Key", ApiKey);
            var authorized = await client.SendAsync(withKey);
            authorized.StatusCode.Should().Be(HttpStatusCode.OK);
            (await authorized.Content.ReadAsStringAsync()).Should().Contain("Echo Agent");
        }

        settings["Nexo:A2A:Server:AllowAnonymousAgentCard"] = "true";
        using (var scope = CreateFactory(settings, withTestAgent: true))
        using (var client = scope.CreateClient())
        {
            var anonymous = await client.GetAsync("/.well-known/agent-card.json");
            anonymous.StatusCode.Should().Be(HttpStatusCode.OK, "anonymity is the explicit operator opt-in");
        }
    }

    [Fact(Timeout = 60000)]
    public async Task A2a_message_send_round_trips_to_the_agent_through_the_real_pipeline()
    {
        using var scope = CreateFactory(
            ApiKeyAuthSettings(
                ("Nexo:A2A:Server:Enabled", "true"),
                ("Nexo:A2A:Server:PublicBaseUrl", "http://localhost"),
                ("Nexo:A2A:Server:ExposedAgentIds:0", "echo-agent")),
            withTestAgent: true);
        using var client = scope.CreateClient();
        client.DefaultRequestHeaders.Add("X-Nexo-Api-Key", ApiKey);

        // The real A2A SDK client against the real Nexo.API pipeline — auth middleware,
        // JSON-RPC binding, task lifecycle, and the echo agent transport all in one pass.
        var a2aClient = new global::A2A.A2AClient(new Uri(client.BaseAddress!, "/api/a2a/echo-agent"), client);
        var response = await a2aClient.SendMessageAsync(new global::A2A.SendMessageRequest
        {
            Message = new global::A2A.Message
            {
                Role = global::A2A.Role.User,
                MessageId = "m1",
                Parts = [global::A2A.Part.FromText("ping over a2a")],
            },
        });

        response.Task.Should().NotBeNull();
        response.Task!.Status!.State.Should().Be(global::A2A.TaskState.Completed,
            "the echo transport completes the task synchronously");
        var artifactJson = JsonSerializer.Serialize(response.Task.Artifacts);
        artifactJson.Should().Contain("ping over a2a");
    }

    // The air-gapped enable refusal sets the process-wide NEXO_DEPLOYMENT_PROFILE and therefore
    // lives in AirGappedProfileApiHostProdStyleTests (the serialized "EnvironmentVariables"
    // collection); a class can belong to only one collection.
}
