// CLI tool for brick certification workflows.
using System.Text.Json;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Certification.Sdk.Extensions;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Ashlar.CertifyBrick <brickProjectDir> <witnessSpec.json> [recordOutputPath]");
    return 2;
}

var brickDir = Path.GetFullPath(args[0]);
var witnessPath = Path.GetFullPath(args[1]);
var recordPath = args.Length > 2
    ? Path.GetFullPath(args[2])
    : Path.Combine(brickDir, "..", "certification-record.json");

var recordDir = Path.GetDirectoryName(recordPath)!;
Directory.CreateDirectory(recordDir);

var store = new FileCertificationRecordStore(recordDir);
var signer = new CertificationRecordSigner();
var gate = new CertificationGate(signer);
var registry = new CertifiedBrickRegistry(store, signer);
var admission = new CertifiedBrickAdmission(gate, registry);

try
{
    var request = await BrickCertificationProjectLoader.LoadAsync(brickDir, witnessPath).ConfigureAwait(false);
    var decision = await admission.CertifyAndAdmitAsync(request).ConfigureAwait(false);

    await File.WriteAllTextAsync(
        recordPath,
        JsonSerializer.Serialize(decision.Record, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
        .ConfigureAwait(false);

    if (request.EmittedArtifact is { } artifact)
    {
        var artifactPath = Path.Combine(recordDir, CertifiedArtifactExporter.ArtifactFileName);
        await File.WriteAllBytesAsync(artifactPath, artifact.AssemblyBytes).ConfigureAwait(false);
    }

    if (!string.Equals(recordPath, Path.Combine(recordDir, $"{decision.Record.BrickId}.json"), StringComparison.OrdinalIgnoreCase))
    {
        await File.WriteAllTextAsync(
            Path.Combine(recordDir, $"{decision.Record.BrickId}.json"),
            JsonSerializer.Serialize(decision.Record, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
            .ConfigureAwait(false);
    }

    if (!decision.Admitted)
    {
        Console.Error.WriteLine($"REJECT ({decision.FailureCheck}): {decision.Record.Reason}");
        Console.Error.WriteLine($"Record: {recordPath}");
        return 1;
    }

    Console.WriteLine($"ADMIT brick={decision.Record.BrickId} escape_rate={decision.Record.EscapeRate} mutants_killed={decision.Record.KilledMutants.Count}");
    Console.WriteLine($"Record: {recordPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Certification failed: {ex.Message}");
    return 1;
}
