using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>In-memory or persistent store for composition certification records.</summary>
public interface ICompositionCertificationRecordStore
{
    /// <summary>Persists a certification record.</summary>
    /// <param name="record">Signed composition certification record to store.</param>
    void Save(CompositionCertificationRecord record);

    /// <summary>Loads a record by composition id, or null when absent.</summary>
    /// <param name="compositionId">Composition identifier to look up.</param>
    CompositionCertificationRecord? Get(string compositionId);

    /// <summary>Returns true when the composition is admitted in this store.</summary>
    /// <param name="compositionId">Composition identifier to check.</param>
    bool IsAdmitted(string compositionId);
}
