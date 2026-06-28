using Nexo.Certification.Contracts;
using Nexo.Certification.State;
using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Certification;

const string HmacKey = "attested-state-log-test-hmac";
const string SchemaCanonical = """{"version":1,"stateBinding":{"version":"witness-v1","hashLength":44}}""";
const string AlphaSource = "certified-behavior-alpha-v1";
const string BetaSource = "certified-behavior-beta-v1";

var outputRoot = args[0];
var behaviorRoot = Path.Combine(outputRoot, "behaviors");

var schema = new StateSchema(SchemaCanonical);
var builder = new CertifiedTransitionBuilder();

var alpha = CreateRecord("behavior-alpha", AlphaSource);
var beta = CreateRecord("behavior-beta", BetaSource);

var genesis = schema.ComputeBoundStateHash("genesis");
var idle = schema.ComputeBoundStateHash("phase:idle");
var armed = schema.ComputeBoundStateHash("phase:armed");
var ready = schema.ComputeBoundStateHash("phase:ready");

var t0 = builder.Create(genesis, "phase:advance", alpha.ContentHash!, idle, CertifiedTransition.GenesisPrevEntryHash);
var t1 = builder.Create(idle, "phase:advance", alpha.ContentHash!, armed, t0.EntryHash);
var t2 = builder.Create(armed, "phase:release", beta.ContentHash!, ready, t1.EntryHash);
var log = new AttestedStateLog(new[] { t0, t1, t2 });

Directory.CreateDirectory(behaviorRoot);
File.WriteAllText(Path.Combine(outputRoot, "state-schema.json"), AttestedStateLogWireFormat.SerializeSchema(schema));
File.WriteAllText(Path.Combine(outputRoot, "attested-state-log.json"), AttestedStateLogWireFormat.SerializeLog(log));

FileCertifiedBehaviorCatalog.WriteEntry(behaviorRoot, new CertifiedBehaviorEntry { ContentHash = alpha.ContentHash!, Record = alpha, Source = AlphaSource });
FileCertifiedBehaviorCatalog.WriteEntry(behaviorRoot, new CertifiedBehaviorEntry { ContentHash = beta.ContentHash!, Record = beta, Source = BetaSource });

Console.WriteLine($"Wrote fixtures to {outputRoot}");

static CertificationRecordData CreateRecord(string brickId, string source)
{
    var record = new CertificationRecordData
    {
        Status = "PASS",
        Stage = "witness",
        Admitted = true,
        Signed = true,
        Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
        BrickId = brickId,
        ContentHash = BrickContentHasher.ComputeSha256(source),
        EscapeRate = 0,
        TotalMutants = 1,
        SurvivingMutants = 0,
        KilledMutants = new[] { "m1" },
        SurvivingMutantIds = Array.Empty<string>()
    };

    return record with { Signature = CertificationRecordSigning.Sign(record, HmacKey) };
}
