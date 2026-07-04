using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Agents.CodeGeneration;
using Nexo.Orchestration.Agents.Playtest;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Communication;
using Nexo.Orchestration.Communication.Models;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Coordination.ErrorRecovery;
using Nexo.Orchestration.Playtest.Models;
using Nexo.Orchestration.Playtest.Ports;
using Xunit;

namespace Nexo.Tests.Orchestration;

/// <summary>Tests for orchestration playtest coordination.</summary>
public class OrchestrationPlaytestCoordinationTests
{
    /// <summary>Spec.</summary>
    /// <param name=""Playtest"">"playtest".</param>
    private static AgentSpawnSpec Spec(string domain = "Playtest") => new()
    {
        AgentId = "agent-1",
        Domain = domain,
        Goal = "Test",
    };

    private static ILogger<BaseAgent> Logger => NullLogger<BaseAgent>.Instance;

    [Fact]
    public async Task BalanceAnalyzerAgent_analyzes_telemetry_and_enriches_issues()
    {
        var sessionId = "sess-1";
        var events = BuildBalanceTelemetryEvents(sessionId);

        var store = new Mock<ITelemetryStore>();
        store.Setup(s => s.GetEventsAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("""
                [{"description":"Reduce weapon damage","confidence":0.85,"parameters":{}}]
                """));

        var session = new PlaytestSession
        {
            SessionId = sessionId,
            BuildId = "build-1",
            Configuration = new PlaytestConfiguration(),
            StartedAt = DateTimeOffset.UtcNow,
        };

        var agent = new BalanceAnalyzerAgent(Spec(), store.Object, model.Object, Logger);
        await agent.InitializeAsync();
        var report = (PlaytestReport)await agent.ExecuteAsync(new Dictionary<string, object> { ["session"] = session });

        report.Issues.Should().NotBeEmpty();
        report.Summary.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task FeedbackSynthesizerAgent_generates_change_requests_from_report()
    {
        var issue = new BalanceIssue
        {
            IssueId = "issue-1",
            Category = BalanceCategory.Combat,
            Severity = BalanceSeverity.Major,
            Title = "TTK too low",
            Description = "Weapon kills too fast",
            AffectedDomains = new[] { "Combat" },
            SuggestedFixes = new[] { new SuggestedFix { Description = "Increase health", Confidence = 0.8 } },
        };
        var report = new PlaytestReport
        {
            ReportId = "rpt-1",
            GeneratedAt = DateTimeOffset.UtcNow,
            Issues = new[] { issue },
        };

        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("""
                {"domain":"Combat","changeType":"modify","targetElement":"health","proposedValue":"200","rationale":"longer fights"}
                """));

        var agent = new FeedbackSynthesizerAgent(Spec("Feedback"), model.Object, Logger);
        await agent.InitializeAsync();
        var synthesis = (FeedbackSynthesis)await agent.ExecuteAsync(new Dictionary<string, object>
        {
            ["report"] = report,
            ["Combat"] = new { health = 100 },
        });

        synthesis.ChangeRequests.Should().NotBeEmpty();
        synthesis.IterationRecommendation!.ShouldIterate.Should().BeTrue();
    }

    [Fact]
    public async Task AIPlayerAgent_executes_one_action_then_stops_on_game_over()
    {
        var callCount = 0;
        var runner = new Mock<IGameRunner>();
        runner.Setup(r => r.GetGameStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new GameState
                {
                    IsGameOver = callCount > 1,
                    PlayerHealth = 80,
                    PlayerMaxHealth = 100,
                    PlayerPosition = "spawn",
                    AvailableActions = new[] { "move", "attack" },
                    NearbyEnemies = new[] { "goblin" },
                    NearbyItems = new[] { "potion" },
                };
            });

        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("""
                {"action":"move","parameters":{"dir":"north"},"reasoning":"explore"}
                """));

        var agent = new AIPlayerAgent(Spec("AIPlayer"), runner.Object, model.Object, "aggressive", Logger);
        await agent.InitializeAsync();
        var session = (AIPlayerSession)await agent.ExecuteAsync();

        session.Profile.Should().Be("aggressive");
        session.Actions.Should().ContainSingle();
        runner.Verify(r => r.ExecuteActionAsync("move", It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CodeOptimizer_applies_security_performance_and_general_fixes()
    {
        var code = """
            password = "secret";
            Thread.Sleep(1000);


            """;
        var analysis = new CodeAnalysis
        {
            Language = "csharp",
            LineCount = 2,
            Complexity = 15,
            Issues = new[]
            {
                new CodeIssue { Category = "Security", Severity = IssueSeverity.High, Message = "Hardcoded password detected" },
                new CodeIssue { Category = "Performance", Severity = IssueSeverity.Medium, Message = "Blocking sleep detected" },
            },
        };

        var optimized = await new CodeOptimizer(NullLogger<CodeOptimizer>.Instance)
            .OptimizeAsync(code, "csharp", analysis);

        optimized.Should().Contain("configuration.GetValue");
        optimized.Should().Contain("await Task.Delay");
    }

    [Fact]
    public async Task ErrorRecoveryManager_retries_recoverable_errors()
    {
        var spec = Spec("Recovery");
        var inner = new GenericAgent(spec, NullLogger<GenericAgent>.Instance);
        await inner.InitializeAsync();
        var container = new AgentContainer(inner, NullLogger<AgentContainer>.Instance);

        var mgr = new ErrorRecoveryManager(NullLogger<ErrorRecoveryManager>.Instance, maxRetries: 2, retryDelay: TimeSpan.FromMilliseconds(1));
        var result = await mgr.RecoverAsync(container, new TimeoutException("network connection lost"));

        result.ShouldRetry.Should().BeTrue();
        result.Success.Should().BeTrue();
        mgr.GetRetryInfo(spec.AgentId)!.RetryCount.Should().Be(1);

        mgr.ResetRetryInfo(spec.AgentId);
        mgr.GetRetryInfo(spec.AgentId).Should().BeNull();
    }

    [Fact]
    public async Task ErrorRecoveryManager_rejects_non_recoverable_errors()
    {
        var spec = new AgentSpawnSpec { AgentId = "a2", Domain = "X", Goal = "g" };
        var inner = new GenericAgent(spec, NullLogger<GenericAgent>.Instance);
        await inner.InitializeAsync();
        var container = new AgentContainer(inner, NullLogger<AgentContainer>.Instance);
        var mgr = new ErrorRecoveryManager(NullLogger<ErrorRecoveryManager>.Instance, maxRetries: 3, retryDelay: TimeSpan.FromMilliseconds(1));

        var result = await mgr.RecoverAsync(container, new InvalidOperationException("bad state"));
        result.ShouldRetry.Should().BeFalse();
    }

    [Fact]
    public void MessageSchemaValidator_validates_registered_and_unregistered_messages()
    {
        var validator = new MessageSchemaValidator(NullLogger<MessageSchemaValidator>.Instance);
        validator.Validate(null!).Should().BeFalse();

        var unregistered = new OutputEmitted
        {
            MessageId = "m1",
            FromAgentId = "a1",
            MessageType = "OutputEmitted",
            Output = "data",
            OutputSchema = "schema",
        };
        validator.Validate(unregistered).Should().BeTrue();

        validator.RegisterSchema("OutputEmitted", new MessageSchemaValidator.JsonSchema { Schema = "{}" });
        var withPayload = unregistered with
        {
            MessageType = "OutputEmitted",
            Payload = JsonDocument.Parse("""{"ok":true}""").RootElement,
        };
        validator.Validate(withPayload).Should().BeTrue();
    }

    [Fact]
    public void TimeoutManager_creates_and_cancels_tokens()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "tm-1",
            Domain = "X",
            Goal = "g",
            ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 30 },
        };
        var inner = new GenericAgent(spec, NullLogger<GenericAgent>.Instance);
        var container = new AgentContainer(inner, NullLogger<AgentContainer>.Instance);
        var mgr = new TimeoutManager(NullLogger<TimeoutManager>.Instance, TimeSpan.FromSeconds(5));

        var token = mgr.CreateTimeoutToken(container);
        token.CanBeCanceled.Should().BeTrue();
        mgr.CancelTimeout(spec.AgentId);
        mgr.CancelTimeout(spec.AgentId);
    }

    [Fact]
    public void Playtest_port_models_round_trip()
    {
        var state = new GameState
        {
            IsGameOver = false,
            PlayerHealth = 50,
            PlayerMaxHealth = 100,
            PlayerPosition = "x:1,y:2",
            CurrentObjective = "tutorial",
            Currency = 10,
            EquippedWeapon = "sword",
            NearbyEnemies = new[] { "e1" },
            NearbyItems = new[] { "i1" },
            AvailableActions = new[] { "move" },
        };
        state.Currency.Should().Be(10);

        var evt = new TelemetryEvent
        {
            EventId = "e1",
            SessionId = "s1",
            EventType = TelemetryEventTypes.PlayerDamaged,
            Timestamp = DateTimeOffset.UtcNow,
            PlayerId = "p1",
            Data = new Dictionary<string, object> { ["weapon"] = "rifle" },
        };
        evt.Data.Should().ContainKey("weapon");
    }

    private static List<TelemetryEvent> BuildBalanceTelemetryEvents(string sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        var events = new List<TelemetryEvent>
        {
            new()
            {
                EventId = "k1",
                SessionId = sessionId,
                EventType = TelemetryEventTypes.PlayerKilled,
                Timestamp = now,
            },
            new()
            {
                EventId = "d1",
                SessionId = sessionId,
                EventType = TelemetryEventTypes.PlayerDamaged,
                Timestamp = now,
                Data = new Dictionary<string, object> { ["weapon"] = "rifle", ["ttk"] = 0.1 },
            },
            new()
            {
                EventId = "e1",
                SessionId = sessionId,
                EventType = TelemetryEventTypes.CurrencyEarned,
                Timestamp = now,
                Data = new Dictionary<string, object> { ["amount"] = 100.0 },
            },
            new()
            {
                EventId = "s1",
                SessionId = sessionId,
                EventType = TelemetryEventTypes.CurrencySpent,
                Timestamp = now,
                Data = new Dictionary<string, object> { ["amount"] = 10.0 },
            },
            new()
            {
                EventId = "f1",
                SessionId = sessionId,
                EventType = TelemetryEventTypes.FrameTime,
                Timestamp = now,
                Data = new Dictionary<string, object> { ["ms"] = 50.0 },
            },
        };

        for (var i = 0; i < 6; i++)
        {
            events.Add(new TelemetryEvent
            {
                EventId = $"o-start-{i}",
                SessionId = sessionId,
                EventType = TelemetryEventTypes.ObjectiveStarted,
                Timestamp = now,
            });
        }
        events.Add(new TelemetryEvent
        {
            EventId = "o-done",
            SessionId = sessionId,
            EventType = TelemetryEventTypes.ObjectiveCompleted,
            Timestamp = now,
        });

        return events;
    }
}
