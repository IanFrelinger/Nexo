using System.Text.Json;

namespace Nexo.Ingress.AwsSns;

/// <summary>
/// Verifies Amazon SNS HTTP(S) message signatures using the signing certificate URL embedded in the payload.
/// </summary>
public interface ISnsSignatureVerifier
{
    /// <summary>
    /// When <paramref name="skipVerification"/> is true, returns <c>true</c> without network or crypto work (labs only).
    /// </summary>
    Task<bool> IsAuthenticAsync(
        JsonElement snsMessage,
        HttpClient httpClient,
        bool skipVerification,
        CancellationToken cancellationToken = default);
}
