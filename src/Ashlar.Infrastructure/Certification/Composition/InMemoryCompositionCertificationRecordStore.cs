using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.Infrastructure.Certification.Composition;

/// <summary>In-memory store for composition certification records (tests and dev hosts).</summary>
public sealed class InMemoryCompositionCertificationRecordStore : ICompositionCertificationRecordStore
{
    private readonly Dictionary<string, CompositionCertificationRecord> _records =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Save.</summary>
    public void Save(CompositionCertificationRecord record) => _records[record.CompositionId] = record;

    /// <summary>Gets .</summary>
    public CompositionCertificationRecord? Get(string compositionId) =>
        _records.TryGetValue(compositionId, out var record) ? record : null;

    /// <summary>Whether admitted.</summary>
    public bool IsAdmitted(string compositionId) =>
        _records.TryGetValue(compositionId, out var record)
        && record.Admitted
        && record.Signed
        && string.Equals(record.Status, "PASS", StringComparison.OrdinalIgnoreCase);
}
