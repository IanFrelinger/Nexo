using FluentAssertions;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Certification.HotSwap;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Autonomy;

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
