// CLI tool to export certified brick packages.
//
// Exit codes, so a script can tell the outcomes apart without parsing text:
//   0  admitted — a content-bound, signed record was written and re-verified
//   1  usage
//   2  the gate REJECTED the brick (a signed FAIL verdict: correctness, mutation, dependency, ...)
//   3  the gate admitted, but the written record did not verify against the source (harness defect)
//   4  refused BEFORE the gate ran — the project could not be loaded into a certification request
//      (multi-file brick, a compile item outside the brick directory, a build failure, no witness,
//      ...). This is not a verdict about the brick's behaviour; the message names the fix.
using System.Text.Json;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Certification;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ExportCertifiedBrick <output-record.json> <brick-project-dir> [witness.json]");
    Console.Error.WriteLine("  witness.json defaults to the single *.witness.json file in <brick-project-dir>.");
    Console.Error.WriteLine("  Exit codes: 0 admitted, 1 usage, 2 gate rejected, 3 post-export verify failed, 4 refused before the gate ran.");
    return 1;
}

var recordPath = Path.GetFullPath(args[0]);
var brickDir = Path.GetFullPath(args[1]);

string witnessPath;
if (args.Length > 2)
{
    witnessPath = Path.GetFullPath(args[2]);
}
else
{
    // The default used to be one hard-coded file name (damage-resolver.witness.json), so every
    // other brick certified from this tool without a third argument stopped on a bare
    // FileNotFoundException for a file it never had. A brick directory carries its own witness;
    // find it by shape, and refuse by name when the shape is ambiguous or absent.
    var witnesses = Directory.Exists(brickDir)
        ? Directory.GetFiles(brickDir, "*.witness.json")
        : [];
    if (witnesses.Length != 1)
    {
        Console.Error.WriteLine(
            $"Refused before certification: {brickDir} holds {witnesses.Length} *.witness.json file(s), so there is no "
            + "single witness to replay. Fix: pass the witness path as the third argument, or keep exactly one "
            + "<brick>.witness.json beside the .csproj.");
        return 4;
    }

    witnessPath = witnesses[0];
}

if (!File.Exists(witnessPath))
{
    Console.Error.WriteLine(
        $"Refused before certification: witness spec not found at {witnessPath}. Fix: pass the path of an existing "
        + "witness JSON as the third argument, or place <brick>.witness.json beside the .csproj.");
    return 4;
}

// ASHLAR_CERT_NUGET_CONFIG is honoured, not cleared: scripts/pack-certified-brick-reuse.sh exports
// it one line before invoking this tool, precisely so the brick restores from the folder feed the
// script has just built. Clearing it here (as this file once did) made that export a no-op and left
// the restore to whatever the machine's default sources happened to hold.

CertificationRequest request;
try
{
    request = await BrickCertificationProjectLoader.LoadAsync(brickDir, witnessPath).ConfigureAwait(false);
}
catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException or DirectoryNotFoundException)
{
    // Every loader-stage refusal is one of these, carrying the gate's designed message — which
    // already names the fix. Print the message, not the stack trace: an unhandled exception here
    // used to end the run with a core dump (exit 134) and the actionable sentence buried under
    // seven frames of the loader's internals.
    Console.Error.WriteLine($"Refused before certification: {ex.Message}");
    return 4;
}

var gate = new CertificationGate(new CertificationRecordSigner());
var decision = await gate.CertifyAsync(request).ConfigureAwait(false);

// Kept apart on the console as on the record: the witness caught it (mutants_killed), the wall
// clock stopped it (killed_by_timeout), or running it killed its process (killed_by_crash).
var mutantSummary =
    $"mutants={decision.Record.TotalMutants} mutants_killed={decision.Record.KilledMutants.Count} "
    + $"killed_by_timeout={decision.Record.TimedOutMutants.Count}{IdList(decision.Record.TimedOutMutants)} "
    + $"killed_by_crash={decision.Record.CrashedMutants.Count}{IdList(decision.Record.CrashedMutants)}";

if (!decision.Admitted)
{
    Console.Error.WriteLine($"Certification failed: {decision.FailureCheck} {decision.Record.Reason}");
    Console.Error.WriteLine($"REJECT brick={decision.Record.BrickId} {mutantSummary}");
    return 2;
}

Console.WriteLine($"ADMIT brick={decision.Record.BrickId} escape_rate={decision.Record.EscapeRate} {mutantSummary}");

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

static string IdList(IReadOnlyList<string> ids) => ids.Count == 0 ? string.Empty : $"[{string.Join(", ", ids)}]";
