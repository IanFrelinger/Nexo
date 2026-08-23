using Ashlar.Core.Application.Environments;

namespace Ashlar.Core.Application.Environments.Ports;

/// <summary>
/// Vector map fetch result from an <see cref="IVectorMapDataProvider"/>.
/// </summary>
/// <param name="Payload">Raw bytes (UTF-8 XML, GeoJSON, etc.).</param>
/// <param name="ContentType">MIME-like hint (e.g. <c>application/xml</c>, <c>application/geo+json</c>).</param>
/// <param name="SourceDescription">Optional provenance string for logs.</param>
public sealed record VectorMapDataResult(
    ReadOnlyMemory<byte> Payload,
    string ContentType,
    string? SourceDescription = null);
