using System.Text.Json;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// File-backed certification record store for CLI / spike workflows.
/// </summary>
public sealed class FileCertificationRecordStore : ICertificationRecordStore
{
    private readonly string _directory;
    private readonly CertificationRecordSigner _signer;

    /// <summary>Initializes a new file certification record store.</summary>
    /// <param name="directory">Directory holding one JSON record per brick.</param>
    /// <param name="signer">
    /// Signer used to RE-VERIFY every record on load. Defaults to the standard signer.
    /// </param>
    /// <remarks>
    /// Verification on load is the whole reason this store can be trusted. Records live
    /// as plain JSON on disk, so anything that can write the directory can set
    /// <c>Admitted: true</c>. The flags on a persisted record are therefore a claim,
    /// not evidence — only the HMAC signature is evidence, and it covers the record
    /// including its <c>ContentHash</c>, so editing either the admission flags or the
    /// certified content hash invalidates it.
    /// </remarks>
    public FileCertificationRecordStore(string directory, CertificationRecordSigner? signer = null)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _signer = signer ?? new CertificationRecordSigner();
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Save.</summary>
    public void Save(CertificationRecord record)
    {
        var path = Path.Combine(_directory, $"{record.BrickId}.json");
        var json = JsonSerializer.Serialize(record, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Loads a record, returning null unless its signature verifies.
    /// </summary>
    /// <remarks>
    /// A record that fails verification is reported as ABSENT rather than as an
    /// untrusted record. Callers treat "no record" as uncertified and refuse the brick,
    /// so a tampered or unsigned file fails closed. Returning it and hoping every caller
    /// re-checks would fail open on the first caller that forgot.
    /// </remarks>
    public CertificationRecord? Get(string brickId)
    {
        var path = Path.Combine(_directory, $"{brickId}.json");
        if (!File.Exists(path))
            return null;

        CertificationRecord? record;
        try
        {
            record = JsonSerializer.Deserialize<CertificationRecord>(File.ReadAllText(path), JsonOptions);
        }
        catch (JsonException)
        {
            // Unparseable on disk is indistinguishable from absent, and both mean
            // "no admission evidence".
            return null;
        }

        if (record is null)
            return null;

        return _signer.Verify(record) ? record : null;
    }

    /// <summary>Whether admitted.</summary>
    /// <remarks>
    /// Get already rejects anything whose signature does not verify, so the checks here
    /// run only against records whose contents are cryptographically vouched for.
    /// </remarks>
    public bool IsAdmitted(string brickId)
    {
        var record = Get(brickId);
        return record is not null &&
               record.Admitted &&
               record.Signed &&
               string.Equals(record.Status, "PASS", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Every verifiable record in the directory. Routed through <see cref="Get"/> per
    /// file, so tampered or unsigned records are excluded exactly as they are for point
    /// lookups — a ledger scan must never treat unverified JSON as evidence.
    /// </summary>
    public IReadOnlyList<CertificationRecord> All() =>
        Directory.EnumerateFiles(_directory, "*.json")
            .Select(path => Get(Path.GetFileNameWithoutExtension(path)))
            .Where(record => record is not null)
            .Select(record => record!)
            .ToArray();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
