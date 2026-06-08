namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Result of inspecting vector map bytes on disk (format sniff only — not a full topology parse).
/// </summary>
public sealed record VectorMapInspectResult(
    bool Ok,
    string? ContentType,
    int ByteLength,
    string? FormatHint,
    string? Error);
