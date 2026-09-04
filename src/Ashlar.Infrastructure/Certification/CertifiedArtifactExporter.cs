using System.Text.Json;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Writes the portable record and the gate-emitted assembly side by side. Consumers load
/// the assembly the certifier compiled; they do not recompile author source.
/// </summary>
public static class CertifiedArtifactExporter
{
    /// <summary>Default file name for the emitted assembly next to a record.</summary>
    public const string ArtifactFileName = "gate-emitted-brick.dll";

    /// <summary>Writes <paramref name="record"/> and <paramref name="artifact"/> under <paramref name="recordPath"/>.</summary>
    public static async Task WriteAsync(
        string recordPath,
        CertificationRecordData record,
        GateEmittedArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(artifact);

        var directory = Path.GetDirectoryName(Path.GetFullPath(recordPath))
            ?? throw new InvalidOperationException("record path has no directory");
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(
            record,
            new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await File.WriteAllTextAsync(recordPath, json, cancellationToken).ConfigureAwait(false);
        await File.WriteAllBytesAsync(
            Path.Combine(directory, ArtifactFileName),
            artifact.AssemblyBytes,
            cancellationToken).ConfigureAwait(false);
    }
}
