namespace Nexo.Certification.Physical.Resolution.Http;

/// <summary>
/// Headless HTTP resolution response (no live server required for tests).
/// </summary>
public sealed record HttpResolutionResponse(
    int StatusCode,
    string? ContentType,
    byte[] Body,
    string? FailureCode = null,
    string? Reason = null);
