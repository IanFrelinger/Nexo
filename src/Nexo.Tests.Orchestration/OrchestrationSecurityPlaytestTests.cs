using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Agents.Templates;
using Nexo.Orchestration.Agents.Security;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Assets.Ports;
using Nexo.Orchestration.Playtest.Models;
using Xunit;

namespace Nexo.Tests.Orchestration;

/// <summary>Tests for orchestration security playtest.</summary>
public class OrchestrationSecurityPlaytestTests
{
    /// <summary>Spec.</summary>
    /// <param name=""Security"">"security".</param>
    private static AgentSpawnSpec Spec(string domain = "Security") => new()
    {
        AgentId = "sec-1",
        Domain = domain,
        Goal = "Analyze",
    };

    [Fact]
    public async Task VulnerabilityScanner_detects_common_patterns()
    {
        var artifacts = new List<SecurityArtifact>
        {
            new()
            {
                Name = "sql",
                Type = "code",
                Content = "var sql = \"SELECT * FROM users WHERE id = \" + userId;",
                Language = "csharp",
            },
            new()
            {
                Name = "xss",
                Type = "code",
                Content = "element.innerHTML = userInput; document.write(x);",
                Language = "javascript",
            },
            new()
            {
                Name = "secrets",
                Type = "code",
                Content = "password = \"secret\"; api_key = \"k\"; token = \"t\";",
                Language = "text",
            },
            new()
            {
                Name = "exec",
                Type = "code",
                Content = "exec(cmd); system(shell);",
                Language = "text",
            },
        };

        var vulns = await new VulnerabilityScanner(NullLogger<VulnerabilityScanner>.Instance)
            .ScanAsync(artifacts);
        vulns.Should().NotBeEmpty();
        vulns.Should().Contain(v => v.Type.Contains("SQL", StringComparison.OrdinalIgnoreCase));
        vulns.Should().Contain(v => v.Severity == VulnerabilitySeverity.High);
    }

    [Fact]
    public async Task ComplianceChecker_evaluates_owasp_cwe_and_pci()
    {
        var artifacts = new List<SecurityArtifact>
        {
            new() { Name = "a", Type = "code", Content = "eval(malicious);", Language = "js" },
            new() { Name = "b", Type = "code", Content = "SELECT * FROM users WHERE 1=1", Language = "sql" },
            new() { Name = "c", Type = "code", Content = "card_number = 4111111111111111", Language = "text" },
        };

        var checker = new ComplianceChecker(NullLogger<ComplianceChecker>.Instance, new List<string> { "OWASP", "CWE", "PCI-DSS" });
        var issues = await checker.CheckAsync(artifacts);
        issues.Should().NotBeEmpty();
        issues.Select(i => i.Standard).Should().Contain("OWASP");
    }

    [Fact]
    public async Task SecurityAnalysisAgent_executes_with_scanner_and_compliance()
    {
        var deps = new Dictionary<string, object>
        {
            ["code"] = JsonDocument.Parse("""
                {"code":"var sql = \"SELECT * FROM t WHERE id = \" + id; element.innerHTML = x; password='p'; exec(cmd);"}
                """).RootElement,
            ["notes"] = "plain text artifact",
        };

        var agent = new SecurityAnalysisAgent(
            Spec(),
            NullLogger<Nexo.Orchestration.Agents.BaseAgent>.Instance,
            model: null,
            scanner: new VulnerabilityScanner(),
            complianceChecker: new ComplianceChecker());

        await agent.InitializeAsync();
        var result = await agent.ExecuteAsync(deps);
        result.Should().BeOfType<SecurityAnalysisResult>();
        var report = (SecurityAnalysisResult)result;
        report.Vulnerabilities.Should().NotBeEmpty();
        report.Metadata.Should().ContainKey("artifactCount");
    }

    [Fact]
    public async Task SecurityAnalysisAgent_uses_llm_when_model_provided()
    {
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("Risk score elevated. Recommend patching SQL injection."));

        var deps = new Dictionary<string, object>
        {
            ["code"] = JsonDocument.Parse("""{"code":"password='x'"}""").RootElement,
        };

        var agent = new SecurityAnalysisAgent(
            Spec(),
            NullLogger<Nexo.Orchestration.Agents.BaseAgent>.Instance,
            model.Object,
            new VulnerabilityScanner(),
            new ComplianceChecker());

        await agent.InitializeAsync();
        var result = (SecurityAnalysisResult)await agent.ExecuteAsync(deps);
        result.AdvancedAnalysis.Should().NotBeNull();
        result.AdvancedAnalysis!.Summary.Should().Contain("Risk");
    }

    [Fact]
    public void Playtest_models_round_trip()
    {
        var config = new PlaytestConfiguration
        {
            PlayerCount = 4,
            MaxDuration = TimeSpan.FromMinutes(10),
            Scenario = "tutorial",
            FocusAreas = new[] { "combat" },
            PlayerProfiles = new[] { "aggressive" },
        };
        var session = new PlaytestSession
        {
            SessionId = "s1",
            BuildId = "b1",
            Configuration = config,
            StartedAt = DateTimeOffset.UtcNow,
            Status = PlaytestStatus.Running,
            Players = new[]
            {
                new AIPlayerSession
                {
                    PlayerId = "p1",
                    Profile = "explorer",
                    Actions = new[]
                    {
                        new PlayerAction
                        {
                            ActionType = "move",
                            Timestamp = DateTimeOffset.UtcNow,
                            Parameters = new Dictionary<string, object> { ["x"] = 1 },
                            Reasoning = "explore",
                        },
                    },
                    Metrics = new PlayerMetrics
                    {
                        TotalActions = 1,
                        Kills = 0,
                        Deaths = 0,
                        ObjectivesCompleted = 1,
                        AverageResponseTimeMs = 12.5,
                        TotalPlayTime = TimeSpan.FromMinutes(5),
                    },
                },
            },
            Metrics = new PlaytestMetrics
            {
                TotalDuration = TimeSpan.FromMinutes(5),
                TotalEvents = 10,
                AverageFrameRate = 60,
                MinFrameRate = 30,
                PeakMemoryBytes = 1024,
                CrashCount = 0,
                ErrorCount = 0,
            },
        };
        session.Players.Should().ContainSingle();
        session.Metrics!.TotalEvents.Should().Be(10);
    }

    [Fact]
    public void Asset_audio_port_records_round_trip()
    {
        var audioReq = new AudioGenerationRequest
        {
            Prompt = "explosion",
            Type = AudioType.Music,
            DurationSeconds = 30,
            Genre = "ambient",
            Bpm = 120,
        };
        audioReq.Bpm.Should().Be(120);

        var speech = new SpeechGenerationRequest
        {
            Text = "hello",
            VoiceId = "v1",
            Speed = 1.1,
            Pitch = 0.9,
        };
        speech.VoiceId.Should().Be("v1");

        var generated = new GeneratedAudio
        {
            FilePath = "/tmp/a.wav",
            MimeType = "audio/wav",
            DurationMs = 5000,
            SampleRate = 44100,
        };
        generated.SampleRate.Should().Be(44100);
    }

    [Fact]
    public async Task InfrastructureAgent_executes_with_mock_output()
    {
        var agent = new InfrastructureAgent(
            Spec("Infrastructure"),
            NullLogger<InfrastructureAgent>.Instance);
        await agent.InitializeAsync();
        var result = await agent.ExecuteAsync();
        result.Should().NotBeNull();
    }
}
