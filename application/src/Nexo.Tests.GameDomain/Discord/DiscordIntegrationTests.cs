using System.Text.Json;
using FluentAssertions;
using Nexo.GameDomain.Discord;
using Nexo.GameDomain.Playtest;

namespace Nexo.Tests.GameDomain.Discord;

public class DiscordIntegrationTests
{
    [Fact]
    public void DiscordConfig_LoadsFromEnvVars_WhenFileMissing()
    {
        var config = DiscordConfig.Load("/nonexistent/path");

        config.Should().NotBeNull();
        config.MaxUploadBytes.Should().Be(25 * 1024 * 1024);
        config.ClipHighlights.Should().BeTrue();
        config.MaxClipDurationSeconds.Should().Be(15);
    }

    [Fact]
    public void DiscordConfig_JsonRoundTrip()
    {
        var original = new DiscordConfig
        {
            WebhookUrl = "https://discord.com/api/webhooks/123/abc",
            BotToken = "token123",
            ChannelId = "999888777",
            OwnerUserId = "111222333",
            MaxUploadBytes = 10 * 1024 * 1024,
            ClipHighlights = false,
            MaxClipDurationSeconds = 30
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(original, options);
        var restored = JsonSerializer.Deserialize<DiscordConfig>(json, options);

        restored.Should().NotBeNull();
        restored!.WebhookUrl.Should().Be(original.WebhookUrl);
        restored.BotToken.Should().Be(original.BotToken);
        restored.ChannelId.Should().Be(original.ChannelId);
        restored.OwnerUserId.Should().Be(original.OwnerUserId);
        restored.MaxUploadBytes.Should().Be(original.MaxUploadBytes);
        restored.ClipHighlights.Should().Be(original.ClipHighlights);
        restored.MaxClipDurationSeconds.Should().Be(original.MaxClipDurationSeconds);
    }

    [Fact]
    public async Task DiscordRateLimiter_AllowsBurstWithinLimits()
    {
        var limiter = new DiscordRateLimiter();
        var counter = 0;

        var tasks = Enumerable.Range(0, 5).Select(_ =>
            limiter.ExecuteAsync(async () =>
            {
                Interlocked.Increment(ref counter);
                await Task.CompletedTask;
            }));

        await Task.WhenAll(tasks);

        counter.Should().Be(5);
    }

    [Fact]
    public void PlaytestReportFormatter_ProducesValidEmbed()
    {
        var report = CreateSampleReport();
        var embed = PlaytestReportFormatter.FormatSummaryEmbed(report);

        embed.Title.Should().Contain("Test Scenario");
        embed.Description.Should().Contain("session-001");
        embed.Color.Should().NotBeNull();
        embed.Fields.Should().NotBeNull();
        embed.Fields!.Count.Should().BeGreaterOrEqualTo(3);
        embed.Footer.Should().NotBeNull();
        embed.Footer!.Text.Should().Contain("Nexo Forge");
        embed.Timestamp.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PlaytestReportFormatter_GreenWhenNoIssues()
    {
        var report = new PlaytestReport
        {
            ScenarioName = "Clean Run",
            SessionId = "s1",
            BalanceFindings = [new BalanceFinding { Severity = "ok" }],
            MapFindings = [new MapFinding { Severity = "ok" }]
        };

        var embed = PlaytestReportFormatter.FormatSummaryEmbed(report);
        embed.Color.Should().Be(PlaytestReportFormatter.Green);
    }

    [Fact]
    public void PlaytestReportFormatter_YellowWhenWarnings()
    {
        var report = new PlaytestReport
        {
            ScenarioName = "Warning Run",
            SessionId = "s2",
            BalanceFindings = [new BalanceFinding { Severity = "warning", ItemName = "Pistol", Summary = "Slightly high" }]
        };

        var embed = PlaytestReportFormatter.FormatSummaryEmbed(report);
        embed.Color.Should().Be(PlaytestReportFormatter.Yellow);
    }

    [Fact]
    public void PlaytestReportFormatter_RedWhenCritical()
    {
        var report = new PlaytestReport
        {
            ScenarioName = "Critical Run",
            SessionId = "s3",
            BalanceFindings = [new BalanceFinding { Severity = "critical", ItemName = "Rocket", Summary = "OP" }],
            MapFindings = [new MapFinding { Severity = "warning", AreaName = "Mid", Summary = "Choke" }]
        };

        var embed = PlaytestReportFormatter.FormatSummaryEmbed(report);
        embed.Color.Should().Be(PlaytestReportFormatter.Red);
    }

    [Fact]
    public void PlaytestReportFormatter_FixProposals_NumberedCorrectly()
    {
        var fixes = new List<ProposedFix>
        {
            new() { FixNumber = 1, TargetSystem = "Weapons/Rocket", Description = "Reduce damage", IterateCommand = "cmd1" },
            new() { FixNumber = 2, TargetSystem = "Map/East", Description = "Widen corridor", IterateCommand = "cmd2" },
            new() { FixNumber = 3, TargetSystem = "Abilities/Shield", Description = "Lower duration", IterateCommand = "cmd3", Severity = "critical" }
        };

        var message = PlaytestReportFormatter.FormatFixProposals(fixes);

        message.Should().Contain("1️⃣");
        message.Should().Contain("2️⃣");
        message.Should().Contain("3️⃣");
        message.Should().Contain("Weapons/Rocket");
        message.Should().Contain("cmd1");
        message.Should().Contain("Proposed Fixes");
    }

    [Fact]
    public void PlaytestReportFormatter_FixProposals_EmptyList()
    {
        var message = PlaytestReportFormatter.FormatFixProposals([]);
        message.Should().Contain("No fixes proposed");
    }

    [Fact]
    public void VideoClipper_FindFfmpeg_DoesNotThrow()
    {
        var action = () => VideoClipper.FindFfmpeg();
        action.Should().NotThrow();
    }

    [Fact]
    public async Task DiscordWebhookClient_RejectsNonExistentFile()
    {
        using var client = new DiscordWebhookClient("https://example.com/webhook");
        var result = await client.SendFileAsync("/nonexistent/file.mp4");
        result.Should().BeFalse();
    }

    [Fact]
    public void DiscordEmbed_SerializesToJsonCorrectly()
    {
        var embed = new DiscordEmbed
        {
            Title = "Test Embed",
            Description = "Some description",
            Color = 0x22C55E,
            Fields =
            [
                new DiscordEmbedField { Name = "Field1", Value = "Value1", Inline = true }
            ],
            Footer = new DiscordEmbedFooter { Text = "Footer text" },
            Timestamp = "2025-01-01T00:00:00Z"
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(embed, options);

        json.Should().Contain("\"title\"");
        json.Should().Contain("Test Embed");
        json.Should().Contain("\"color\"");
        json.Should().Contain("\"fields\"");
        json.Should().Contain("Field1");
        json.Should().Contain("\"footer\"");
        json.Should().Contain("\"timestamp\"");
    }

    [Fact]
    public void PlaytestReportFormatter_FormatClipMessage()
    {
        var clip = new VideoClip
        {
            FileName = "highlight_01.mp4",
            Description = "Epic flank maneuver",
            StartSeconds = 60,
            EndSeconds = 72.5,
            FileSizeBytes = 4_500_000
        };

        var message = PlaytestReportFormatter.FormatClipMessage(clip);

        message.Should().Contain("Epic flank maneuver");
        message.Should().Contain("12.5s");
        message.Should().Contain("highlight_01.mp4");
    }

    private static PlaytestReport CreateSampleReport() => new()
    {
        SessionId = "session-001",
        ScenarioName = "Test Scenario",
        DurationSeconds = 480,
        BalanceFindings =
        [
            new BalanceFinding { ItemName = "RocketLauncher", Severity = "critical", Summary = "OP splash" },
            new BalanceFinding { ItemName = "Pistol", Severity = "ok", Summary = "Balanced" }
        ],
        MapFindings =
        [
            new MapFinding { AreaName = "East Wing", Severity = "warning", FindingType = "choke_point", Summary = "Bottleneck" }
        ],
        Performance = new PerformanceMetrics
        {
            AverageFps = 58.3,
            P99Fps = 42.1,
            DrawCalls = 1500,
            MemoryMb = 480,
            ActiveParticles = 2500
        },
        ProposedFixes =
        [
            new ProposedFix { FixNumber = 1, TargetSystem = "Weapons/RocketLauncher", Description = "Reduce splash" }
        ],
        VideoClips =
        [
            new VideoClip { FileName = "clip.mp4", Description = "Highlight", StartSeconds = 10, EndSeconds = 25, FileSizeBytes = 5_000_000 }
        ]
    };
}
