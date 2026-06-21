using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

public sealed class InMemoryCertificationRecordStore : ICertificationRecordStore
{
    private readonly Dictionary<string, CertificationRecord> _records = new(StringComparer.OrdinalIgnoreCase);

    public void Save(CertificationRecord record) => _records[record.BrickId] = record;

    public CertificationRecord? Get(string brickId) =>
        _records.TryGetValue(brickId, out var record) ? record : null;

    public bool IsAdmitted(string brickId) =>
        _records.TryGetValue(brickId, out var record) &&
        record.Admitted &&
        record.Signed &&
        string.Equals(record.Status, "PASS", StringComparison.OrdinalIgnoreCase);
}
