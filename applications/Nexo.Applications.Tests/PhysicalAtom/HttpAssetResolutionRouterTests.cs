using FluentAssertions;
using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Resolution.Http;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Phase 3 HTTP resolution router tests (headless request/response records, no live server).
/// </summary>
[Trait("Category", "Certification")]
public sealed class HttpAssetResolutionRouterTests
{
    private static readonly Guid AtomId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly string AssetHash =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void R1_UnknownRoute_Returns404()
    {
        var store = HttpRouterWitness.CreateStore();
        var response = HttpAssetResolutionRouter.Handle("GET", "/unknown", store);

        response.StatusCode.Should().Be(404);
        response.FailureCode.Should().Be("route-not-found");
    }

    [Fact]
    public void R2_UnregisteredAtomCert_Returns404()
    {
        var store = new InMemoryAssetResolutionStore();
        var response = HttpAssetResolutionRouter.Handle(
            "GET",
            $"/nexo/atoms/{AtomId}/cert",
            store);

        response.StatusCode.Should().Be(404);
        response.FailureCode.Should().Be("atom-unresolved");
    }

    [Fact]
    public void A1_GetCert_ReturnsRegisteredCertificate()
    {
        var store = HttpRouterWitness.CreateStore();
        var response = HttpAssetResolutionRouter.Handle(
            "GET",
            $"/nexo/atoms/{AtomId}/cert",
            store);

        response.StatusCode.Should().Be(200);
        response.ContentType.Should().Be("application/json");
        response.Body.Should().NotBeEmpty();
    }

    [Fact]
    public void A2_GetAsset_ReturnsRegisteredBytes()
    {
        var store = HttpRouterWitness.CreateStore();
        var response = HttpAssetResolutionRouter.Handle(
            "GET",
            $"/nexo/assets/{AssetHash}/1.0.0",
            store);

        response.StatusCode.Should().Be(200);
        response.ContentType.Should().Be("application/octet-stream");
        response.Body.Should().Equal("http-router-asset"u8.ToArray());
    }

    private static class HttpRouterWitness
    {
        public static InMemoryAssetResolutionStore CreateStore()
        {
            var store = new InMemoryAssetResolutionStore();
            store.RegisterCert(new PhysicalAtomCertificate
            {
                AtomId = AtomId,
                BindingScope = BindingScope.Design,
                AssetHash = AssetHash,
                AssetVersion = "1.0.0",
                IssuerSignature = "witness"
            });
            store.RegisterAsset(new DigitalTwinAssetRecord(
                AssetHash,
                "1.0.0",
                "application/octet-stream",
                "http-router-asset"u8.ToArray()));
            return store;
        }
    }
}
