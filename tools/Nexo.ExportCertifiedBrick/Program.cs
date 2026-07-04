// CLI tool to export certified brick packages.
using System.Text.Json;
using Nexo.Certification.Contracts;
using Nexo.Infrastructure.Certification;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ExportCertifiedBrick <output-record.json> <brick-project-dir> [witness.json]");
    return 1;
}

var recordPath = Path.GetFullPath(args[0]);
var brickDir = Path.GetFullPath(args[1]);
var witnessPath = args.Length > 2
    ? Path.GetFullPath(args[2])
    : Path.Combine(brickDir, "damage-resolver.witness.json");

Environment.SetEnvironmentVariable("NEXO_CERT_NUGET_CONFIG", null);

var request = await BrickCertificationProjectLoader.LoadAsync(brickDir, witnessPath).ConfigureAwait(false);
var gate = new CertificationGate(new CertificationRecordSigner());
var decision = await gate.CertifyAsync(request).ConfigureAwait(false);

if (!decision.Admitted)
{
    Console.Error.WriteLine($"Certification failed: {decision.FailureCheck} {decision.Record.Reason}");
    return 2;
}

var data = CertificationRecordMapper.ToData(decision.Record);
var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
Directory.CreateDirectory(Path.GetDirectoryName(recordPath)!);
await File.WriteAllTextAsync(recordPath, json).ConfigureAwait(false);

var verify = CertificationTrustVerifier.Verify(data, request.SourceCode);
if (!verify.Trusted)
{
    Console.Error.WriteLine($"Post-export verify failed: {verify.FailureCode} {verify.Reason}");
    return 3;
}

Console.WriteLine($"Wrote content-bound record to {recordPath}");
Console.WriteLine($"contentHash={data.ContentHash}");
return 0;
