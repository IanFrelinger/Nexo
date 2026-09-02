using FluentAssertions;
using Ashlar.CLI.Commands;
using Ashlar.Manifest;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// The defect: <c>ashlar self-extend run</c> printed "ok / passed QA gates" against a SEALED
/// project. The verb never read the operator-owned policy at all, so the enforcement point was
/// simply absent on this path — and its absence read as permission.
///
/// <para><c>sealed</c> means nothing changes after deploy; a self-extend cycle changes files. The
/// two cannot both be true, and the one that must win is the operator's.</para>
/// </summary>
public sealed class SelfExtendSealedProjectTests : IDisposable
{
    private readonly string _dir;

    public SelfExtendSealedProjectTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-sx-sealed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private void WritePolicy(string mode)
    {
        ProjectScaffold.TryScaffold("sx-demo", out _, out var policy, out var reason).Should().BeTrue(reason);
        File.WriteAllText(Path.Combine(_dir, "ashlar.policy.yaml"), policy.Replace("mode: sealed", "mode: " + mode));
    }

    [Fact]
    public void A_sealed_project_refuses_the_cycle_and_names_the_dial()
    {
        WritePolicy(SelfExtendMode.Sealed);

        var refusal = SelfExtendCommand.SealedProjectRefusal(_dir);

        refusal.Should().NotBeNull("a sealed project is exactly what this verb must not write into");
        refusal!.Should().Contain("sealed");
        refusal.Should().Contain("ashlar policy set self_extend proposing",
            "the refusal names the fix — the same command the rest of the CLI recommends");
    }

    [Fact]
    public void A_proposing_project_may_run()
    {
        WritePolicy(SelfExtendMode.Proposing);

        SelfExtendCommand.SealedProjectRefusal(_dir).Should().BeNull();
    }

    [Fact]
    public void A_self_extending_project_may_run()
    {
        WritePolicy(SelfExtendMode.SelfExtending);

        SelfExtendCommand.SealedProjectRefusal(_dir).Should().BeNull();
    }

    [Fact]
    public void A_directory_that_is_not_a_project_is_unaffected()
    {
        // This verb predates projects and is still used on plain repos. A directory with no
        // envelope has no envelope to violate.
        SelfExtendCommand.SealedProjectRefusal(_dir).Should().BeNull();
    }

    [Fact]
    public void A_policy_that_will_not_load_is_refused_rather_than_treated_as_absent()
    {
        // "I could not parse the constraints" must never resolve to "then there are none".
        File.WriteAllText(Path.Combine(_dir, "ashlar.policy.yaml"), "apiVersion: ashlar/v1\nkind: NotAPolicy\n");

        var refusal = SelfExtendCommand.SealedProjectRefusal(_dir);

        refusal.Should().NotBeNull();
        refusal!.Should().Contain("does not load");
        refusal.Should().Contain("Fix ashlar.policy.yaml");
    }
}
