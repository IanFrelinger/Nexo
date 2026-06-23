using Nexo.Core.Application.Certification.Models;
using Nexo.Certification.Contracts;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Dev HMAC signer for certification records. Seam for real PKI later.
/// </summary>
public sealed class CertificationRecordSigner
{
    public const string DefaultDevKey = CertificationRecordSigning.DefaultDevKey;

    private readonly string? _hmacKey;

    public CertificationRecordSigner(string? hmacKey = null)
    {
        _hmacKey = string.IsNullOrWhiteSpace(hmacKey)
            ? Environment.GetEnvironmentVariable("NEXO_CERT_DEV_HMAC_KEY")
            : hmacKey;
    }

    public string Sign(CertificationRecord record) =>
        CertificationRecordSigning.Sign(CertificationRecordMapper.ToData(record), _hmacKey);

    public bool Verify(CertificationRecord record) =>
        CertificationRecordSigning.VerifySignature(CertificationRecordMapper.ToData(record), _hmacKey);
}
