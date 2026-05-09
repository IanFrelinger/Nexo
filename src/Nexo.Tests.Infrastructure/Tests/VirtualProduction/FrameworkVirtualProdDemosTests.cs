using System.Net.Http.Json;
using FluentAssertions;
using Nexo.Client;
using Nexo.Contracts;
using Nexo.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// End-to-end parity with <c>docs/demos/</c> samples: real Kestrel pipeline + <see cref="INexoClient"/> over HTTP.
/// Complements NCR routing harness tests (which isolate cloud/peers with test doubles).
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class FrameworkVirtualProdDemosTests : IClassFixture<NexoApiWebApplicationFactory>
{
    private readonly NexoApiWebApplicationFactory _factory;

    public FrameworkVirtualProdDemosTests(NexoApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(Timeout = 120000)]
    public async Task Virtual_prod_GET_api_status_full_HTTP_stack_matches_demo_contract()
    {
        var client = _factory.CreateClient();
        using var resp = await client.GetAsync(new Uri("api/status", UriKind.Relative));
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<StatusResponse>();
        dto.Should().NotBeNull();
        dto!.Mode.Should().NotBeNullOrWhiteSpace();
        dto.Message.Should().NotBeNull();
        dto.TotalAgents.Should().BeGreaterThanOrEqualTo(0);
        dto.ActiveAgents.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact(Timeout = 120000)]
    public async Task Virtual_prod_INexoClient_GetStatusAsync_matches_console_and_blazor_demos()
    {
        var http = _factory.CreateClient();
        var nexo = new NexoClient(http);
        var status = await nexo.GetStatusAsync();
        status.Mode.Should().NotBeNullOrWhiteSpace();
        status.Message.Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task Virtual_prod_health_and_root_static_pipeline_ready()
    {
        var client = _factory.CreateClient();
        using var health = await client.GetAsync(new Uri("health", UriKind.Relative));
        health.EnsureSuccessStatusCode();

        using var root = await client.GetAsync(new Uri("/", UriKind.Relative));
        root.IsSuccessStatusCode.Should().BeTrue();
        var html = await root.Content.ReadAsStringAsync();
        html.Should().Contain("<html");
    }
}
