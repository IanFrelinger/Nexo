using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nexo.API.Security;
using Nexo.Core.Application.Product.Models;
using Nexo.Infrastructure.Product;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

/// <summary>Tests for nexo http org.</summary>
public sealed class NexoHttpOrgTests
{
    [Fact]
    public void TryResolveOrgContext_requires_membership()
    {
        var store = new InMemoryOrganizationStore();
        var org = store.CreateOrganization("Acme");
        store.AddMember(org.OrgId, "alice@example.com", OrganizationRole.Admin);

        var context = new DefaultHttpContext();
        context.Request.Headers[NexoHttpOrg.UserHeaderName] = "bob@example.com";
        context.Request.Headers[NexoHttpOrg.OrgHeaderName] = org.OrgId;

        var ok = NexoHttpOrg.TryResolveOrgContext(
            context.Request,
            new NexoProductOptions(),
            store,
            out _,
            out _,
            out _,
            out var error);

        ok.Should().BeFalse();
        error.Should().Contain("not a member");
    }

    [Fact]
    public void TryResolveOrgContext_succeeds_for_member()
    {
        var store = new InMemoryOrganizationStore();
        var org = store.CreateOrganization("Acme");
        store.AddMember(org.OrgId, "alice@example.com", OrganizationRole.Member);

        var context = new DefaultHttpContext();
        context.Request.Headers[NexoHttpOrg.UserHeaderName] = "alice@example.com";
        context.Request.Headers[NexoHttpOrg.OrgHeaderName] = org.OrgId;

        var ok = NexoHttpOrg.TryResolveOrgContext(
            context.Request,
            new NexoProductOptions(),
            store,
            out var resolvedOrg,
            out var userId,
            out var role,
            out _);

        ok.Should().BeTrue();
        resolvedOrg.OrgId.Should().Be(org.OrgId);
        userId.Should().Be("alice@example.com");
        role.Should().Be(OrganizationRole.Member);
    }
}
