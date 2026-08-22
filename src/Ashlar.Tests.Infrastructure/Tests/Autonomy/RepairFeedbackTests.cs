using FluentAssertions;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Autonomy;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Certification.HotSwap;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Autonomy;

/// <summary>
/// The repair channel's disclosure discipline. The one property that matters most: at the
/// default level, the proposer must NEVER see an expected witness value — that leak is what
/// turns "certified against the contract" into "converged on the tests". Everything else
/// here pins that the levels are ordered, that locations survive at every level, and that
/// the human-facing record is untouched by any of it.
/// </summary>
[Trait("Category", "Certification")]
public sealed class RepairFeedbackTests
{
    // A distinctive expected value that must never appear in projected feedback.
    private const string Secret = "SECRET-WITNESS-VALUE-7731";

    [Fact]
    public void OwnOutput_NeverLeaksTheExpectedValue_ButShowsTheProposerItsOwn()
    {
        var decision = CorrectnessRejection(
            new WitnessFinding(0, WitnessFindingKind.Mismatch, "failureCode", Expected: $"\"{Secret}\"", Actual: "<null>"),
            new WitnessFinding(2, WitnessFindingKind.MissingKey, "isValid", Expected: $"\"{Secret}\""));

        var text = RepairFeedback.Render(decision, RepairFeedbackPolicy.Default());

        text.Should().NotContain(Secret, "the default level must never disclose the witness");
        text.Should().Contain("case 0").And.Contain("failureCode").And.Contain("you produced <null>",
            "the proposer's own output and the location are what it needs to repair");
        text.Should().Contain("case 2").And.Contain("isValid").And.Contain("was not produced");
        text.Should().Contain("REJECT at 'correctness'");
    }

    [Fact]
    public void CheckOnly_SharesNothingButTheFailingCheck()
    {
        var decision = CorrectnessRejection(
            new WitnessFinding(0, WitnessFindingKind.Mismatch, "failureCode", Expected: Secret, Actual: "<null>"));

        var text = RepairFeedback.Render(decision, RepairFeedbackPolicy.Blind());

        text.Should().Be("certification: REJECT at 'correctness'");
        text.Should().NotContain("failureCode").And.NotContain("case 0").And.NotContain(Secret);
    }

    [Fact]
    public void Full_IsOptIn_AndDoesDiscloseTheExpectedValue()
    {
        var decision = CorrectnessRejection(
            new WitnessFinding(0, WitnessFindingKind.Mismatch, "failureCode", Expected: $"\"{Secret}\"", Actual: "<null>"));

        var text = RepairFeedback.Render(decision, RepairFeedbackPolicy.FullDisclosure());

        text.Should().Contain(Secret, "full disclosure is exactly that — and it is opt-in for a reason");
        text.Should().Contain("you produced <null>");
    }

    [Fact]
    public void ProjectionNeverTouchesTheHumanFacingRecord()
    {
        var decision = CorrectnessRejection(
            new WitnessFinding(0, WitnessFindingKind.Mismatch, "failureCode", Expected: $"\"{Secret}\"", Actual: "<null>"));

        _ = RepairFeedback.Render(decision, RepairFeedbackPolicy.Blind());

        decision.Record.Reason.Should().Contain(Secret,
            "the digest and ledger keep full evidence; only the proposer view is narrowed");
    }

    [Fact]
    public void Mutation_ExposesLocationsNotAnswers_AndLocationsAreSwitchable()
    {
        var decision = MutationRejection("mutate-string-literal-33", "remove-statement-40");

        var withLocations = RepairFeedback.Render(decision, RepairFeedbackPolicy.Default());
        withLocations.Should().Contain("mutate-string-literal-33").And.Contain("escape_rate=0.20");

        var without = RepairFeedback.Render(decision, new RepairFeedbackPolicy { IncludeMutantLocations = false });
        without.Should().NotContain("mutate-string-literal-33", "some proposers do worse when told where to look")
            .And.Contain("escape_rate=0.20", "the rate itself is not a witness value");
    }

    [Fact]
    public void FindingsAreTruncatedDeterministically_ByPolicy()
    {
        var findings = Enumerable.Range(0, 6)
            .Select(i => new WitnessFinding(i, WitnessFindingKind.Mismatch, "k", Expected: "e", Actual: "a"))
            .ToArray();
        var decision = CorrectnessRejection(findings);

        var text = RepairFeedback.Render(decision, new RepairFeedbackPolicy { MaxFindings = 2 });

        text.Should().Contain("case 0").And.Contain("case 1").And.NotContain("case 2");
        text.Should().Contain("4 further finding(s) omitted");
    }

    [Fact]
    public void WithoutStructuredFindings_OwnOutputRefusesToFallBackToProse()
    {
        // An older decision path that produced only the reason string: at OwnOutput we must
        // NOT echo the prose, because the prose carries expected values.
        var decision = new CertificationDecision
        {
            Admitted = false,
            FailureCheck = "correctness",
            Record = Record("correctness", $"case 0: output['x'] expected \"{Secret}\" got <null>"),
            WitnessFindings = Array.Empty<WitnessFinding>(),
        };

        var text = RepairFeedback.Render(decision, RepairFeedbackPolicy.Default());

        text.Should().NotContain(Secret);
        text.Should().Contain("the witness failed");
    }

    [Fact]
    public void ReservedSummaryKey_IsDescribedInTheProposersVocabulary()
    {
        // Campaign 3: "output['$summary'] was not produced" three times, no repair — a proposer
        // has no way to know the reserved key means the output's Summary property.
        var decision = CorrectnessRejection(
            new WitnessFinding(0, WitnessFindingKind.MissingKey, Ashlar.Infrastructure.Certification.WitnessObservableOutput.SummaryKey, Expected: $"\"{Secret}\""));

        var text = RepairFeedback.Render(decision, RepairFeedbackPolicy.Default());

        text.Should().Contain("the output Summary (output.Summary) was not produced");
        text.Should().NotContain("$summary").And.NotContain(Secret);
    }

    [Fact]
    public void BuildFailure_ShowsTheCompilerDiagnosticsAboveCheckOnly_AndOnlyTheCheckAtCheckOnly()
    {
        var diagnostics = new[]
        {
            "line 31, col 17: CS0117: 'BrickOutput' does not contain a definition for 'Minor'",
            "line 40, col 13: CS0103: The name 'Set' does not exist in the current context",
        };

        var blind = RepairFeedback.RenderBuildFailure(diagnostics, RepairFeedbackPolicy.Blind());
        blind.Should().Be("certification: REJECT at 'build' — the candidate did not compile");

        var text = RepairFeedback.RenderBuildFailure(diagnostics, RepairFeedbackPolicy.Default());
        text.Should().Contain("2 diagnostic(s)");
        text.Should().Contain("line 31, col 17: CS0117").And.Contain("line 40, col 13: CS0103",
            "diagnostics describe the candidate's own text, never the witness — the safest feedback there is");
        text.Should().Contain("keep the class, namespace, Id and interface exactly as declared");
    }

    [Fact]
    public void BuildFailure_IsTruncatedByPolicy_AndSaysSoWhenNothingParsed()
    {
        var many = Enumerable.Range(1, 5).Select(i => $"line {i}, col 1: CS0001: error {i}").ToArray();
        var text = RepairFeedback.RenderBuildFailure(many, new RepairFeedbackPolicy { MaxFindings = 2 });
        text.Should().Contain("error 1").And.Contain("error 2").And.NotContain("error 3");
        text.Should().Contain("3 further diagnostic(s) omitted");

        var none = RepairFeedback.RenderBuildFailure(Array.Empty<string>(), RepairFeedbackPolicy.Default());
        none.Should().Contain("no parseable diagnostics");
    }

    [Fact]
    public void BuildDiagnostics_AreExtractedInCandidateCoordinates_DistinctAndCapped()
    {
        // The wrapper prepends a usings preamble; the transcript's line numbers are WRAPPED
        // coordinates and must map back to lines the proposer actually wrote.
        const string source = "namespace P;\npublic sealed class X\n{\n    public int Y() => Z;\n}\n";
        // Round trip through the real wrapper: find where the candidate's line 4 lands in the
        // wrapped text (preamble + audit-context insertion), exactly as the compiler sees it.
        var wrapped = Ashlar.Infrastructure.Certification.CandidateSourceWrapper.Wrap(source);
        var wrappedLine = wrapped.Split('\n').ToList().FindIndex(l => l.Contains("public int Y() => Z;", StringComparison.Ordinal)) + 1;
        wrappedLine.Should().BeGreaterThan(4, "the wrapper prepends lines");
        var preamble = Ashlar.Infrastructure.Certification.CandidateSourceWrapper.PreambleLineCount;
        var transcript =
            $"/ashlar-candidate/Candidate.cs({wrappedLine},23): error CS0103: The name 'Z' does not exist in the current context [/ashlar-candidate/Candidate.csproj]\n" +
            $"/ashlar-candidate/Candidate.cs({wrappedLine},23): error CS0103: The name 'Z' does not exist in the current context [/ashlar-candidate/Candidate.csproj]\n" +
            "    0 Warning(s)\n    2 Error(s)\n";

        var diags = SessionCandidateBuild.ExtractDiagnostics(transcript, source);

        diags.Should().HaveCount(1, "the same (code, message) is reported once");
        diags[0].Should().Be("line 4, col 23: CS0103: The name 'Z' does not exist in the current context");

        var flood = string.Concat(Enumerable.Range(1, 30).Select(i =>
            $"/ashlar-candidate/Candidate.cs({preamble + 1},{i}): error CS{1000 + i}: distinct {i} [/x.csproj]\n"));
        SessionCandidateBuild.ExtractDiagnostics(flood, source).Should().HaveCount(SessionCandidateBuild.MaxDiagnostics,
            "the first errors are the causes; the rest is cascade");
        SessionCandidateBuild.ExtractDiagnostics(string.Empty, source).Should().BeEmpty();
    }

    // --- helpers -------------------------------------------------------------------------

    private static CertificationDecision CorrectnessRejection(params WitnessFinding[] findings) => new()
    {
        Admitted = false,
        FailureCheck = "correctness",
        Record = Record("correctness",
            "Correctness check failed: " + string.Join("; ", findings.Select(f =>
                $"case {f.CaseIndex}: output['{f.Key}'] expected {f.Expected} got {f.Actual ?? "<null>"}"))),
        WitnessFindings = findings,
    };

    private static CertificationDecision MutationRejection(params string[] survivors) => new()
    {
        Admitted = false,
        FailureCheck = "mutation",
        Record = Record("mutation", "Mutation escape check failed") with
        {
            EscapeRate = 0.20,
            TotalMutants = 10,
            SurvivingMutants = survivors.Length,
            SurvivingMutantIds = survivors,
        },
    };

    private static CertificationRecord Record(string check, string reason) => new()
    {
        Status = "FAIL",
        Stage = "S0-S2",
        Admitted = false,
        Signed = false,
        Timestamp = DateTimeOffset.UtcNow,
        BrickId = "probe",
        ContentHash = "sha256:probe",
        Reason = reason,
        Gate = "test",
    };
}
