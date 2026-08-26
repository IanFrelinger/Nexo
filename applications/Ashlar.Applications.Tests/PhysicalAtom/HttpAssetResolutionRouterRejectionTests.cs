using FluentAssertions;
using Ashlar.Certification.Physical.Resolution;
using Ashlar.Certification.Physical.Resolution.Http;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Phase 3 HTTP resolution router rejection tests: the router fails closed with a
/// specific status and failure code for a missing store, non-GET methods, and
/// unresolved assets (the happy paths live in <see cref="HttpAssetResolutionRouterTests"/>).
/// </summary>
[Trait("Category", "Certification")]
public sealed class HttpAssetResolutionRouterRejectionTests
{
    [Fact]
    public void R1_MissingStore_Returns500StoreMissing()
    {
        var response = HttpAssetResolutionRouter.Handle("GET", "/ashlar/atoms/x/cert", null!);

        response.StatusCode.Should().Be(500);
        response.FailureCode.Should().Be("store-missing");
        response.ContentType.Should().Be("text/plain");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public void R2_NonGetMethod_Returns405MethodNotAllowed(string method)
    {
        var response = HttpAssetResolutionRouter.Handle(
            method,
            "/ashlar/atoms/11111111-1111-1111-1111-111111111111/cert",
            new InMemoryAssetResolutionStore());

        response.StatusCode.Should().Be(405);
        response.FailureCode.Should().Be("method-not-allowed");
    }

    [Fact]
    public void R3_UnregisteredAsset_Returns404AssetUnresolved()
    {
        var response = HttpAssetResolutionRouter.Handle(
            "GET",
            $"/ashlar/assets/{new string('b', 64)}/1.0.0",
            new InMemoryAssetResolutionStore());

        response.StatusCode.Should().Be(404);
        response.FailureCode.Should().Be("asset-unresolved");
    }

    [Fact]
    public void R4_MalformedAtomIdSegment_FallsThroughTo404Route()
    {
        var response = HttpAssetResolutionRouter.Handle(
            "GET",
            "/ashlar/atoms/not-a-guid/cert",
            new InMemoryAssetResolutionStore());

        response.StatusCode.Should().Be(404);
        response.FailureCode.Should().Be("route-not-found");
    }

    [Fact]
    public void A1_LowercaseGetMethod_AcceptedCaseInsensitively()
    {
        var response = HttpAssetResolutionRouter.Handle(
            "get",
            "/ashlar/atoms/11111111-1111-1111-1111-111111111111/cert",
            new InMemoryAssetResolutionStore());

        response.StatusCode.Should().Be(404, "the method is accepted and the lookup itself misses");
        response.FailureCode.Should().Be("atom-unresolved");
    }
}
