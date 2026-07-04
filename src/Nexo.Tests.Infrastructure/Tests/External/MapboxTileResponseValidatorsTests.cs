using System.Net;
using FluentAssertions;
using Nexo.Tests.Infrastructure.External;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.External;

/// <summary>Tests for mapbox tile response validators.</summary>
public sealed class MapboxTileResponseValidatorsTests
{
    [Fact]
    public void AssertRasterTileSuccess_AcceptsValidJpeg()
    {
        var jpeg = new byte[120];
        jpeg[0] = 0xFF;
        jpeg[1] = 0xD8;

        var act = () => MapboxTileResponseValidators.AssertRasterTileSuccess(
            HttpStatusCode.OK,
            "image/jpeg",
            jpeg);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertRasterTileSuccess_RejectsWrongStatus()
    {
        var jpeg = new byte[120];
        jpeg[0] = 0xFF;
        jpeg[1] = 0xD8;

        var act = () => MapboxTileResponseValidators.AssertRasterTileSuccess(
            HttpStatusCode.NotFound,
            "image/jpeg",
            jpeg);

        act.Should().Throw<InvalidOperationException>().WithMessage("*NotFound*");
    }

    [Fact]
    public void AssertRasterTileSuccess_RejectsNonImageContentType()
    {
        var jpeg = new byte[120];
        jpeg[0] = 0xFF;
        jpeg[1] = 0xD8;

        var act = () => MapboxTileResponseValidators.AssertRasterTileSuccess(
            HttpStatusCode.OK,
            "text/html",
            jpeg);

        act.Should().Throw<InvalidOperationException>().WithMessage("*image*");
    }

    [Fact]
    public void AssertRasterTileSuccess_RejectsTooSmallBody()
    {
        var jpeg = new byte[10];
        jpeg[0] = 0xFF;
        jpeg[1] = 0xD8;

        var act = () => MapboxTileResponseValidators.AssertRasterTileSuccess(
            HttpStatusCode.OK,
            "image/jpeg",
            jpeg);

        act.Should().Throw<InvalidOperationException>().WithMessage("*10 bytes*");
    }

    [Fact]
    public void AssertVectorTileSuccess_AcceptsGzipPayload()
    {
        var gzip = new byte[30];
        gzip[0] = 0x1F;
        gzip[1] = 0x8B;

        var act = () => MapboxTileResponseValidators.AssertVectorTileSuccess(
            HttpStatusCode.OK,
            "application/gzip",
            gzip);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertVectorTileSuccess_RejectsBadContentTypeWhenPresent()
    {
        var body = new byte[50];

        var act = () => MapboxTileResponseValidators.AssertVectorTileSuccess(
            HttpStatusCode.OK,
            "text/html",
            body);

        act.Should().Throw<InvalidOperationException>().WithMessage("*text/html*");
    }
}
