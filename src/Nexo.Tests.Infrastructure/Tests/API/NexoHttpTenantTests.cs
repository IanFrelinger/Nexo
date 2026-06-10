using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nexo.API.Security;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

/// <summary>
/// Product Fleet Phase 0.1 — tenant header resolution and allow-list enforcement.
/// </summary>
public sealed class NexoHttpTenantTests
{
    [Fact]
    public void TryResolve_uses_default_when_header_missing()
    {
        var ctx = new DefaultHttpContext();
        var options = new NexoProductOptions { DefaultTenantId = "acme" };

        NexoHttpTenant.TryResolve(ctx.Request, options, out var tenantId, out var error).Should().BeTrue();
        tenantId.Should().Be("acme");
        error.Should().BeNull();
    }

    [Fact]
    public void TryResolve_accepts_header_when_allow_list_empty()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[NexoHttpTenant.TenantHeaderName] = "tenant-b";
        var options = new NexoProductOptions { DefaultTenantId = "default" };

        NexoHttpTenant.TryResolve(ctx.Request, options, out var tenantId, out _).Should().BeTrue();
        tenantId.Should().Be("tenant-b");
    }

    [Fact]
    public void TryResolve_rejects_tenant_not_in_allow_list()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[NexoHttpTenant.TenantHeaderName] = "rogue";
        var options = new NexoProductOptions
        {
            DefaultTenantId = "default",
            AllowedTenantIds = ["tenant-a", "tenant-b"],
        };

        NexoHttpTenant.TryResolve(ctx.Request, options, out _, out var error).Should().BeFalse();
        error.Should().Contain("allow-list");
    }

    [Fact]
    public void TryResolve_rejects_overlong_tenant_header()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[NexoHttpTenant.TenantHeaderName] = new string('x', 129);
        var options = new NexoProductOptions();

        NexoHttpTenant.TryResolve(ctx.Request, options, out _, out var error).Should().BeFalse();
        error.Should().Contain("128");
    }
}
