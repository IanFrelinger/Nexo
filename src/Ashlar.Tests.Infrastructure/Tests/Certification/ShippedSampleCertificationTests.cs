using FluentAssertions;
using Ashlar.Infrastructure.Certification;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The two shipped sample bricks must certify, as checked in, through the real on-disk path:
/// <see cref="BrickCertificationProjectLoader.LoadAsync"/> over the tracked directory, then
/// <see cref="CertificationGate"/>.
///
/// <para><b>Why this exists.</b> Nothing under <c>samples/</c> was inside the gate's own coverage.
/// <c>CrossProjectReuseTests</c> certifies a damage resolver it builds IN MEMORY, and the loader
/// tests write synthetic temp projects — so when the loader started taking the compiled source set
/// from the compiler's own record, both shipped samples stopped certifying (they had only ever
/// certified because <c>samples/Directory.Build.props</c> injected a compile item from outside
/// the brick directory, which the old <c>*.cs</c> glob could not see) and the export tool crashed
/// on both with a stack trace, and no test went red. The samples are what the docs send newcomers
/// to; a sample that does not certify is a claim the framework cannot back.</para>
///
/// <para><b>What it needs.</b> The .NET SDK (the loader shells out to <c>dotnet msbuild</c>) and
/// nuget.org, because the samples take <c>Ashlar.Brick.Contracts</c> as a package at its released
/// version — the same environment every other loader test that reaches a build already assumes
/// (<c>BrickEvaluatedCompileSetTests</c>). Like those, it clears <c>ASHLAR_CERT_NUGET_CONFIG</c>
/// so the restore is the plain one a consumer gets, not a portability feed.</para>
///
/// <para>One fact per sample, and each sample is loaded exactly once per process:
/// <c>Assembly.LoadFrom</c> of a second <c>HelloBrick.dll</c> from another path would fail with
/// "assembly with same name is already loaded", which reads like a gate refusal and is not one.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class ShippedSampleCertificationTests
{
    /// <summary>
    /// A cold NuGet cache plus a full <c>dotnet msbuild -restore</c>, the analyzer fence, and the
    /// mutation leg's per-mutant compiles. Healthy runs finish well inside a minute; this is a
    /// hang net for a wedged restore, not a budget.
    /// </summary>
    private const int SampleCertificationTimeout = 300_000;

    [Fact(Timeout = SampleCertificationTimeout)]
    public Task The_hello_brick_sample_certifies_as_checked_in() =>
        AssertShippedSampleAdmitsAsync(
            Path.Combine("samples", "hello-brick", "HelloBrick"),
            "hello-brick.witness.json",
            expectedBrickId: "hello");

    [Fact(Timeout = SampleCertificationTimeout)]
    public Task The_certified_damage_resolver_sample_certifies_as_checked_in() =>
        AssertShippedSampleAdmitsAsync(
            Path.Combine("samples", "certified-brick-reuse", "Ashlar.Certified.DamageResolver"),
            "damage-resolver.witness.json",
            expectedBrickId: "damage-resolver");

    private static async Task AssertShippedSampleAdmitsAsync(
        string sampleRelativeDirectory, string witnessFileName, string expectedBrickId)
    {
        Environment.SetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG", null);

        var brickDirectory = Path.Combine(TestPaths.FindRepoRoot(), sampleRelativeDirectory);
        var witnessPath = Path.Combine(brickDirectory, witnessFileName);
        Directory.Exists(brickDirectory).Should().BeTrue(
            "the shipped sample directory {0} is tracked in the repository", sampleRelativeDirectory);
        File.Exists(witnessPath).Should().BeTrue(
            "the shipped sample carries its witness beside the project at {0}", witnessPath);

        // Any refusal here is the loader saying the tracked sample cannot even be loaded into a
        // certification request — the exact state both samples were merged in. Let the designed
        // message surface as the failure rather than wrapping it: it names the fix.
        var request = await BrickCertificationProjectLoader.LoadAsync(brickDirectory, witnessPath)
            .ConfigureAwait(false);

        request.Brick.Id.Should().Be(expectedBrickId, "the loader instantiated the sample's own brick type");
        request.ProjectPath.Should().StartWith(brickDirectory, "the request was built from the tracked project");

        var gate = new CertificationGate(new CertificationRecordSigner());
        var decision = await gate.CertifyAsync(request).ConfigureAwait(false);

        decision.Admitted.Should().BeTrue(
            "the shipped sample {0} must be admitted by the gate as checked in, but failed the '{1}' leg: {2}",
            sampleRelativeDirectory, decision.FailureCheck, decision.Record.Reason);
        decision.Record.Signed.Should().BeTrue("an admitted record is a signed record");
        decision.Record.ContentHash.Should().NotBeNullOrWhiteSpace("the record is bound to the sample's source text");
        decision.Record.TotalMutants.Should().BePositive(
            "the mutation leg must have derived mutants from the sample's ExecuteAsync, or the escape rate is vacuous");
        decision.Record.SurvivingMutants.Should().Be(0, "every mutant must be observable by the shipped witness");
    }
}
