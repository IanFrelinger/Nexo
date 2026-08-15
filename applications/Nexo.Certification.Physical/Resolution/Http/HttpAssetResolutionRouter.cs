using System.Text;
using Nexo.Certification.Physical.Tagging;

namespace Nexo.Certification.Physical.Resolution.Http;

/// <summary>
/// Headless HTTP router for atom certificate and asset resolution (Phase 3 prototype).
/// </summary>
public static class HttpAssetResolutionRouter
{
    public static HttpResolutionResponse Handle(
        string method,
        string path,
        IAssetResolutionStore store)
    {
        if (store is null)
        {
            return new HttpResolutionResponse(
                500,
                "text/plain",
                Encoding.UTF8.GetBytes("store-missing"),
                "store-missing",
                "Asset resolution store is required.");
        }

        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResolutionResponse(
                405,
                "text/plain",
                Encoding.UTF8.GetBytes("method-not-allowed"),
                "method-not-allowed",
                "Only GET is supported.");
        }

        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length >= 4
            && string.Equals(segments[0], "nexo", StringComparison.Ordinal)
            && string.Equals(segments[1], "atoms", StringComparison.Ordinal)
            && string.Equals(segments[3], "cert", StringComparison.Ordinal)
            && Guid.TryParse(segments[2], out var atomId))
        {
            return HandleCert(atomId, store);
        }

        if (segments.Length >= 4
            && string.Equals(segments[0], "nexo", StringComparison.Ordinal)
            && string.Equals(segments[1], "assets", StringComparison.Ordinal))
        {
            return HandleAsset(segments[2], segments[3], store);
        }

        return new HttpResolutionResponse(
            404,
            "text/plain",
            Encoding.UTF8.GetBytes("route-not-found"),
            "route-not-found",
            $"No route matches '{path}'.");
    }

    private static HttpResolutionResponse HandleCert(Guid atomId, IAssetResolutionStore store)
    {
        if (!store.TryResolveCert(atomId, out var certificate) || certificate is null)
        {
            return new HttpResolutionResponse(
                404,
                "text/plain",
                Encoding.UTF8.GetBytes("atom-unresolved"),
                "atom-unresolved",
                $"No certificate registered for atom_id '{atomId}'.");
        }

        return new HttpResolutionResponse(
            200,
            "application/json",
            PhysicalAtomCertificateJsonCodec.Serialize(certificate));
    }

    private static HttpResolutionResponse HandleAsset(
        string assetHash,
        string assetVersion,
        IAssetResolutionStore store)
    {
        if (!store.TryResolveAsset(assetHash, assetVersion, out var asset) || asset is null)
        {
            return new HttpResolutionResponse(
                404,
                "text/plain",
                Encoding.UTF8.GetBytes("asset-unresolved"),
                "asset-unresolved",
                $"No asset registered for hash '{assetHash}' version '{assetVersion}'.");
        }

        return new HttpResolutionResponse(
            200,
            asset.ContentType,
            asset.AssetBytes);
    }
}
