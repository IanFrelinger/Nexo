using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Nexo.Ingress.AwsSns;

/// <summary>
/// RSA PKCS#1 verification for Amazon SNS HTTP(S) payloads using the signing certificate URL from the JSON payload.
/// </summary>
public sealed class SnsRsaSignatureVerifier : ISnsSignatureVerifier
{
    public async Task<bool> IsAuthenticAsync(
        JsonElement snsMessage,
        HttpClient httpClient,
        bool skipVerification,
        CancellationToken cancellationToken = default)
    {
        if (skipVerification)
            return true;

        if (!SnsCanonicalStringBuilder.TryBuild(snsMessage, out var stringToSign, out _))
            return false;

        if (!snsMessage.TryGetProperty("Signature", out var sigProp) || sigProp.ValueKind != JsonValueKind.String)
            return false;
        if (!snsMessage.TryGetProperty("SignatureVersion", out var verProp) || verProp.ValueKind != JsonValueKind.String)
            return false;
        if (!snsMessage.TryGetProperty("SigningCertURL", out var certUrlProp) || certUrlProp.ValueKind != JsonValueKind.String)
            return false;

        var certUrl = certUrlProp.GetString();
        if (string.IsNullOrWhiteSpace(certUrl) || !Uri.TryCreate(certUrl, UriKind.Absolute, out var uri))
            return false;
        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!IsTrustedSigningHost(uri.Host))
            return false;

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(sigProp.GetString() ?? string.Empty);
        }
        catch (FormatException)
        {
            return false;
        }

        string pemText;
        try
        {
            pemText = await httpClient.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return false;
        }

        using var cert = X509Certificate2.CreateFromPem(pemText);
        using var rsa = cert.GetRSAPublicKey();
        if (rsa is null)
            return false;

        var data = Encoding.UTF8.GetBytes(stringToSign);
        var version = verProp.GetString();
        if (string.Equals(version, "1", StringComparison.Ordinal))
            return rsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        if (string.Equals(version, "2", StringComparison.Ordinal))
            return rsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return false;
    }

    private static bool IsTrustedSigningHost(string host) =>
        host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".amazonaws.com.cn", StringComparison.OrdinalIgnoreCase);
}
