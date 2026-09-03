using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Every attack that ever beat the certification gate is a checked-in fixture under
/// <c>tests/adversarial-corpus/&lt;id&gt;/</c> with the verdict the gate SHOULD give, and this theory
/// replays each one through the real on-disk path — <see cref="BrickCertificationProjectLoader.LoadAsync"/>
/// over the fixture directory, then <see cref="CertificationGate"/> — exactly as
/// <c>ShippedSampleCertificationTests</c> drives the samples.
///
/// <para><b>Why this exists.</b> Nine adversarial rounds produced the bricks that broke the gate:
/// a countdown loop that hung the certifier forever, a recursive helper that killed it with a
/// stack overflow, twin projects whose byte-identical source certified under different
/// <c>DefineConstants</c>, a symbol the SDK defines for free that split the program with no csproj
/// edit, witnesses with no teeth that a capped catalog could not expose, a helper body the catalog
/// never mutated. Every one of them lived only under <c>/tmp</c> in a dev container, and every one
/// of them dies when that container is recreated. A fix without its attack beside it is a claim the
/// next refactor can silently unmake. The corpus is where the attacks live now, and the cert-gate is
/// where they are replayed on every pull request.</para>
///
/// <para><b>What a fixture asserts.</b> <c>expect.json</c> names a verdict class — <c>ADMIT</c>,
/// <c>REJECT</c> (with the leg), <c>REFUSE</c> (the loader would not even build a request), or
/// <c>VERDICT</c> (either signed outcome, for a brick whose only remaining defect is a known harness
/// one that <c>knownIssue</c> must name) — plus invariants over the record that are robust to the
/// catalog widening: a count floor on timed-out or crashed mutants, a survivors floor or ceiling, a
/// fragment the reason must or must not contain, a fragment the signed compile options must carry, a
/// wall-clock ceiling on the gate itself. Never a mutant id, a hash or an exact count. The schema is
/// documented in <c>tests/adversarial-corpus/README.md</c> and read strictly here: an unknown key is a
/// failure, because a misspelt invariant that is silently ignored is an expectation nobody is held
/// to. A fixture directory with no <c>expect.json</c> fails here by name rather than being skipped,
/// for the same reason.</para>
///
/// <para><b>What it needs.</b> The .NET SDK and nuget.org, like every other loader test that
/// reaches a build; each fixture is a stock consumer project taking <c>Ashlar.Brick.Contracts</c>
/// as a package. Every fixture project has a UNIQUE assembly name (<c>Corpus.&lt;Id&gt;</c>):
/// several fixtures share a type name, and the loader's <c>Assembly.LoadFrom</c> of a second
/// assembly with the same simple name in one test process would hand back the first one's
/// types — a wrong brick judged under the right fixture's name.</para>
///
/// <para><b>Tier.</b> This class spawns a build and child processes per case; it carries
/// <c>Tier=Build</c> beside <c>Category=Certification</c> so a fast-tier filter can leave it out
/// without leaving the cert-gate.</para>
/// </summary>
[Trait("Category", "Certification")]
[Trait("Tier", "Build")]
public sealed class AdversarialCorpusTests
{
    private const string CorpusRelativePath = "tests/adversarial-corpus";

    /// <summary>The row the theory gets when the corpus root is not there, so the failure is a named test, not a discovery error.</summary>
    private const string CorpusMissing = "<corpus-missing>";

    /// <summary>
    /// The leg names a fixture may use for a REJECT, mapped onto
    /// <see cref="CertificationDecision.FailureCheck"/>. "fence" is the corpus's name for the
    /// analyzer leg — the docs and the adversarial rounds both call it that.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> LegNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["fence"] = "analyzer",
        ["analyzer"] = "analyzer",
        ["correctness"] = "correctness",
        ["mutation"] = "mutation",
        ["determinism"] = "determinism",
        ["dependency"] = "dependency",
    };

    private static readonly JsonSerializerOptions ExpectJson = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static string CorpusRoot() =>
        Path.Combine(TestPaths.FindRepoRoot(), CorpusRelativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Every fixture directory under the corpus root, by name, so the test name IS the fixture id.
    /// A directory whose name starts with <c>_</c> or <c>.</c> is shared material (a payload another
    /// fixture smuggles in, say), not a fixture.
    /// </summary>
    public static IEnumerable<object[]> Fixtures()
    {
        var root = CorpusRoot();
        if (!Directory.Exists(root))
        {
            yield return new object[] { CorpusMissing };
            yield break;
        }

        foreach (var name in Directory.GetDirectories(root)
                     .Select(Path.GetFileName)
                     .Where(name => name is { Length: > 0 } && name[0] != '_' && name[0] != '.')
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            yield return new object[] { name! };
        }
    }

    [Theory(Timeout = TestTimeouts.HostTouching)]
    [MemberData(nameof(Fixtures))]
    public async Task Every_attack_that_ever_beat_the_gate_gets_the_verdict_it_should(string fixture)
    {
        fixture.Should().NotBe(CorpusMissing,
            "{0} is tracked in the repository, so a missing directory means the test is running from the wrong tree ({1})",
            CorpusRelativePath, CorpusRoot());

        var directory = Path.Combine(CorpusRoot(), fixture);
        var expectPath = Path.Combine(directory, "expect.json");
        File.Exists(expectPath).Should().BeTrue(
            "every fixture under {0} carries an expect.json naming the verdict the gate should give; {1} has none, "
            + "so it is an attack the gate is not being held to — write the expectation (see the README there)",
            CorpusRelativePath, fixture);

        var expectation = Read(fixture, expectPath);

        var witnesses = Directory.GetFiles(directory, "*.witness.json");
        witnesses.Should().ContainSingle("a fixture is one brick under one witness; {0} has [{1}]",
            fixture, string.Join(", ", witnesses.Select(Path.GetFileName)));

        // The plain consumer restore, not a portability feed — as ShippedSampleCertificationTests.
        Environment.SetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG", null);

        CertificationRequest request;
        try
        {
            request = await BrickCertificationProjectLoader.LoadAsync(directory, witnesses[0]);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException or DirectoryNotFoundException)
        {
            // The set tools/Ashlar.CertifyBrick reports as exit 3: refused before the gate ran, with a
            // designed message that names the fix. Not a verdict about the brick.
            expectation.Expect.Should().Be(Verdicts.Refuse,
                "the loader refused fixture {0} before the gate ran, but the fixture expects {1}. Refusal: {2}",
                fixture, expectation.Expect, ex.Message);
            if (expectation.MessageContains is { } fragment)
            {
                ex.Message.Should().Contain(fragment,
                    "the refusal for {0} must name the shape it is refusing, or the author cannot act on it", fixture);
            }

            return;
        }

        expectation.Expect.Should().NotBe(Verdicts.Refuse,
            "fixture {0} expects the loader to refuse it, but it loaded into a certification request for brick '{1}'",
            fixture, request.Brick.Id);

        var gate = new CertificationGate(new CertificationRecordSigner());
        var clock = Stopwatch.StartNew();
        var decision = await gate.CertifyAsync(request);
        clock.Stop();
        var record = decision.Record;

        var because = decision.Admitted
            ? $"ADMIT (mutants={record.TotalMutants} killed={record.KilledMutants.Count} timedOut={record.TimedOutMutants.Count} crashed={record.CrashedMutants.Count})"
            : $"REJECT ({decision.FailureCheck}): {record.Reason}";

        switch (expectation.Expect)
        {
            case Verdicts.Admit:
                decision.Admitted.Should().BeTrue("fixture {0} must be admitted; the gate said {1}", fixture, because);
                break;

            case Verdicts.Reject:
                decision.Admitted.Should().BeFalse("fixture {0} must be rejected; the gate said {1}", fixture, because);
                AssertLeg(fixture, expectation, decision);
                break;

            case Verdicts.Verdict:
                // Either signed outcome is acceptable — the fixture exists to show the certifier
                // SURVIVES the brick and still says something. Reaching this line is that proof; the
                // leg, when given, still constrains a rejection.
                if (!decision.Admitted)
                    AssertLeg(fixture, expectation, decision);
                break;

            default:
                throw new InvalidOperationException(
                    $"fixture {fixture}: expect '{expectation.Expect}' is not one of ADMIT, REJECT, REFUSE, VERDICT");
        }

        if (decision.Admitted)
        {
            record.Signed.Should().BeTrue("fixture {0}: an admitted record is a signed record", fixture);
            new CertificationRecordSigner().Verify(record).Should().BeTrue("fixture {0}: an admitted record verifies as written", fixture);
        }
        else
        {
            record.Signed.Should().BeFalse("fixture {0}: a rejection is never a signed certificate", fixture);
        }

        // Every mutant is in exactly one list. This is the round-8 accounting and it holds for every
        // verdict, so it is asserted for every fixture rather than declared by each.
        record.TotalMutants.Should().Be(
            record.KilledMutants.Count + record.TimedOutMutants.Count + record.CrashedMutants.Count + record.SurvivingMutantIds.Count,
            "fixture {0}: killed, timed-out, crashed and surviving mutants partition the total", fixture);
        record.KilledMutants.Intersect(record.TimedOutMutants.Concat(record.CrashedMutants)).Should().BeEmpty(
            "fixture {0}: a kill the clock or a process death decided is never filed as a witness kill", fixture);

        AssertInvariants(fixture, expectation.Invariants, record, clock.Elapsed);
    }

    private static void AssertLeg(string fixture, Expectation expectation, CertificationDecision decision)
    {
        var legs = expectation.Legs();
        if (legs.Count == 0)
            return;

        var accepted = legs.Select(leg => LegNames.TryGetValue(leg, out var check)
                ? check
                : throw new InvalidOperationException($"fixture {fixture}: leg '{leg}' is not one of {string.Join("|", LegNames.Keys)}"))
            .ToList();

        accepted.Should().Contain(decision.FailureCheck,
            "fixture {0} must fail at the {1} leg, but failed at '{2}': {3}",
            fixture, string.Join(" or ", legs), decision.FailureCheck, decision.Record.Reason);
    }

    private static void AssertInvariants(string fixture, Invariants? invariants, CertificationRecord record, TimeSpan wall)
    {
        if (invariants is null)
            return;

        if (invariants.TimedOutMutantsMin is { } timedOut)
            record.TimedOutMutants.Count.Should().BeGreaterThanOrEqualTo(timedOut,
                "fixture {0}: the wall clock, not the witness, must have stopped at least {1} mutant(s); timed out were [{2}]",
                fixture, timedOut, string.Join(", ", record.TimedOutMutants));

        if (invariants.CrashedMutantsMin is { } crashed)
            record.CrashedMutants.Count.Should().BeGreaterThanOrEqualTo(crashed,
                "fixture {0}: at least {1} mutant(s) must have taken their process down and been recorded as crashes; crashed were [{2}]",
                fixture, crashed, string.Join(", ", record.CrashedMutants));

        if (invariants.SurvivorsMin is { } survivorsMin)
            record.SurvivingMutantIds.Count.Should().BeGreaterThanOrEqualTo(survivorsMin,
                "fixture {0}: the witness has no teeth for at least {1} mutant(s), and the catalog must surface them; survivors were [{2}]",
                fixture, survivorsMin, string.Join(", ", record.SurvivingMutantIds));

        if (invariants.SurvivorsMax is { } survivorsMax)
            record.SurvivingMutantIds.Count.Should().BeLessThanOrEqualTo(survivorsMax,
                "fixture {0}: survivors were [{1}]", fixture, string.Join(", ", record.SurvivingMutantIds));

        if (invariants.TotalMutantsMin is { } totalMin)
            record.TotalMutants.Should().BeGreaterThanOrEqualTo(totalMin,
                "fixture {0}: the mutation leg must have derived mutants, or the escape rate is vacuous", fixture);

        if (invariants.TotalMutantsMax is { } totalMax)
            record.TotalMutants.Should().BeLessThanOrEqualTo(totalMax,
                "fixture {0}: a brick rejected before the mutation leg has no mutants to report", fixture);

        if (invariants.WallSecondsMax is { } wallMax)
            wall.TotalSeconds.Should().BeLessThanOrEqualTo(wallMax,
                "fixture {0}: the gate must reach its verdict inside a bounded wall clock (a hung mutant is killed, not waited for)",
                fixture);

        foreach (var fragment in invariants.ReasonContainsAll())
            (record.Reason ?? string.Empty).Should().Contain(fragment,
                "fixture {0}: the verdict must name the mechanism; reason was: {1}", fixture, record.Reason);

        foreach (var fragment in invariants.ReasonNotContainsAll())
            (record.Reason ?? string.Empty).Should().NotContain(fragment,
                "fixture {0}: the verdict names something the judged program does not contain; reason was: {1}",
                fixture, record.Reason);

        if (invariants.GatePassConfigurationContains is { } configurationFragment)
            record.GatesPassed.Select(g => g.Configuration ?? string.Empty).Should().Contain(
                c => c.Contains(configurationFragment, StringComparison.Ordinal),
                "fixture {0}: the record must disclose how the legs ran; gate passes were [{1}]",
                fixture, string.Join(" | ", record.GatesPassed.Select(g => $"{g.Name}: {g.Configuration}")));

        var mustContain = invariants.CompileOptionsContainsAll();
        var mustNotContain = invariants.CompileOptionsNotContainsAll();
        if (mustContain.Count > 0 || mustNotContain.Count > 0)
        {
            // The signed record must say which PROGRAM was judged: the source text plus the options it
            // was compiled under. Two byte-identical sources are two programs when one is compiled
            // with a symbol the other lacks, and the record is where that difference is disclosed.
            var compileOptions = record.Inputs.FirstOrDefault(i => i.Kind == "compile-options");
            compileOptions.Should().NotBeNull(
                "fixture {0}: the record must carry the compile options the program was judged under; inputs were [{1}]",
                fixture, string.Join(", ", record.Inputs.Select(i => i.Kind)));
            foreach (var fragment in mustContain)
                compileOptions!.Id.Should().Contain(fragment,
                    "fixture {0}: the legs must have judged under the build's own options; recorded were: {1}",
                    fixture, compileOptions.Id);
            foreach (var fragment in mustNotContain)
                compileOptions!.Id.Should().NotContain(fragment,
                    "fixture {0}: the record claims an option this build did not use; recorded were: {1}",
                    fixture, compileOptions.Id);
        }
    }

    private static Expectation Read(string fixture, string path)
    {
        Expectation expectation;
        try
        {
            expectation = JsonSerializer.Deserialize<Expectation>(File.ReadAllText(path), ExpectJson)
                ?? throw new InvalidOperationException($"{path} is empty");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"fixture {fixture}: expect.json does not match the schema in {CorpusRelativePath}/README.md ({ex.Message})", ex);
        }

        expectation.Id.Should().Be(fixture, "expect.json names its own directory, so a copied fixture cannot answer for another");
        expectation.Class.Should().BeOneOf(new[] { "A", "B", "C", "D" },
            "a fixture is filed under one of the four adjudicated classes (README: A source-set divergence, "
            + "B author code in the certifier, C coverage truncation, D drift)");
        expectation.Origin.Should().NotBeNull("fixture {0}: origin names the round, the lens and the adjudicated severity", fixture);
        expectation.Origin!.Round.Should().NotBeNullOrWhiteSpace("fixture {0}: origin.round", fixture);
        expectation.Origin.Lens.Should().NotBeNullOrWhiteSpace("fixture {0}: origin.lens", fixture);
        expectation.Origin.Severity.Should().NotBeNullOrWhiteSpace("fixture {0}: origin.severity", fixture);
        expectation.Expect.Should().BeOneOf(new[] { Verdicts.Admit, Verdicts.Reject, Verdicts.Refuse, Verdicts.Verdict },
            "fixture {0}: expect names a verdict class", fixture);

        if (expectation.Expect == Verdicts.Verdict)
            expectation.KnownIssue.Should().NotBeNullOrWhiteSpace(
                "fixture {0}: VERDICT accepts either outcome, which is only honest when knownIssue says why the gate's own "
                + "answer cannot yet be pinned", fixture);
        if (expectation.MessageContains is not null)
            expectation.Expect.Should().Be(Verdicts.Refuse, "fixture {0}: messageContains constrains a refusal message", fixture);
        if (expectation.Legs().Count > 0)
            expectation.Expect.Should().BeOneOf(new[] { Verdicts.Reject, Verdicts.Verdict },
                "fixture {0}: leg constrains a rejection", fixture);

        return expectation;
    }

    private static IReadOnlyList<string> Strings(JsonElement element, string field) => element.ValueKind switch
    {
        JsonValueKind.String => [element.GetString()!],
        JsonValueKind.Array => element.EnumerateArray().Select(e => e.GetString()!).ToList(),
        JsonValueKind.Undefined or JsonValueKind.Null => [],
        _ => throw new InvalidOperationException($"expect.json: {field} must be a string or an array of strings"),
    };

    private static class Verdicts
    {
        public const string Admit = "ADMIT";
        public const string Reject = "REJECT";
        public const string Refuse = "REFUSE";
        public const string Verdict = "VERDICT";
    }

    /// <summary>The expect.json schema; see tests/adversarial-corpus/README.md. Unknown keys are refused.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record Expectation(
        string Id,
        Origin? Origin,
        string Class,
        string Expect,
        JsonElement Leg,
        string? Description,
        string? MessageContains,
        Invariants? Invariants,
        string? KnownIssue)
    {
        /// <summary>The leg is a single name or a list of acceptable names.</summary>
        public IReadOnlyList<string> Legs() => Strings(Leg, "leg");
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record Origin(string? Round, string? Lens, string? Severity);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record Invariants(
        int? TimedOutMutantsMin,
        int? CrashedMutantsMin,
        int? SurvivorsMin,
        int? SurvivorsMax,
        int? TotalMutantsMin,
        int? TotalMutantsMax,
        double? WallSecondsMax,
        JsonElement ReasonContains,
        JsonElement ReasonNotContains,
        string? GatePassConfigurationContains,
        JsonElement CompileOptionsContains,
        JsonElement CompileOptionsNotContains)
    {
        public IReadOnlyList<string> ReasonContainsAll() => Strings(ReasonContains, "invariants.reasonContains");
        public IReadOnlyList<string> ReasonNotContainsAll() => Strings(ReasonNotContains, "invariants.reasonNotContains");
        public IReadOnlyList<string> CompileOptionsContainsAll() => Strings(CompileOptionsContains, "invariants.compileOptionsContains");
        public IReadOnlyList<string> CompileOptionsNotContainsAll() => Strings(CompileOptionsNotContains, "invariants.compileOptionsNotContains");
    }
}
