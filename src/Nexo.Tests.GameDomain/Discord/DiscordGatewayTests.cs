using FluentAssertions;
using Nexo.GameDomain.Discord;

namespace Nexo.Tests.GameDomain.Discord;

public class DiscordGatewayTests
{
    private DiscordGatewayClient CreateClient() => new("test-bot-token", "test-channel-id");

    [Fact]
    public void RegisterApproval_StoresPending()
    {
        using var client = CreateClient();

        client.RegisterApproval("msg-1", _ => Task.CompletedTask);

        client.PendingCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleReactionAsync_FiresCallback()
    {
        using var client = CreateClient();
        string? capturedEmoji = null;
        client.RegisterApproval("msg-1", emoji =>
        {
            capturedEmoji = emoji;
            return Task.CompletedTask;
        });

        var handled = await client.HandleReactionAsync("msg-1", "✅", "user-1");

        handled.Should().BeTrue();
        capturedEmoji.Should().Be("✅");
    }

    [Fact]
    public async Task HandleReactionAsync_ReturnsFalse_ForUnknownMessage()
    {
        using var client = CreateClient();

        var handled = await client.HandleReactionAsync("nonexistent", "✅", "user-1");

        handled.Should().BeFalse();
    }

    [Fact]
    public void CleanupStaleApprovals_RemovesOldEntries()
    {
        using var client = CreateClient();
        client.RegisterApproval("msg-old", _ => Task.CompletedTask);

        client.CleanupStaleApprovals(TimeSpan.Zero);

        client.PendingCount.Should().Be(0);
    }

    [Fact]
    public void PendingCount_TracksCorrectly()
    {
        using var client = CreateClient();

        client.PendingCount.Should().Be(0);

        client.RegisterApproval("msg-1", _ => Task.CompletedTask);
        client.RegisterApproval("msg-2", _ => Task.CompletedTask);

        client.PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task HandleReactionAsync_RemovesApprovalAfterHandling()
    {
        using var client = CreateClient();
        client.RegisterApproval("msg-1", _ => Task.CompletedTask);

        await client.HandleReactionAsync("msg-1", "✅", "user-1");

        client.PendingCount.Should().Be(0);

        var secondHandle = await client.HandleReactionAsync("msg-1", "✅", "user-1");
        secondHandle.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ClearsPendingApprovals()
    {
        var client = CreateClient();
        client.RegisterApproval("msg-1", _ => Task.CompletedTask);
        client.RegisterApproval("msg-2", _ => Task.CompletedTask);

        client.Dispose();

        client.PendingCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_ThrowsOnNullBotToken()
    {
        var act = () => new DiscordGatewayClient(null!, "channel");
        act.Should().Throw<ArgumentNullException>().WithParameterName("botToken");
    }

    [Fact]
    public void Constructor_ThrowsOnNullChannelId()
    {
        var act = () => new DiscordGatewayClient("token", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("channelId");
    }
}
