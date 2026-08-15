using Nexo.Core.Application.Certification.Models;
using Nexo.Certification.Contracts;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Dev HMAC signer for certification records, dual-writing an Ed25519 signature
/// when a private key is configured (explicitly or via
/// <see cref="CertificationRecordEd25519.PrivateKeyEnvVar"/>).
/// </summary>
public sealed class CertificationRecordSigner
{
    /// <summary>default dev key constant.</summary>
    public const string DefaultDevKey = CertificationRecordSigning.DefaultDevKey;

    private readonly string? _hmacKey;
    private readonly byte[]? _ed25519PrivateKey;

    /// <summary>Initializes a new certification record signer.</summary>
    public CertificationRecordSigner(string? hmacKey = null, string? ed25519PrivateKeyBase64 = null)
    {
        _hmacKey = string.IsNullOrWhiteSpace(hmacKey)
            ? Environment.GetEnvironmentVariable("NEXO_CERT_DEV_HMAC_KEY")
            : hmacKey;
        _ed25519PrivateKey = CertificationRecordEd25519.ResolvePrivateKey(ed25519PrivateKeyBase64);
    }

    /// <summary>Sign.</summary>
    public string Sign(CertificationRecord record) =>
        CertificationRecordSigning.Sign(CertificationRecordMapper.ToData(record), _hmacKey);

    /// <summary>
    /// Attaches signatures to a record: always the HMAC signature, plus the Ed25519
    /// signature and public key when a private key is configured. Both signatures
    /// cover the same canonical payload, including the public key.
    /// </summary>
    public CertificationRecord SignRecord(CertificationRecord record)
    {
        if (_ed25519PrivateKey is not null)
            record = record with { Ed25519PublicKey = CertificationRecordEd25519.DerivePublicKeyBase64(_ed25519PrivateKey) };

        var data = CertificationRecordMapper.ToData(record);
        var hmacSignature = CertificationRecordSigning.Sign(data, _hmacKey);
        var ed25519Signature = _ed25519PrivateKey is not null
            ? CertificationRecordEd25519.Sign(data, _ed25519PrivateKey)
            : null;
        return record with { Signature = hmacSignature, Ed25519Signature = ed25519Signature };
    }

    /// <summary>Verify. Enforces the Ed25519 signature whenever the record carries one.</summary>
    public bool Verify(CertificationRecord record)
    {
        var data = CertificationRecordMapper.ToData(record);
        if (!CertificationRecordSigning.VerifySignature(data, _hmacKey))
            return false;
        return string.IsNullOrWhiteSpace(data.Ed25519Signature) || CertificationRecordEd25519.VerifySignature(data);
    }
}
