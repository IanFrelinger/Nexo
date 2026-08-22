using FluentAssertions;
using Ashlar.Core.Application.Product.Models;
using Ashlar.Infrastructure.Product;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Product;

/// <summary>Tests for in memory organization store.</summary>
public sealed class InMemoryOrganizationStoreTests
{
    [Fact]
    public void CreateOrganization_assigns_tenant_scope()
    {
        var store = new InMemoryOrganizationStore();
        var org = store.CreateOrganization("Acme");

        org.TenantId.Should().StartWith("org-");
        store.GetOrganization(org.OrgId).Should().NotBeNull();
    }

    [Fact]
    public void AddMember_and_GetMemberRole_round_trip()
    {
        var store = new InMemoryOrganizationStore();
        var org = store.CreateOrganization("Acme");
        store.AddMember(org.OrgId, "alice@example.com", OrganizationRole.Admin);

        store.GetMemberRole(org.OrgId, "alice@example.com").Should().Be(OrganizationRole.Admin);
        store.GetMembers(org.OrgId).Should().ContainSingle();
    }
}
