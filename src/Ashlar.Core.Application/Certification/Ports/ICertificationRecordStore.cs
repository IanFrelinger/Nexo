using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Core.Application.Certification.Ports;

/// <summary>
/// Persists signed certification records for admitted bricks.
/// </summary>
public interface ICertificationRecordStore
{
    /// <summary>Persists a brick certification record.</summary>
    /// <param name="record">Signed certification record to store.</param>
    void Save(CertificationRecord record);

    /// <summary>Loads a record by brick id, or null when absent.</summary>
    /// <param name="brickId">Brick identifier to look up.</param>
    CertificationRecord? Get(string brickId);

    /// <summary>Returns true when the brick is admitted in this store.</summary>
    /// <param name="brickId">Brick identifier to check.</param>
    bool IsAdmitted(string brickId);

    /// <summary>
    /// Every verifiable record in the store, for ledger scans (autonomy spec R5.4 chain
    /// propagation, §8 depth-laundering detection). Implementations apply the same
    /// trust rules as <see cref="Get"/> — a record that fails verification is absent
    /// here too, never surfaced as data.
    /// </summary>
    IReadOnlyList<CertificationRecord> All();
}
