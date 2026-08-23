using System.Text.Json;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Tests.Infrastructure.Certification.Reuse;

/// <summary>
/// Project B consumer surface: verifier + untouched brick execution only (no gate, no generator).
/// </summary>
public static class ProjectBTrustConsumer
{
    public static CertificationTrustResult VerifyArtifact(string brickSource, CertificationRecordData record) =>
        CertificationTrustVerifier.Verify(record, brickSource);

    public static async Task<int> ExecuteDamageResolverSmokeAsync(
        string brickSource,
        string brickTypeName = "Ashlar.Certified.DamageResolver.DamageResolverBrick")
    {
        var brick = CertifiedBrickCompiler.InstantiateBrick(brickSource, brickTypeName);
        var input = new Ashlar.Core.Domain.Execution.BrickInput(new Dictionary<string, object>
        {
            ["baseDamage"] = 50,
            ["critMultiplierPercent"] = 100,
            ["armor"] = 10,
            ["isCrit"] = false
        });

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            new ProjectBAuditExecutionContext()).ConfigureAwait(false);

        return output.Get<int>("finalDamage");
    }

    public static CertificationRecordData LoadRecord(string json) =>
        JsonSerializer.Deserialize<CertificationRecordData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException("Failed to deserialize certification record.");

    public static CertificationRecordData FromInternalRecord(CertificationRecord record) => new()
    {
        Status = record.Status,
        Stage = record.Stage,
        Admitted = record.Admitted,
        Signed = record.Signed,
        Timestamp = record.Timestamp,
        BrickId = record.BrickId,
        ContentHash = record.ContentHash,
        EscapeRate = record.EscapeRate,
        TotalMutants = record.TotalMutants,
        SurvivingMutants = record.SurvivingMutants,
        KilledMutants = record.KilledMutants,
        SurvivingMutantIds = record.SurvivingMutantIds,
        Signature = record.Signature,
        Reason = record.Reason,
        Gate = record.Gate,
        SchemaVersion = record.SchemaVersion,
        GatesPassed = record.GatesPassed,
        Inputs = record.Inputs,
        Proposer = record.Proposer,
        Attempts = record.Attempts,
        Ed25519Signature = record.Ed25519Signature,
        Ed25519PublicKey = record.Ed25519PublicKey
    };

    public static void AssertProjectBProjectHasNoGateOrGeneratorReferences(string projectBcsprojPath)
    {
        var text = File.ReadAllText(projectBcsprojPath);
        if (text.Contains("Ashlar.Infrastructure", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Project B must not reference Ashlar.Infrastructure.");
        if (text.Contains("Adaptation.Generation", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Project B must not reference the generator.");
    }
}
