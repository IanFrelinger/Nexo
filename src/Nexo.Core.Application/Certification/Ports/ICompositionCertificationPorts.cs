using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

public interface ICompositionCertificationGate
{
    Task<CompositionCertificationDecision> CertifyAsync(
        CompositionCertificationRequest request,
        CancellationToken cancellationToken = default);
}

public interface ICertifiedCompositionAdmission
{
    Task<CompositionCertificationDecision> CertifyAndAdmitAsync(
        CompositionCertificationRequest request,
        CancellationToken cancellationToken = default);

    bool IsAdmitted(string compositionId);
}

public interface ICompositionCertificationRecordStore
{
    void Save(CompositionCertificationRecord record);
    CompositionCertificationRecord? Get(string compositionId);
    bool IsAdmitted(string compositionId);
}
