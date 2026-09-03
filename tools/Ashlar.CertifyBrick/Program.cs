// CLI tool for brick certification workflows.
//
// Exit codes, so a script can tell the outcomes apart without parsing text:
//   0  ADMIT — a signed PASS record was written
//   1  REJECT — the gate ran and signed a FAIL verdict (correctness, mutation, dependency, ...)
//   2  usage
//   3  refused BEFORE the gate ran — the project could not be loaded into a certification request
//      (multi-file brick, a compile item outside the brick directory, a build failure, no witness,
//      ...) or the harness itself failed. Not a verdict about the brick; the message names the fix.
//   4  an unexpected error the tool has no designed message for
using System.Text.Json;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Certification.Sdk.Extensions;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Ashlar.CertifyBrick <brickProjectDir> <witnessSpec.json> [recordOutputPath]");
    Console.Error.WriteLine("  Exit codes: 0 admit, 1 reject, 2 usage, 3 refused before the gate ran, 4 unexpected error.");
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
catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException or DirectoryNotFoundException)
{
    // Every loader-stage refusal (and every harness failure inside the gate) is one of these,
    // carrying a designed message that names the fix. It used to share exit code 1 with REJECT, so
    // a script could not tell "the gate judged this brick and failed it" from "the gate never ran".
    Console.Error.WriteLine($"Refused before certification: {ex.Message}");
    return 3;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Certification failed unexpectedly: {ex}");
    return 4;
}
