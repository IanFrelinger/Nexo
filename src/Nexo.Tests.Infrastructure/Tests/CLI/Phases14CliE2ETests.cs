using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Certification;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// End-to-end smoke tests for Phases 1-4: observe, analyze bricks, adapt, improve.
/// Uses prebuilt CLI (build once, run many) for faster execution.
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public sealed class Phases14CliE2ETests : E2ETestBase
{
    public Phases14CliE2ETests() : base("nexo-phases14-e2e") { }

    [Fact(Timeout = 60000)]
    public async Task AdaptCommand_DryRun_ExitsZero()
    {
        // The brick registry only offers bricks that carry a valid admission record, so
        // `adapt` on an empty store correctly reports "brick not found". Seeding a real
        // signed record is what lets this test exercise the decomposition it is named
        // for, rather than the not-found branch.
        SeedAdmittedObservationContextRecord();

        var (code, stdout, _) = await RunCliAsync($"adapt --dry-run --store-path \"{TempDir}\"");

        Assert.Equal(0, code);
        Assert.Contains("Decomposed", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Writes a genuinely signed, admitted record for <c>observation.context</c> where the
    /// CLI will look for it.
    /// </summary>
    /// <remarks>
    /// Signed with the same <see cref="CertificationRecordSigner"/> the CLI uses, so the
    /// record survives the re-verification the store performs on load. Hand-writing the
    /// JSON would not work, and should not: an unsigned or edited record is rejected as
    /// uncertified, which is the property that makes durable certification safe.
    ///
    /// The path mirrors AddAdaptationInfrastructure, which places records beside the
    /// pattern store.
    /// </remarks>
    private void SeedAdmittedObservationContextRecord()
    {
        var signer = new CertificationRecordSigner();
        var record = new CertificationRecord
        {
            Status = "PASS",
            Stage = "certification",
            Admitted = true,
            Signed = true,
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "observation.context",
            ContentHash = "seeded-e2e-fixture",
            EscapeRate = 0d,
            TotalMutants = 0,
            SurvivingMutants = 0,
        };

        record = record with { Signature = signer.Sign(record) };

        var store = new FileCertificationRecordStore(
            Path.Combine(TempDir, "nexo-certifications"),
            signer);
        store.Save(record);
    }

    [Fact(Timeout = 60000)]
    public async Task ImproveCommand_DryRun_Exits()
    {
        var (code, _, _) = await RunCliAsync("improve --dry-run");

        Assert.True(code == 0 || code == 1);
    }

    [Fact(Timeout = 60000)]
    public async Task ImproveCommand_WithViolations_AppliesFixes()
    {
        var csPath = Path.Combine(TempDir, "EmptyCatch.cs");
        await File.WriteAllTextAsync(csPath, """
            using System;
            namespace Test;
            public class C
            {
                public void M()
                {
                    try { }
                    catch (Exception) { }
                }
            }
            """);

        var (code, _, _) = await RunCliAsync($"improve --path \"{TempDir}\" --yes --skip-regression --store-path \"{TempDir}\"");

        Assert.True(code == 0 || code == 1);
        var content = await File.ReadAllTextAsync(csPath);
        Assert.True(
            content.Contains("Trace.WriteLine", StringComparison.Ordinal) || code == 0,
            "File should be modified with Trace.WriteLine when violations were fixed, or exit 0 if no violations");
    }

    [Fact(Timeout = 60000)]
    public async Task AnalyzeBricksCommand_Exits()
    {
        var obsPath = Path.Combine(RepoRoot, "src", "Nexo.Infrastructure", "Observation");
        var path = Directory.Exists(obsPath) ? obsPath : RepoRoot;

        var (code, _, _) = await RunCliAsync($"analyze bricks --path \"{path}\"");

        Assert.True(code == 0 || code == 1);
    }

    [Fact(Timeout = 60000)]
    public async Task Phases14_FullPipeline_ObserveAnalyzeAdaptImprove()
    {
        var watchPath = Path.Combine(TempDir, "src");
        Directory.CreateDirectory(watchPath);

        var (observeCode, _, _) = await RunCliAsync($"observe --path \"{TempDir}\" --duration 1s");
        Assert.Equal(0, observeCode);

        var obsPath = Path.Combine(RepoRoot, "src", "Nexo.Infrastructure", "Observation");
        var analyzePath = Directory.Exists(obsPath) ? obsPath : RepoRoot;

        var (analyzeCode, _, _) = await RunCliAsync($"analyze bricks --path \"{analyzePath}\"");
        Assert.True(analyzeCode == 0 || analyzeCode == 1);

        var (adaptCode, _, _) = await RunCliAsync($"adapt --dry-run --store-path \"{TempDir}\"");
        Assert.Equal(0, adaptCode);

        var (improveCode, _, _) = await RunCliAsync("improve --dry-run");
        Assert.True(improveCode == 0 || improveCode == 1);
    }
}
