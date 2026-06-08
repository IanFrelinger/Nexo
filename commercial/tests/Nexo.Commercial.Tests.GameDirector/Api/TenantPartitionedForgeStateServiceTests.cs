using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.API.Forge;
using Nexo.GameDomain.Session;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

public sealed class TenantPartitionedForgeStateServiceTests
{
    [Fact]
    public void InMemory_IsolatesSessionsPerTenant()
    {
        var http = new HttpContextAccessor();
        var opts = Options.Create(new ForgeSessionOptions { LiteDbPath = null });
        var env = new StubWebHostEnvironment();
        using var svc = new TenantPartitionedForgeStateService(http, opts, env, NullLoggerFactory.Instance);

        http.HttpContext = new DefaultHttpContext();
        http.HttpContext.Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        http.HttpContext.Items[ForgeTenantResolver.HttpContextItemKey] = "tenant-a";
        svc.Session = new SessionState { SessionId = "1", Name = "Alpha" };

        http.HttpContext = new DefaultHttpContext();
        http.HttpContext.Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        http.HttpContext.Items[ForgeTenantResolver.HttpContextItemKey] = "tenant-b";
        svc.Session.Name.Should().NotBe("Alpha");

        http.HttpContext = new DefaultHttpContext();
        http.HttpContext.Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        http.HttpContext.Items[ForgeTenantResolver.HttpContextItemKey] = "tenant-a";
        svc.Session.Name.Should().Be("Alpha");
    }
}
