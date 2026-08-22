using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Ashlar.API.Security;
using Ashlar.Core.Application.Product.Models;
using Ashlar.Infrastructure.Product;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.API;

/// <summary>Tests for ashlar http org.</summary>
public sealed class AshlarHttpOrgTests
{
    [Fact]
    public void TryResolveOrgContext_requires_membership()
    {
        var store = new InMemoryOrganizationStore();
        var org = store.CreateOrganization("Acme");
        store.AddMember(org.OrgId, "alice@example.com", OrganizationRole.Admin);

        var context = new DefaultHttpContext();
        context.Request.Headers[AshlarHttpOrg.UserHeaderName] = "bob@example.com";
        context.Request.Headers[AshlarHttpOrg.OrgHeaderName] = org.OrgId;

        var ok = AshlarHttpOrg.TryResolveOrgContext(
            context.Request,
            new AshlarProductOptions(),
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
        context.Request.Headers[AshlarHttpOrg.UserHeaderName] = "alice@example.com";
        context.Request.Headers[AshlarHttpOrg.OrgHeaderName] = org.OrgId;

        var ok = AshlarHttpOrg.TryResolveOrgContext(
            context.Request,
            new AshlarProductOptions(),
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
