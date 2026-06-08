using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet.Host;

[Trait("Category", "CommercialFleetHost")]
public sealed class FleetHostEndpointTests : IClassFixture<WebApplicationFactory<FleetHostProgram>>
{
    private readonly WebApplicationFactory<FleetHostProgram> _factory;

    public FleetHostEndpointTests(WebApplicationFactory<FleetHostProgram> factory) =>
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

    [Fact]
    public async Task Health_returns_ok()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Commercial_fleet_nodes_list_returns_ok_without_auth()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/mesh/fleet/nodes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var nodes = await response.Content.ReadFromJsonAsync<List<FleetNodeDto>>();
        nodes.Should().NotBeNull();
    }

    [Fact]
    public async Task Commercial_mesh_task_create_accepts_mutating_request_with_api_key()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/mesh/tasks")
        {
            Content = JsonContent.Create(new { name = "commercial-fleet-host-test-task", steps = 1 })
        };
        request.Headers.Add("X-Nexo-Api-Key", "fleet-host-dev-key");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Commercial_fleet_register_node_accepts_mutating_request_with_api_key()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/mesh/fleet/nodes")
        {
            Content = JsonContent.Create(new
            {
                peerId = "commercial-fleet-host-test-peer",
                apiBaseUrl = "http://127.0.0.1:8080",
                trustTier = "Trusted"
            })
        };
        request.Headers.Add("X-Nexo-Api-Key", "fleet-host-dev-key");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record FleetNodeDto(string PeerId);
}
