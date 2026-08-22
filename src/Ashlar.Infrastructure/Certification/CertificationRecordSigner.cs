using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Certification.Contracts;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Dev HMAC signer for certification records, dual-writing an Ed25519 signature
/// when a private key is configured (explicitly or via
/// <see cref="CertificationRecordEd25519.PrivateKeyEnvVar"/>).
///
/// <para><b>Key resolution is loud.</b> With no explicit key and no
/// <c>ASHLAR_CERT_DEV_HMAC_KEY</c>, every record is signed and verified with the COMMITTED
/// <see cref="CertificationRecordSigning.DefaultDevKey"/>, which anyone with the source can
/// reproduce — a certificate under it proves integrity against accident, not against an
/// adversary. The constructor logs a warning in that state (when given a logger) and
/// exposes it as <see cref="UsesDevKey"/>, so a host cannot run production admissions on
/// the dev key without the fact being on the record.</para>
/// </summary>
public sealed class CertificationRecordSigner
{
    /// <summary>default dev key constant.</summary>
    public const string DefaultDevKey = CertificationRecordSigning.DefaultDevKey;

    private readonly string? _hmacKey;
    private readonly byte[]? _ed25519PrivateKey;

    /// <summary>Initializes a new certification record signer.</summary>
    /// <param name="hmacKey">Explicit HMAC key; null falls back to <c>ASHLAR_CERT_DEV_HMAC_KEY</c>, then the committed dev key.</param>
    /// <param name="ed25519PrivateKeyBase64">Optional Ed25519 private key for the dual-write signature.</param>
    /// <param name="logger">Optional logger; receives the dev-key warning when the committed key is in effect.</param>
    public CertificationRecordSigner(
        string? hmacKey = null,
        string? ed25519PrivateKeyBase64 = null,
        ILogger<CertificationRecordSigner>? logger = null)
    {
        _hmacKey = string.IsNullOrWhiteSpace(hmacKey)
            ? Environment.GetEnvironmentVariable(CertificationRecordSigning.HmacKeyEnvVar)
            : hmacKey;
        _ed25519PrivateKey = CertificationRecordEd25519.ResolvePrivateKey(ed25519PrivateKeyBase64);
        UsesDevKey = CertificationRecordSigning.UsesDevKey(_hmacKey);
        if (UsesDevKey)
            WarnDevKey(logger, nameof(CertificationRecordSigner));
    }

    /// <summary>
    /// True when records are signed with the committed development key (no explicit key and
    /// no <c>ASHLAR_CERT_DEV_HMAC_KEY</c>): every signature this instance mints or accepts is
    /// forgeable by anyone with the source.
    /// </summary>
    public bool UsesDevKey { get; }

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

    /// <summary>
    /// The one dev-key warning, shared with the composition signer so both surfaces say the
    /// same thing. Nothing about the key itself is logged.
    /// </summary>
    internal static void WarnDevKey(ILogger? logger, string signerName) =>
        logger?.LogWarning(
            "{Signer} is signing and verifying certification records with the COMMITTED development HMAC key "
            + "(no explicit key, {EnvVar} unset). Anyone with the source can forge a record that verifies here; "
            + "these certificates prove integrity against accident, not against an adversary. Set {EnvVar} to a "
            + "secret before admitting anything you would not admit unsigned.",
            signerName, CertificationRecordSigning.HmacKeyEnvVar, CertificationRecordSigning.HmacKeyEnvVar);
}
