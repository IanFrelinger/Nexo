using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Persists signed certification records for admitted bricks.
/// </summary>
public interface ICertificationRecordStore
{
    void Save(CertificationRecord record);
    CertificationRecord? Get(string brickId);
    bool IsAdmitted(string brickId);
}
