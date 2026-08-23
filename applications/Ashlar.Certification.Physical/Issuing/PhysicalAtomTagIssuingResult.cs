using Ashlar.Certification.Physical;
using Ashlar.Certification.Physical.Resolution;
using Ashlar.Certification.Physical.Tagging;

namespace Ashlar.Certification.Physical.Issuing;

/// <summary>Result of physical-atom tag issuing.</summary>
/// <param name="Succeeded">Whether tag issuing succeeded.</param>
/// <param name="Reference">Issued tag reference when tag issuing succeeds.</param>
/// <param name="QrPayload">QR code payload for the issued tag.</param>
/// <param name="NdefRecord">NDEF record bytes for the issued tag.</param>
/// <param name="FailureCode">Machine-readable failure code when tag issuing is refused.</param>
/// <param name="Reason">Human-readable failure reason when tag issuing is refused.</param>
public sealed record PhysicalAtomTagIssuingResult(
    bool Succeeded,
    PhysicalAtomTagReference? Reference,
    string? QrPayload,
    byte[]? NdefRecord,
    string? FailureCode,
    string? Reason)
{
    /// <summary>Creates a successful tag issuing result.</summary>
    public static PhysicalAtomTagIssuingResult Ok(
        PhysicalAtomTagReference reference,
        string qrPayload,
        byte[] ndefRecord) =>
        new(true, reference, qrPayload, ndefRecord, null, null);

    /// <summary>Creates a refused result with a failure code and reason.</summary>
    public static PhysicalAtomTagIssuingResult Refused(string code, string reason) =>
        new(false, null, null, null, code, reason);
}
