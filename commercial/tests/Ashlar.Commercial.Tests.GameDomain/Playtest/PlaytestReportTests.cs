using System.Text.Json;
using FluentAssertions;
using Ashlar.Commercial.GameDomain.Playtest;

namespace Ashlar.Commercial.Tests.GameDomain.Playtest;
/// <summary>Tests for playtest report.</summary>
public class PlaytestReportTests
{
    [Fact]
    public void PlaytestReport_DefaultValues()
    {
        var report = new PlaytestReport();

        report.SessionId.Should().BeEmpty();
        report.ScenarioName.Should().BeEmpty();
        report.DurationSeconds.Should().Be(0);
        report.BalanceFindings.Should().BeEmpty();
        report.MapFindings.Should().BeEmpty();
        report.Performance.Should().NotBeNull();
        report.ProposedFixes.Should().BeEmpty();
        report.VideoClips.Should().BeEmpty();
        report.TotalIssues.Should().Be(0);
    }

    [Fact]
    public void TotalIssues_CountsNonOkFindings()
    {
        var report = new PlaytestReport
        {
            BalanceFindings =
            [
                new BalanceFinding { Severity = "ok" },
                new BalanceFinding { Severity = "warning" },
                new BalanceFinding { Severity = "critical" }
            ],
            MapFindings =
            [
                new MapFinding { Severity = "ok" },
                new MapFinding { Severity = "warning" }
            ]
        };

        report.TotalIssues.Should().Be(3);
    }

    [Theory]
    [InlineData("ok")]
    [InlineData("warning")]
    [InlineData("critical")]
    public void BalanceFinding_SeverityLevels(string severity)
    {
        var finding = new BalanceFinding { Severity = severity };
        finding.Severity.Should().Be(severity);
    }

    [Theory]
    [InlineData("choke_point")]
    [InlineData("dead_zone")]
    [InlineData("spawn_camp")]
    [InlineData("sightline_issue")]
    public void MapFinding_Types(string findingType)
    {
        var finding = new MapFinding { FindingType = findingType };
        finding.FindingType.Should().Be(findingType);
    }

    [Fact]
    public void PerformanceMetrics_Defaults()
    {
        var perf = new PerformanceMetrics();

        perf.AverageFps.Should().Be(0);
        perf.P99Fps.Should().Be(0);
        perf.DrawCalls.Should().Be(0);
        perf.MemoryMb.Should().Be(0);
        perf.ActiveParticles.Should().Be(0);
    }

    [Fact]
    public void ProposedFix_Structure()
    {
        var fix = new ProposedFix
        {
            FixNumber = 1,
            TargetSystem = "Weapons/RocketLauncher",
            Description = "Reduce blast radius by 20%",
            IterateCommand = "unity-dev iterate --target Weapons/RocketLauncher --param blastRadius=-20%",
            Severity = "critical"
        };

        fix.FixNumber.Should().Be(1);
        fix.TargetSystem.Should().Be("Weapons/RocketLauncher");
        fix.Description.Should().NotBeEmpty();
        fix.IterateCommand.Should().Contain("iterate");
        fix.Severity.Should().Be("critical");
    }

    [Fact]
    public void VideoClip_Structure()
    {
        var clip = new VideoClip
        {
            FileName = "highlight_01.mp4",
            Description = "Rocket launcher triple kill",
            StartSeconds = 120.5,
            EndSeconds = 135.0,
            FileSizeBytes = 8_000_000
        };

        clip.FileName.Should().Be("highlight_01.mp4");
        clip.Description.Should().Contain("Rocket");
        clip.EndSeconds.Should().BeGreaterThan(clip.StartSeconds);
        clip.FileSizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PlaytestScenarioDescriptor_Defaults()
    {
        var scenario = new PlaytestScenarioDescriptor();

        scenario.Id.Should().HaveLength(12);
        scenario.Name.Should().BeEmpty();
        scenario.MapId.Should().BeEmpty();
        scenario.GameModeId.Should().BeEmpty();
        scenario.BotCount.Should().Be(10);
        scenario.TeamCount.Should().Be(2);
        scenario.DurationSeconds.Should().Be(480);
        scenario.WeaponPool.Should().BeEmpty();
        scenario.AbilityPool.Should().BeEmpty();
        scenario.BotDifficulty.Should().Be("medium");
        scenario.RecordVideo.Should().BeTrue();
        scenario.OutputDirectory.Should().Be(".ashlar/playtests");
    }

    [Fact]
    public void PlaytestReport_JsonRoundTrip()
    {
        var original = new PlaytestReport
        {
            SessionId = "abc123",
            ScenarioName = "Dust2 Deathmatch",
            DurationSeconds = 480,
            BalanceFindings =
            [
                new BalanceFinding
                {
                    ItemName = "RocketLauncher",
                    Severity = "critical",
                    KillRateMultiplier = 3.2,
                    Summary = "Overpowered splash damage"
                }
            ],
            MapFindings =
            [
                new MapFinding
                {
                    AreaName = "East Corridor",
                    Severity = "warning",
                    DeathPercentage = 45.0,
                    FindingType = "choke_point",
                    Summary = "Too many deaths concentrated here"
                }
            ],
            Performance = new PerformanceMetrics
            {
                AverageFps = 60.5,
                P99Fps = 45.2,
                DrawCalls = 1200,
                MemoryMb = 512,
                ActiveParticles = 3000
            },
            ProposedFixes =
            [
                new ProposedFix
                {
                    FixNumber = 1,
                    TargetSystem = "Weapons/RocketLauncher",
                    Description = "Reduce splash radius",
                    IterateCommand = "unity-dev iterate --target Weapons/RocketLauncher"
                }
            ],
            VideoClips =
            [
                new VideoClip
                {
                    FileName = "clip_01.mp4",
                    Description = "Triple kill highlight",
                    StartSeconds = 10,
                    EndSeconds = 25,
                    FileSizeBytes = 5_000_000
                }
            ]
        };

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        var json = JsonSerializer.Serialize(original, options);
        var restored = JsonSerializer.Deserialize<PlaytestReport>(json, options);

        restored.Should().NotBeNull();
        restored!.SessionId.Should().Be(original.SessionId);
        restored.ScenarioName.Should().Be(original.ScenarioName);
        restored.DurationSeconds.Should().Be(original.DurationSeconds);
        restored.BalanceFindings.Should().HaveCount(1);
        restored.BalanceFindings[0].ItemName.Should().Be("RocketLauncher");
        restored.BalanceFindings[0].KillRateMultiplier.Should().Be(3.2);
        restored.MapFindings.Should().HaveCount(1);
        restored.MapFindings[0].AreaName.Should().Be("East Corridor");
        restored.Performance.AverageFps.Should().Be(60.5);
        restored.Performance.DrawCalls.Should().Be(1200);
        restored.ProposedFixes.Should().HaveCount(1);
        restored.VideoClips.Should().HaveCount(1);
        restored.TotalIssues.Should().Be(2);
    }
}
