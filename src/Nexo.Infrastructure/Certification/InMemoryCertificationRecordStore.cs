using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>In-memory store for certification records used in tests and dev hosts.</summary>
public sealed class InMemoryCertificationRecordStore : ICertificationRecordStore
{
    private readonly Dictionary<string, CertificationRecord> _records = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Save.</summary>
    public void Save(CertificationRecord record) => _records[record.BrickId] = record;

    /// <summary>Gets .</summary>
    public CertificationRecord? Get(string brickId) =>
        _records.TryGetValue(brickId, out var record) ? record : null;

    /// <summary>Whether admitted.</summary>
    public bool IsAdmitted(string brickId) =>
        _records.TryGetValue(brickId, out var record) &&
        record.Admitted &&
        record.Signed &&
        string.Equals(record.Status, "PASS", StringComparison.OrdinalIgnoreCase);

    /// <summary>Every stored record, for ledger scans.</summary>
    public IReadOnlyList<CertificationRecord> All() => _records.Values.ToArray();
}
