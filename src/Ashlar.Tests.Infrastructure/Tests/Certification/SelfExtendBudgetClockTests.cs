using Ashlar.Manifest;
using Ashlar.Manifest.Admission;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The self-extension budget is a rolling window, and the window is computed entirely from the
/// <c>now</c> handed to the gate.
///
/// <para><b>The property under test.</b> <see cref="GateStore.AdmittedInWindowAsync"/> counts
/// admitted records whose <c>DecidedAt &gt;= now - window</c>. So moving <c>now</c> FORWARD past
/// the window slides the cutoff beyond every prior admission, the count falls to zero, and the
/// budget refills to full. On a node whose clock can jump — an NTP step after a long power-off,
/// a container starting with a bad RTC, a deliberately advanced host clock — that is a budget
/// reset, and the runtime cannot tell it from a day genuinely passing. Whatever controls the
/// clock controls how much the application may rewrite itself.</para>
///
/// <para><b>The direction that is safe.</b> A BACKWARD jump moves the cutoff earlier, so MORE
/// prior admissions fall inside the window and the budget is if anything tighter. The two are
/// not symmetric, and a remedy that guards the backward direction guards the harmless one.</para>
///
/// <para>These assertions are deterministic and hermetic — the clock is already an injected
/// parameter (<see cref="GateStore.ProposeAsync"/> takes <c>now</c>; only three call sites
/// hardcode <c>DateTimeOffset.UtcNow</c>), so this needs no container, no libfaketime and no
/// soak. It documents today's behaviour so a future fix has something to change.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class SelfExtendBudgetClockTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "ashlar-budget-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private GateStore Store() => new(_root);

    /// <summary>Self-extending, two admissions per rolling 24 hours, bricks only, no gates required.</summary>
    private static AshlarPolicy Policy(int extensions = 2, string window = "24h") => new()
    {
        SelfExtend = new PolicySelfExtend
        {
            Mode = SelfExtendMode.SelfExtending,
            Budget = new PolicyBudget { Extensions = extensions, Window = window },
            MayAdd = ["brick"],
            GatesRequired = [],
        },
    };

    private static ExtensionProposal Proposal(string id) => new()
    {
        Id = id,
        Kind = "brick",
        Summary = "add brick budget.clock",
        ProposedBy = "night-agent",
        ProposedAt = T0,
        Courses =
        [
            new CourseResult { Name = "sandbox", Passed = true, Detail = "confined" },
            new CourseResult { Name = "tests", Passed = true, Detail = "14 passed" },
            new CourseResult { Name = "security", Passed = true, Detail = "0 findings" },
        ],
    };

    [Fact]
    public async Task The_budget_is_spent_within_the_window()
    {
        var store = Store();
        var policy = Policy(extensions: 2);

        (await store.ProposeAsync(policy, Proposal("ext-1"), T0)).State.Should().Be(ProposalState.Admitted);
        (await store.ProposeAsync(policy, Proposal("ext-2"), T0)).State.Should().Be(ProposalState.Admitted);

        var third = await store.ProposeAsync(policy, Proposal("ext-3"), T0);

        third.State.Should().Be(ProposalState.Held,
            "budget exhaustion degrades to held — never to admit, and never to a silent drop");
        third.Reason.Should().Contain("budget");
    }

    /// <summary>
    /// The headline property. Everything before the jump is identical to the test above; the only
    /// difference is the <c>now</c> the gate is handed.
    /// </summary>
    [Fact]
    public async Task A_forward_clock_jump_past_the_window_refills_the_budget()
    {
        var store = Store();
        var policy = Policy(extensions: 2, window: "24h");

        await store.ProposeAsync(policy, Proposal("ext-1"), T0);
        await store.ProposeAsync(policy, Proposal("ext-2"), T0);
        (await store.ProposeAsync(policy, Proposal("ext-3"), T0)).State
            .Should().Be(ProposalState.Held, "the budget is spent at T0");

        // One second past the window. Nothing was admitted in between; only the clock moved.
        var afterJump = await store.ProposeAsync(
            policy, Proposal("ext-4"), T0 + TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1));

        afterJump.State.Should().Be(ProposalState.Admitted,
            "the cutoff (now - window) has slid past every prior admission, so the count is zero "
            + "and the budget reads as full — which is correct for time genuinely passing and is "
            + "indistinguishable, to this code, from a clock that was stepped forward");
    }

    /// <summary>
    /// The other direction, pinned so a future fix is aimed at the dangerous one. A backward jump
    /// keeps every prior admission inside the window; it cannot loosen the budget.
    /// </summary>
    [Fact]
    public async Task A_backward_clock_jump_does_not_refill_the_budget()
    {
        var store = Store();
        var policy = Policy(extensions: 2, window: "24h");

        await store.ProposeAsync(policy, Proposal("ext-1"), T0);
        await store.ProposeAsync(policy, Proposal("ext-2"), T0);

        var afterRewind = await store.ProposeAsync(
            policy, Proposal("ext-3"), T0 - TimeSpan.FromHours(12));

        afterRewind.State.Should().Be(ProposalState.Held,
            "rewinding the clock moves the cutoff earlier, so prior admissions still count — "
            + "the backward direction is conservative and is not the one worth guarding");
    }

    /// <summary>
    /// A jump that stays inside the window changes nothing, which is what makes the test above
    /// about the WINDOW boundary rather than about any movement of the clock at all.
    /// </summary>
    [Fact]
    public async Task A_forward_jump_inside_the_window_does_not_refill_the_budget()
    {
        var store = Store();
        var policy = Policy(extensions: 2, window: "24h");

        await store.ProposeAsync(policy, Proposal("ext-1"), T0);
        await store.ProposeAsync(policy, Proposal("ext-2"), T0);

        var stillInside = await store.ProposeAsync(
            policy, Proposal("ext-3"), T0 + TimeSpan.FromHours(23));

        stillInside.State.Should().Be(ProposalState.Held,
            "23 hours into a 24-hour window both admissions are still counted");
    }
}
