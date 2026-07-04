using System.Text.Json;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// File-backed certification record store for CLI / spike workflows.
/// </summary>
public sealed class FileCertificationRecordStore : ICertificationRecordStore
{
    private readonly string _directory;

    /// <summary>Initializes a new file certification record store.</summary>
    public FileCertificationRecordStore(string directory)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Save.</summary>
    public void Save(CertificationRecord record)
    {
        var path = Path.Combine(_directory, $"{record.BrickId}.json");
        var json = JsonSerializer.Serialize(record, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>Gets .</summary>
    public CertificationRecord? Get(string brickId)
    {
        var path = Path.Combine(_directory, $"{brickId}.json");
        if (!File.Exists(path))
            return null;
        return JsonSerializer.Deserialize<CertificationRecord>(File.ReadAllText(path), JsonOptions);
    }

    /// <summary>Whether admitted.</summary>
    public bool IsAdmitted(string brickId)
    {
        var record = Get(brickId);
        return record is not null &&
               record.Admitted &&
               record.Signed &&
               string.Equals(record.Status, "PASS", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
