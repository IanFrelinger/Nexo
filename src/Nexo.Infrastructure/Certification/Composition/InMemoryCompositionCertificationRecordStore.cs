using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Composition;

public sealed class InMemoryCompositionCertificationRecordStore : ICompositionCertificationRecordStore
{
    private readonly Dictionary<string, CompositionCertificationRecord> _records =
        new(StringComparer.OrdinalIgnoreCase);

    public void Save(CompositionCertificationRecord record) => _records[record.CompositionId] = record;

    public CompositionCertificationRecord? Get(string compositionId) =>
        _records.TryGetValue(compositionId, out var record) ? record : null;

    public bool IsAdmitted(string compositionId) =>
        _records.TryGetValue(compositionId, out var record)
        && record.Admitted
        && record.Signed
        && string.Equals(record.Status, "PASS", StringComparison.OrdinalIgnoreCase);
}
