using System.Net;

namespace Nexo.Commercial.GameDomain.Maps;
/// <summary>
/// Shared assertions for Mapbox tile HTTP responses (hosts and tests).
/// </summary>
public static class MapboxTileResponseValidators
{
    /// <summary>Assert raster tile success operation.</summary>
    public static void AssertRasterTileSuccess(HttpStatusCode status, string? contentType, ReadOnlySpan<byte> body)
    {
        if (status != HttpStatusCode.OK)
            throw new InvalidOperationException($"Expected OK, got {status}.");

        if (string.IsNullOrEmpty(contentType) || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Expected image/* content type, got '{contentType}'.");

        if (body.Length < 100)
            throw new InvalidOperationException($"Expected non-trivial JPEG body, got {body.Length} bytes.");

        if (!MapboxTileUrls.IsJpegImage(body))
            throw new InvalidOperationException("Body does not start with JPEG SOI (FF D8).");
    }

    /// <summary>Assert vector tile success operation.</summary>
    public static void AssertVectorTileSuccess(HttpStatusCode status, string? contentType, ReadOnlySpan<byte> body)
    {
        if (status != HttpStatusCode.OK)
            throw new InvalidOperationException($"Expected OK, got {status}.");

        if (!string.IsNullOrEmpty(contentType) && !MapboxTileUrls.IsLikelyVectorTileContentType(contentType))
            throw new InvalidOperationException($"Unexpected Content-Type for vector tile: '{contentType}'.");

        if (body.Length < 20)
            throw new InvalidOperationException($"Expected non-trivial tile body, got {body.Length} bytes.");
    }
}
