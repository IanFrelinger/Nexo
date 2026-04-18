using FluentAssertions;
using Nexo.GameDomain.Discord;
using Nexo.GameDomain.Playtest;

namespace Nexo.Tests.GameDomain.Discord;

public class ApprovalBridgeTests
{
    private static IReadOnlyList<ProposedFix> SampleFixes() => new[]
    {
        new ProposedFix { FixNumber = 1, Description = "Fix collision", TargetSystem = "Player.cs" },
        new ProposedFix { FixNumber = 2, Description = "Fix animation", TargetSystem = "Animator.cs" },
        new ProposedFix { FixNumber = 3, Description = "Fix audio", TargetSystem = "AudioManager.cs" }
    };

    [Fact]
    public void ApproveAll_ReturnsAllFixes()
    {
        var fixes = SampleFixes();

        var result = ApprovalBridge.Resolve("✅", fixes);

        result.Action.Should().Be(ApprovalAction.ApproveAll);
        result.FixesToApply.Should().HaveCount(3);
        result.Summary.Should().Contain("3 fix(es) approved");
    }

    [Fact]
    public void RejectAll_ReturnsEmpty()
    {
        var fixes = SampleFixes();

        var result = ApprovalBridge.Resolve("❌", fixes);

        result.Action.Should().Be(ApprovalAction.RejectAll);
        result.FixesToApply.Should().BeEmpty();
        result.Summary.Should().Contain("rejected");
    }

    [Fact]
    public void NumberEmoji_ReturnsSingleFix()
    {
        var fixes = SampleFixes();

        var result = ApprovalBridge.Resolve("1️⃣", fixes);

        result.Action.Should().Be(ApprovalAction.ApproveSingle);
        result.FixesToApply.Should().HaveCount(1);
        result.FixesToApply[0].FixNumber.Should().Be(1);
        result.FixesToApply[0].Description.Should().Be("Fix collision");
        result.Summary.Should().Contain("Fix #1 approved");
    }

    [Fact]
    public void NumberEmoji_ForNonexistentFix_ReturnsUnknown()
    {
        var fixes = SampleFixes();

        var result = ApprovalBridge.Resolve("9️⃣", fixes);

        result.Action.Should().Be(ApprovalAction.Unknown);
        result.FixesToApply.Should().BeEmpty();
    }

    [Fact]
    public void UnrecognizedEmoji_ReturnsUnknown()
    {
        var fixes = SampleFixes();

        var result = ApprovalBridge.Resolve("🤷", fixes);

        result.Action.Should().Be(ApprovalAction.Unknown);
        result.FixesToApply.Should().BeEmpty();
        result.Summary.Should().Contain("Unrecognized reaction: 🤷");
    }

    [Fact]
    public void ApproveAll_WithEmptyFixes_ReturnsEmptyList()
    {
        var result = ApprovalBridge.Resolve("✅", Array.Empty<ProposedFix>());

        result.Action.Should().Be(ApprovalAction.ApproveAll);
        result.FixesToApply.Should().BeEmpty();
        result.Summary.Should().Contain("0 fix(es) approved");
    }

    [Fact]
    public void NumberEmoji_Two_ReturnsCorrectFix()
    {
        var fixes = SampleFixes();

        var result = ApprovalBridge.Resolve("2️⃣", fixes);

        result.Action.Should().Be(ApprovalAction.ApproveSingle);
        result.FixesToApply.Should().HaveCount(1);
        result.FixesToApply[0].FixNumber.Should().Be(2);
    }
}
