using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Ashlar.API.Endpoints;
using Ashlar.API.Security;
using Ashlar.Core.Application.Product.Models;
using Ashlar.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>Tests for product fleet org control plane.</summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class ProductFleetOrgControlPlaneTests : IClassFixture<AshlarCloudApiWebApplicationFactory>
{
    private readonly AshlarCloudApiWebApplicationFactory _factory;

    /// <summary>Product fleet org control plane tests.</summary>
    /// <param name="factory">Factory.</param>
    public ProductFleetOrgControlPlaneTests(AshlarCloudApiWebApplicationFactory factory) => _factory = factory;

    [Fact(Timeout = 120000)]
    public async Task Member_cannot_add_other_members()
    {
        var client = _factory.CreateClient();
        var org = await CreateOrgAsync(client, "alice@acme.example", "Acme");
        /// <summary>Add member async.</summary>
        await AddMemberAsync(client, org.OrgId, "alice@acme.example", "bob@acme.example", OrganizationRole.Member);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/orgs/{org.OrgId}/members");
        request.Headers.TryAddWithoutValidation(AshlarHttpOrg.UserHeaderName, "bob@acme.example");
        request.Headers.TryAddWithoutValidation(AshlarHttpOrg.OrgHeaderName, org.OrgId);
        request.Content = JsonContent.Create(new AddOrganizationMemberRequest("eve@acme.example", "Member"));

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(Timeout = 120000)]
    public async Task Copilot_usage_requires_org_membership_when_cloud_mode_enabled()
    {
        var client = _factory.CreateClient();
        var org = await CreateOrgAsync(client, "alice@acme.example", "Acme");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/usage/summary?hours=24");
        request.Headers.TryAddWithoutValidation(AshlarHttpTenant.TenantHeaderName, org.TenantId);
        request.Headers.TryAddWithoutValidation(AshlarHttpOrg.UserHeaderName, "stranger@example.com");
        request.Headers.TryAddWithoutValidation(AshlarHttpOrg.OrgHeaderName, org.OrgId);

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<Organization> CreateOrgAsync(HttpClient client, string creator, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/orgs");
        request.Headers.TryAddWithoutValidation(AshlarHttpOrg.UserHeaderName, creator);
        request.Content = JsonContent.Create(new CreateOrganizationRequest(name, null));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var org = await response.Content.ReadFromJsonAsync<Organization>();
        org.Should().NotBeNull();
        return org!;
    }

    private static async Task AddMemberAsync(
        HttpClient client,
        string orgId,
        string adminUser,
        string newUser,
        OrganizationRole role)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/orgs/{orgId}/members");
        request.Headers.TryAddWithoutValidation(AshlarHttpOrg.UserHeaderName, adminUser);
        request.Headers.TryAddWithoutValidation(AshlarHttpOrg.OrgHeaderName, orgId);
        request.Content = JsonContent.Create(new AddOrganizationMemberRequest(newUser, role.ToString()));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
