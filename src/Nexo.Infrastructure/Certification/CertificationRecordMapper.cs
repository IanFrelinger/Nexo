using Nexo.Core.Application.Certification.Models;
using Nexo.Certification.Contracts;

namespace Nexo.Infrastructure.Certification;

public static class CertificationRecordMapper
{
    public static CertificationRecordData ToData(CertificationRecord record) => new()
    {
        Status = record.Status,
        Stage = record.Stage,
        Admitted = record.Admitted,
        Signed = record.Signed,
        Timestamp = record.Timestamp,
        BrickId = record.BrickId,
        ContentHash = record.ContentHash,
        EscapeRate = record.EscapeRate,
        TotalMutants = record.TotalMutants,
        SurvivingMutants = record.SurvivingMutants,
        KilledMutants = record.KilledMutants,
        SurvivingMutantIds = record.SurvivingMutantIds,
        Signature = record.Signature,
        Reason = record.Reason,
        Gate = record.Gate
    };
}
