using FluentAssertions;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;
using Xunit;

namespace Ashlar.Tests.Kernel;

/// <summary>
/// Property-based tests for <see cref="AdmissionGate.Decide"/> (gold plan step 3). The
/// hand-written tests cover the semantic table's rows; these cover the space between the
/// rows: thousands of generated policy/proposal pairs, machine-checking the invariants that
/// must hold for EVERY input.
///
/// <para>Deliberately dependency-free: a seeded <see cref="Random"/> instead of FsCheck, so
/// no new package pin rides in on a test change (that supply-chain decision belongs to the
/// maintainer). Every failure message carries the seed and iteration, so any counterexample
/// reproduces exactly.</para>
/// </summary>
public sealed class AdmissionPropertyTests
{
    private const int IterationsPerSeed = 2_000;

    private static readonly string[] KindPool = ["brick", "tool", "capability", "policy", "model"];
    private static readonly string[] GatePool = ["sandbox", "tests", "security", "provenance", "style"];
    private static readonly string[] ModePool =
        [SelfExtendMode.Sealed, SelfExtendMode.Proposing, SelfExtendMode.SelfExtending];

    private sealed record Case(AshlarPolicy Policy, ExtensionProposal Proposal, int AdmittedInWindow)
    {
        public bool KindInEnvelope => Policy.SelfExtend.MayAdd.Contains(Proposal.Kind, StringComparer.Ordinal);

        public bool AllRequiredRanAndPassed => Policy.SelfExtend.GatesRequired.All(required =>
            Proposal.Courses.Any(c => c.Name == required && c.Passed));

        public bool WithinBudget => AdmittedInWindow < Policy.SelfExtend.Budget.Extensions;
    }

    private static Case Generate(Random rng)
    {
        var mode = ModePool[rng.Next(ModePool.Length)];
        var mayAdd = KindPool.Where(_ => rng.Next(3) == 0).ToList();
        if (rng.Next(2) == 0)
        {
            mayAdd.Add("brick");
        }
        var required = GatePool.Where(_ => rng.Next(2) == 0).ToList();
        if (required.Count == 0 && mode != SelfExtendMode.Sealed)
        {
            required.Add(GatePool[rng.Next(GatePool.Length)]);   // loader invariant: admitting modes declare gates
        }

        var policy = new AshlarPolicy
        {
            ApiVersion = "ashlar/v1",
            Kind = "Policy",
            Sandbox = new PolicySandbox { Root = "." },
            SelfExtend = new PolicySelfExtend
            {
                Mode = mode,
                Budget = new PolicyBudget { Extensions = rng.Next(0, 5), Window = "24h" },
                MayAdd = mayAdd.Distinct().ToList(),
                GatesRequired = required,
            },
            Never = [.. PolicyLoader.RequiredNeverEntries],
        };

        // Courses: sometimes a subset of required (missing), sometimes extras, random passes.
        var courses = new List<CourseResult>();
        foreach (var gate in required)
        {
            if (rng.Next(5) != 0)   // 20%: required course never runs
            {
                courses.Add(new CourseResult { Name = gate, Passed = rng.Next(4) != 0, Detail = "gen" });
            }
        }
        for (var extra = rng.Next(3); extra > 0; extra--)
        {
            courses.Add(new CourseResult { Name = "extra-" + rng.Next(3), Passed = rng.Next(2) == 0, Detail = "gen" });
        }

        var proposal = new ExtensionProposal
        {
            Id = "gen-" + rng.Next(100000),
            Kind = KindPool[rng.Next(KindPool.Length)],
            Summary = "generated",
            ProposedBy = "propgen",
            ProposedAt = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero),
            Courses = courses,
        };

        return new Case(policy, proposal, rng.Next(0, 7));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Invariants_hold_across_the_generated_space(int seed)
    {
        var rng = new Random(seed);
        for (var i = 0; i < IterationsPerSeed; i++)
        {
            var c = Generate(rng);
            var where = $"seed {seed}, iteration {i}: mode={c.Policy.SelfExtend.Mode}, kind={c.Proposal.Kind}, "
                      + $"mayAdd=[{string.Join(",", c.Policy.SelfExtend.MayAdd)}], "
                      + $"required=[{string.Join(",", c.Policy.SelfExtend.GatesRequired)}], "
                      + $"budget={c.Policy.SelfExtend.Budget.Extensions}, admitted={c.AdmittedInWindow}";

            var outcome = AdmissionGate.Decide(c.Policy, c.Proposal, c.AdmittedInWindow);

            // P0 — a reason, always: a verdict that does not teach is not a verdict.
            outcome.Reason.Should().NotBeNullOrWhiteSpace(where);

            // P1 — SOUNDNESS, the sentence the product stands on: Admitted only ever means
            // self-extending mode, kind inside the envelope, every required gate ran and
            // passed, and budget remained. No other path in.
            if (outcome.State == ProposalState.Admitted)
            {
                c.Policy.SelfExtend.Mode.Should().Be(SelfExtendMode.SelfExtending, where);
                c.KindInEnvelope.Should().BeTrue(where);
                c.AllRequiredRanAndPassed.Should().BeTrue(where);
                c.WithinBudget.Should().BeTrue(where);
            }

            // P2 — sealed always rejects.
            if (c.Policy.SelfExtend.Mode == SelfExtendMode.Sealed)
            {
                outcome.State.Should().Be(ProposalState.Rejected, where);
            }

            // P3 — anything short of Rejected implies the envelope and the gates held.
            if (outcome.State is ProposalState.Held or ProposalState.Admitted)
            {
                c.KindInEnvelope.Should().BeTrue(where);
                c.AllRequiredRanAndPassed.Should().BeTrue(where);
            }

            // P4 — determinism: the same inputs produce the same outcome.
            AdmissionGate.Decide(c.Policy, c.Proposal, c.AdmittedInWindow)
                .Should().Be(outcome, where);

            // P5 — course ORDER is irrelevant: the verdict is about what ran, not how the
            // list was assembled.
            var shuffled = c.Proposal with
            {
                Courses = c.Proposal.Courses.OrderBy(_ => rng.Next()).ToList(),
            };
            AdmissionGate.Decide(c.Policy, shuffled, c.AdmittedInWindow).State
                .Should().Be(outcome.State, where);

            // P6 — budget monotonicity: spending MORE of the budget can never turn a
            // non-admit into an admit.
            if (outcome.State != ProposalState.Admitted)
            {
                AdmissionGate.Decide(c.Policy, c.Proposal, c.AdmittedInWindow + 1).State
                    .Should().NotBe(ProposalState.Admitted, where);
            }
        }
    }
}
