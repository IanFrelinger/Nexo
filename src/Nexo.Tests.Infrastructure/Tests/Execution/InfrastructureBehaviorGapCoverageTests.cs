using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for infrastructure behavior gap coverage.</summary>
public class InfrastructureBehaviorGapCoverageTests
{
    [Fact]
    public async Task BehaviorExecutor_executes_deterministic_step_and_maps_outputs()
    {
        var brick = new EchoBehaviorBrick("echo-step");
        var executor = CreateExecutor(brick);
        var behavior = new Behavior
        {
            Id = "behavior-1",
            Name = "Echo behavior",
            Description = "test",
            Steps =
            [
                new BehaviorStep
                {
                    Id = "step-1",
                    BrickId = brick.Id,
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string>(),
                    OutputMapping = new Dictionary<string, string> { ["result"] = "result" },
                },
            ],
            OnStepFailure = FailurePolicy.Abort,
        };

        var result = await executor.ExecuteAsync(
            new AgentCard { Id = "agent-1", Name = "Agent", Description = "desc", Behaviors = [behavior.Id] },
            behavior,
            new BehaviorInput { Parameters = new Dictionary<string, object>() },
            new ExecutionOptions { Provider = "mock", IsAirGapped = false, AuditMode = false });

        result.Success.Should().BeTrue();
        result.Outputs["result"].Should().Be("done");
    }

    [Fact]
    public async Task BehaviorExecutor_skips_steps_when_condition_false_and_reports_missing_brick()
    {
        var brick = new EchoBehaviorBrick("present");
        var executor = CreateExecutor(brick);
        var behavior = new Behavior
        {
            Id = "behavior-2",
            Name = "Conditional",
            Description = "test",
            Steps =
            [
                new BehaviorStep
                {
                    Id = "skip-me",
                    BrickId = brick.Id,
                    Condition = "never",
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string>(),
                    OutputMapping = new Dictionary<string, string>(),
                },
                new BehaviorStep
                {
                    Id = "missing",
                    BrickId = "absent",
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string>(),
                    OutputMapping = new Dictionary<string, string>(),
                },
            ],
            OnStepFailure = FailurePolicy.Continue,
        };

        var events = new List<ExecutionEvent>();
        await foreach (var evt in executor.ExecuteWithEventsAsync(
                           new AgentCard { Id = "agent-2", Name = "Agent", Description = "desc", Behaviors = [behavior.Id] },
                           behavior,
                           new BehaviorInput(),
                           new ExecutionOptions { Provider = "mock" }))
        {
            events.Add(evt);
        }

        events.Should().Contain(e => e is StepSkippedEvent);
        events.Should().Contain(e => e is StepErrorEvent);
    }

    [Fact]
    public async Task BehaviorExecutor_air_gapped_agentic_only_reports_no_available_implementation()
    {
        var brick = new AgenticOnlyBehaviorBrick("agentic-only");
        var executor = CreateExecutor(brick);
        var behavior = new Behavior
        {
            Id = "airgap-behavior",
            Name = "Air gap",
            Description = "test",
            Steps =
            [
                new BehaviorStep
                {
                    Id = "step",
                    BrickId = brick.Id,
                    Implementation = ImplementationType.Agentic,
                    InputMapping = new Dictionary<string, string>(),
                    OutputMapping = new Dictionary<string, string>(),
                },
            ],
            OnStepFailure = FailurePolicy.Continue,
        };

        var events = new List<ExecutionEvent>();
        await foreach (var evt in executor.ExecuteWithEventsAsync(
                           new AgentCard { Id = "agent-air", Name = "Agent", Description = "desc", Behaviors = [behavior.Id] },
                           behavior,
                           new BehaviorInput(),
                           new ExecutionOptions { Provider = "mock", IsAirGapped = true }))
        {
            events.Add(evt);
        }

        events.OfType<StepErrorEvent>().Should().Contain(e => e.Error.Contains("No available implementation"));
    }

    [Fact]
    public async Task BehaviorExecutor_second_run_emits_cache_hit()
    {
        var brick = new EchoBehaviorBrick("cached-echo");
        var executor = CreateExecutor(brick);
        var behavior = new Behavior
        {
            Id = "cache-behavior",
            Name = "Cache",
            Description = "test",
            Steps =
            [
                new BehaviorStep
                {
                    Id = "step",
                    BrickId = brick.Id,
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string>(),
                    OutputMapping = new Dictionary<string, string> { ["result"] = "result" },
                },
            ],
        };

        var agent = new AgentCard { Id = "agent-cache", Name = "Agent", Description = "desc", Behaviors = [behavior.Id] };
        var input = new BehaviorInput { Parameters = new Dictionary<string, object>() };
        var options = new ExecutionOptions { Provider = "mock", IsAirGapped = false, AuditMode = false };

        await executor.ExecuteAsync(agent, behavior, input, options);
        var events = new List<ExecutionEvent>();
        await foreach (var evt in executor.ExecuteWithEventsAsync(agent, behavior, input, options))
            events.Add(evt);

        events.Should().Contain(e => e is CacheHitEvent);
    }

    [Fact]
    public async Task BehaviorExecutor_swap_on_failure_disabled_stops_after_first_error()
    {
        var brick = new FlakyDeterministicBehaviorBrick("flaky-det");
        var executor = CreateExecutor(brick);

        var behavior = new Behavior
        {
            Id = "no-swap",
            Name = "No swap",
            Description = "test",
            Steps =
            [
                new BehaviorStep
                {
                    Id = "step",
                    BrickId = brick.Id,
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string>(),
                    OutputMapping = new Dictionary<string, string>(),
                },
            ],
            OnStepFailure = FailurePolicy.Abort,
        };

        var events = new List<ExecutionEvent>();
        await foreach (var evt in executor.ExecuteWithEventsAsync(
                           new AgentCard { Id = "agent", Name = "Agent", Description = "desc", Behaviors = [behavior.Id] },
                           behavior,
                           new BehaviorInput(),
                           new ExecutionOptions { Provider = "mock", SwapOnFailure = false }))
        {
            events.Add(evt);
        }

        events.OfType<StepErrorEvent>().Should().NotBeEmpty();
        events.OfType<StepCompletedEvent>().Should().BeEmpty();
    }

    private static BehaviorExecutor CreateExecutor(DomainBrick brick)
    {
        var registry = new SingleBrickRegistry(brick);
        var cache = new SemanticCache(NullLogger<SemanticCache>.Instance);
        return new BehaviorExecutor(
            registry,
            new StubProviderFactory(),
            cache,
            new SequentialLoopKernel(),
            NullLogger<BehaviorExecutor>.Instance);
    }

    /// <summary>Tests for single brick registry.</summary>
    private sealed class SingleBrickRegistry(DomainBrick brick) : IBrickRegistry
    {
        /// <summary>Gets brick.</summary>
        /// <param name="id">Id.</param>
        public DomainBrick? GetBrick(string id) => id == brick.Id ? brick : null;
        /// <summary>Gets all bricks.</summary>
        public IReadOnlyList<DomainBrick> GetAllBricks() => [brick];
    }

    /// <summary>Tests for stub provider factory.</summary>
    private sealed class StubProviderFactory : IProviderFactory
    {
        /// <summary>Returns whether  provider available.</summary>
        /// <param name="provider">Provider.</param>
        public bool IsProviderAvailable(string provider) => true;
        /// <summary>Execute llm async.</summary>
        /// <param name="provider">Provider.</param>
        /// <param name="systemPrompt">System prompt.</param>
        /// <param name="userPrompt">User prompt.</param>
        /// <param name="config">Config.</param>
        /// <param name="default">Default.</param>
        public Task<string> ExecuteLLMAsync(string provider, string systemPrompt, string userPrompt, object config, CancellationToken cancellationToken = default) => Task.FromResult("{}");
        /// <summary>Execute vision async.</summary>
        /// <param name="provider">Provider.</param>
        /// <param name="systemPrompt">System prompt.</param>
        /// <param name="userPrompt">User prompt.</param>
        /// <param name="imageBytes">Image bytes.</param>
        /// <param name="config">Config.</param>
        /// <param name="default">Default.</param>
        public Task<string> ExecuteVisionAsync(string provider, string systemPrompt, string userPrompt, byte[] imageBytes, object config, CancellationToken cancellationToken = default) => Task.FromResult("{}");
        /// <summary>Execute vision multi frame async.</summary>
        /// <param name="provider">Provider.</param>
        /// <param name="systemPrompt">System prompt.</param>
        /// <param name="userPrompt">User prompt.</param>
        /// <param name="frameBytes">Frame bytes.</param>
        /// <param name="config">Config.</param>
        /// <param name="default">Default.</param>
        public Task<string> ExecuteVisionMultiFrameAsync(string provider, string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default) => Task.FromResult("{}");
        /// <summary>Execute video async.</summary>
        /// <param name="systemPrompt">System prompt.</param>
        /// <param name="userPrompt">User prompt.</param>
        /// <param name="frameBytes">Frame bytes.</param>
        /// <param name="config">Config.</param>
        /// <param name="default">Default.</param>
        public Task<string> ExecuteVideoAsync(string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default) => Task.FromResult("{}");
        /// <summary>Ensure ollama reachable async.</summary>
        /// <param name="requireVisionModel">Require vision model.</param>
        /// <param name="default">Default.</param>
        public Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>Tests for flaky deterministic behavior brick.</summary>
    private sealed class FlakyDeterministicBehaviorBrick : DomainBrick
    {
        public FlakyDeterministicBehaviorBrick(string id)
        {
            Id = id;
            Name = id;
            Description = "flaky";
            Category = BrickCategory.Control;
            Implementations = new BrickImplementations
            {
                Deterministic = new DeterministicImplementation
                {
                    Id = "det",
                    Name = "Deterministic",
                    Description = "det",
                    Executor = "local",
                },
            };
        }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default) =>
            /// <summary>Invalid operation exception.</summary>
            /// <param name="fails"">Fails".</param>
            throw new InvalidOperationException("always fails");
    }

    /// <summary>Tests for agentic only behavior brick.</summary>
    private sealed class AgenticOnlyBehaviorBrick : DomainBrick
    {
        public AgenticOnlyBehaviorBrick(string id)
        {
            Id = id;
            Name = id;
            Description = "agentic only";
            Category = BrickCategory.Control;
            Implementations = new BrickImplementations
            {
                Agentic = new AgenticImplementation
                {
                    Id = "agentic",
                    Name = "Agentic",
                    Description = "agentic",
                    Executor = "LLMChainExecutor",
                    LLMConfig = new LLMConfig { SystemPrompt = "test" },
                },
            };
        }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            var output = new BrickOutput();
            output.Set("result", "agentic");
            return Task.FromResult(output);
        }
    }

    /// <summary>Tests for echo behavior brick.</summary>
    private sealed class EchoBehaviorBrick : DomainBrick
    {
        public EchoBehaviorBrick(string id)
        {
            Id = id;
            Name = id;
            Description = "echo";
            Category = BrickCategory.Control;
            Implementations = new BrickImplementations
            {
                Deterministic = new DeterministicImplementation
                {
                    Id = "det",
                    Name = "Deterministic",
                    Description = "det",
                    Executor = "local",
                },
            };
        }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            var output = new BrickOutput { Summary = "ok" };
            output.Set("result", "done");
            return Task.FromResult(output);
        }
    }
}
