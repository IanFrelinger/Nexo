using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Nexo.Tests.Infrastructure.Helpers;
using Nexo.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// ProdStyle coverage for the boundary between the API and the portal it is co-hosted with, on the real
/// Nexo.API pipeline. <c>MapFallbackToFile("index.html")</c> serves the SPA for unmatched paths so
/// client-side routing works; without a terminal <c>/api/{**rest}</c> it also swallowed unmatched API
/// paths, and every one of them answered <c>200 text/html</c>.
/// </summary>
/// <remarks>
/// Two things depended on this and neither was tested. SECURITY.md tells a reviewer that the
/// remote-execution routes are "mapped only when Nexo:Execution:ServeRemoteExecution=true (404
/// otherwise)" — the routes really were unmapped, but the evidence a reviewer would check said 405/200.
/// And an integrator whose client misspells a route, or calls a surface that is feature-flagged off,
/// received a web page to parse rather than a 404: the failure a client is least equipped to notice.
/// </remarks>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class ApiNotFoundProdStyleTests
{
    private static WebApplicationFactory<Program> CreateFactory() => new NexoApiWebApplicationFactory();

    [Theory(Timeout = TestTimeouts.HostTouching)]
    [InlineData("/api/definitely-not-a-route")]
    [InlineData("/api/execution/build")]   // unmapped unless Nexo:Execution:ServeRemoteExecution=true
    [InlineData("/api/mcp")]               // feature-flagged off by default
    public async Task Unmatched_api_paths_are_404_not_the_portal(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var get = await client.GetAsync(path);
        get.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "an unmatched path under /api is a missing endpoint, not a page");
        get.Content.Headers.ContentType?.MediaType.Should().NotBe(
            "text/html",
            "an API client must never be handed the portal to parse as its result");

        using var post = await client.PostAsync(path, new StringContent("{}"));
        post.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Mapped_api_routes_and_the_portal_are_both_unaffected()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // The catch-all is less specific than every mapped API route, so real endpoints still win it.
        using var mapped = await client.GetAsync("/api/trust/status");
        mapped.StatusCode.Should().Be(HttpStatusCode.OK, "a mapped API route outranks the /api catch-all");

        using var probe = await client.GetAsync("/health");
        probe.StatusCode.Should().Be(HttpStatusCode.OK, "probes are outside both the catch-all and auth");

        // ...and anything that is not /api still falls through to the SPA, or client-side routing breaks.
        using var spa = await client.GetAsync("/some/portal/route");
        spa.StatusCode.Should().Be(HttpStatusCode.OK, "the portal still serves its own client-side routes");
    }
}
