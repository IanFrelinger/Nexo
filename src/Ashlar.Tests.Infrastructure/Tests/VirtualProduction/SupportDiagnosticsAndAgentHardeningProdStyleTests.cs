using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Ashlar.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// ProdStyle coverage on the real Ashlar.API pipeline for two API-hardening findings:
/// (465) GET /api/support/diagnostics must be credentialed like the rest of /api when a built-in
/// auth mode is configured — it was reachable unauthenticated because the default MutatingApi scope
/// ignores GETs; and (459) POST /api/agent with an unknown agent name must be a named 404 (not a
/// bare, unlogged 500). Runs the production Program with the full middleware chain.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class SupportDiagnosticsAndAgentHardeningProdStyleTests
{
    private const string ApiKey = "prodstyle-diag-key-123";

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> ApiKeyAuthFactory()
        => new AshlarApiWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Ashlar:Security:AuthorizationMode", "ApiKey");
            builder.UseSetting("Ashlar:Security:ApiKey", ApiKey);
        });

    [Fact(Timeout = 60000)]
    public async Task Support_diagnostics_requires_the_key_when_auth_is_configured()
    {
        using var factory = ApiKeyAuthFactory();
        using var client = factory.CreateClient();

        var withoutKey = await client.GetAsync("/api/support/diagnostics");
        withoutKey.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "the diagnostics bundle is a GET the mutating-verb scope would have left open");

        using var authorized = new HttpRequestMessage(HttpMethod.Get, "/api/support/diagnostics");
        authorized.Headers.Add("X-Ashlar-Api-Key", ApiKey);
        var withKey = await client.SendAsync(authorized);
        withKey.StatusCode.Should().Be(HttpStatusCode.OK, "a valid key must still reach the endpoint");
    }

    [Fact(Timeout = 60000)]
    public async Task Support_diagnostics_stays_open_when_auth_mode_is_none()
    {
        // The default Testing host configures no built-in auth mode.
        using var factory = new AshlarApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/support/diagnostics");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "diagnostics remains reachable only while AuthorizationMode=None");
    }

    [Fact(Timeout = 60000)]
    public async Task Post_agent_with_unknown_name_returns_named_404()
    {
        using var factory = new AshlarApiWebApplicationFactory();
        using var client = factory.CreateClient();

        const string unknownAgent = "this-agent-does-not-exist";
        var response = await client.PostAsJsonAsync("/api/agent", new { agentName = unknownAgent });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "an unknown agent name is a client error, not a server fault");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(unknownAgent, "the response body must name the missing agent");
    }

    [Fact(Timeout = 60000)]
    public async Task Post_agent_with_blank_name_still_returns_400()
    {
        using var factory = new AshlarApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/agent",
            new StringContent("{\"agentName\":\"\"}", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "an empty agent name is rejected before dispatch");
    }
}
