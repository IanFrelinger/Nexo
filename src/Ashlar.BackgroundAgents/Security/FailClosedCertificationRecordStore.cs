using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.BackgroundAgents.Security;

/// <summary>
/// Fail-closed certification store used when no <see cref="ICertificationRecordStore"/> is wired.
/// All brick admission checks deny.
/// </summary>
public sealed class FailClosedCertificationRecordStore : ICertificationRecordStore
{
    public static readonly FailClosedCertificationRecordStore Instance = new();

    private FailClosedCertificationRecordStore() { }

    public CertificationRecord? Get(string brickId) => null;

    public bool IsAdmitted(string brickId) => false;

    public IReadOnlyList<CertificationRecord> All() => Array.Empty<CertificationRecord>();

    public void Save(CertificationRecord record) =>
        throw new InvalidOperationException("Fail-closed certification store cannot persist records.");
}
