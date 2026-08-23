using FluentAssertions;
using Ashlar.Commercial.GameDomain.Discord;
using Ashlar.Commercial.GameDomain.Playtest;

namespace Ashlar.Commercial.Tests.GameDomain.Discord;
/// <summary>Tests for forge notification orchestrator.</summary>
public class ForgeNotificationOrchestratorTests
{
    [Fact]
    public void UnconfiguredOrchestrator_SkipsWithoutError()
    {
        var config = new DiscordConfig();
        using var orchestrator = new ForgeNotificationOrchestrator(config, "/tmp");

        orchestrator.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task NotifyPlaytestComplete_WithNoWebhook_DoesNotThrow()
    {
        var config = new DiscordConfig();
        using var orchestrator = new ForgeNotificationOrchestrator(config, "/tmp");
        var report = new PlaytestReport { SessionId = "s1", ScenarioName = "Test" };

        var act = () => orchestrator.NotifyPlaytestCompleteAsync(report);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void IsConfigured_ReflectsWebhookPresence()
    {
        var withWebhook = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        using var configured = new ForgeNotificationOrchestrator(withWebhook, "/tmp");
        configured.IsConfigured.Should().BeTrue();

        var withoutWebhook = new DiscordConfig();
        using var unconfigured = new ForgeNotificationOrchestrator(withoutWebhook, "/tmp");
        unconfigured.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void HasBidirectional_ReflectsBotTokenAndChannel()
    {
        var bidirectional = new DiscordConfig
        {
            BotToken = "bot-token",
            ChannelId = "channel-123"
        };
        using var withGateway = new ForgeNotificationOrchestrator(bidirectional, "/tmp");
        withGateway.HasBidirectional.Should().BeTrue();

        var unidirectional = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        using var withoutGateway = new ForgeNotificationOrchestrator(unidirectional, "/tmp");
        withoutGateway.HasBidirectional.Should().BeFalse();
    }

    [Fact]
    public async Task HandleApprovalAsync_ApproveAll_ReturnsCorrectResult()
    {
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        using var orchestrator = new ForgeNotificationOrchestrator(config, "/tmp");
        var fixes = new[]
        {
            new ProposedFix { FixNumber = 1, Description = "Fix A", TargetSystem = "A.cs" },
            new ProposedFix { FixNumber = 2, Description = "Fix B", TargetSystem = "B.cs" }
        };

        var result = await orchestrator.HandleApprovalAsync("msg-1", "✅", fixes);

        result.Should().NotBeNull();
        result!.Action.Should().Be(ApprovalAction.ApproveAll);
        result.FixesToApply.Should().HaveCount(2);
        result.Summary.Should().Contain("2 fix(es) approved");
    }

    [Fact]
    public async Task HandleApprovalAsync_Reject_ReturnsCorrectResult()
    {
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        using var orchestrator = new ForgeNotificationOrchestrator(config, "/tmp");
        var fixes = new[]
        {
            new ProposedFix { FixNumber = 1, Description = "Fix A", TargetSystem = "A.cs" }
        };

        var result = await orchestrator.HandleApprovalAsync("msg-1", "❌", fixes);

        result.Should().NotBeNull();
        result!.Action.Should().Be(ApprovalAction.RejectAll);
        result.FixesToApply.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleApprovalAsync_UnknownEmoji_ReturnsUnknown()
    {
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        using var orchestrator = new ForgeNotificationOrchestrator(config, "/tmp");
        var fixes = new[]
        {
            new ProposedFix { FixNumber = 1, Description = "Fix A", TargetSystem = "A.cs" }
        };

        var result = await orchestrator.HandleApprovalAsync("msg-1", "🤷", fixes);

        result.Should().NotBeNull();
        result!.Action.Should().Be(ApprovalAction.Unknown);
    }

    [Fact]
    public void Constructor_ThrowsOnNullConfig()
    {
        var act = () => new ForgeNotificationOrchestrator(null!, "/tmp");
        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public async Task HandleApprovalAsync_ApproveSingle_ReturnsSelectedFix()
    {
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        using var orchestrator = new ForgeNotificationOrchestrator(config, "/tmp");
        var fixes = new[]
        {
            new ProposedFix { FixNumber = 1, Description = "Fix A", TargetSystem = "A.cs" },
            new ProposedFix { FixNumber = 2, Description = "Fix B", TargetSystem = "B.cs" }
        };

        var result = await orchestrator.HandleApprovalAsync("msg-1", "2️⃣", fixes);

        result.Should().NotBeNull();
        result!.Action.Should().Be(ApprovalAction.ApproveSingle);
        result.FixesToApply.Should().HaveCount(1);
        result.FixesToApply[0].FixNumber.Should().Be(2);
    }
}
