using Ashlar.Agents.TestKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Behaviors;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Core.Domain.Execution.Events;
using Ashlar.Core.Domain.Workflows;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Core.Application.Execution.Ports;
using Ashlar.Infrastructure.Execution;
using Ashlar.Infrastructure.Execution.Models;
using Ashlar.Core.Application.Common.Services;

namespace Ashlar.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for behavior executor hot swap.</summary>
public sealed class BehaviorExecutorHotSwapTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test swaps on failure async.</summary>
            await TestSwapsOnFailureAsync();
            /// <summary>Test prefers deterministic via runtime spec async.</summary>
            await TestPrefersDeterministicViaRuntimeSpecAsync();
            /// <summary>Test hot swappable model falls back async.</summary>
            await TestHotSwappableModelFallsBackAsync();

            return new TestResult
            {
                Name = nameof(BehaviorExecutorHotSwapTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "BehaviorExecutor hot-swap tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(BehaviorExecutorHotSwapTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(BehaviorExecutorHotSwapTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSwapsOnFailureAsync()
    {
        var brick = new FlakyAgenticBrick();
        var exec = CreateExecutor(brick, providerAvailable: true);

        var behavior = new Behavior
        {
            Id = "b1",
            Name = "test",
            Description = "test",
            Steps = new[]
            {
                new BehaviorStep
                {
                    Id = "s1",
                    BrickId = brick.Id,
                    Implementation = ImplementationType.Auto,
                    InputMapping = new Dictionary<string, string>(),
                    OutputMapping = new Dictionary<string, string> { ["ok"] = "ok" }
                }
            },
            OnStepFailure = FailurePolicy.Abort
        };

        var agent = new AgentCard { Id = "a1", Name = "a", Description = "a", Behaviors = new[] { behavior.Id } };

        var opts = new ExecutionOptions
        {
            IsAirGapped = false,
            AuditMode = false,
            Provider = "offline",
            ImplementationMode = ImplementationMode.Auto,
            SwapOnFailure = true
        };

        var impls = new List<ImplementationType>();
        var errors = 0;
        var completed = false;

        await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, new Ashlar.Core.Domain.Execution.BehaviorInput(), opts))
        {
            if (evt is StepStartedEvent s) impls.Add(s.Implementation);
            if (evt is StepErrorEvent) errors++;
            if (evt is StepCompletedEvent) completed = true;
        }

        /// <summary>Assert true.</summary>
        /// <param name="fallback"">Fallback".</param>
        AssertTrue(completed, "Step should complete via deterministic fallback");
        /// <summary>Assert true.</summary>
        /// <param name="1">1.</param>
        /// <param name="impl"">Impl".</param>
        AssertTrue(errors >= 1, "Should emit a step error for the failing impl");
        /// <summary>Assert true.</summary>
        /// <param name="2">2.</param>
        /// <param name="implementations"">Implementations".</param>
        AssertTrue(impls.Count >= 2, "Should attempt at least two implementations");
        /// <summary>Assert equal.</summary>
        AssertEqual(ImplementationType.Agentic, impls[0]);
        AssertTrue(impls.Contains(ImplementationType.Deterministic), "Should fall back to deterministic");
    }

    private async Task TestPrefersDeterministicViaRuntimeSpecAsync()
    {
        var brick = new FlakyAgenticBrick();
        var exec = CreateExecutor(brick, providerAvailable: true);

        var behavior = new Behavior
        {
            Id = "b2",
            Name = "test2",
            Description = "test2",
            Steps = new[]
            {
                new BehaviorStep
                {
                    Id = "s1",
                    BrickId = brick.Id,
                    Implementation = ImplementationType.Auto,
                    InputMapping = new Dictionary<string, string>(),
                    OutputMapping = new Dictionary<string, string> { ["ok"] = "ok" }
                }
            }
        };

        var agent = new AgentCard { Id = "a1", Name = "a", Description = "a", Behaviors = new[] { behavior.Id } };

        var opts = new ExecutionOptions
        {
            IsAirGapped = false,
            AuditMode = false,
            Provider = "offline",
            ImplementationMode = ImplementationMode.Auto,
            BrickRuntime = new Dictionary<string, BrickRuntimeSpec>
            {
                [brick.Id] = BrickRuntimeSpec.DeterministicOnly()
            }
        };

        var firstImpl = (ImplementationType?)null;
        await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, new Ashlar.Core.Domain.Execution.BehaviorInput(), opts))
        {
            if (evt is StepStartedEvent s)
            {
                firstImpl ??= s.Implementation;
            }
        }

        /// <summary>Assert equal.</summary>
        /// <param name="first"">First".</param>
        AssertEqual(ImplementationType.Deterministic, firstImpl!.Value, "Runtime spec should force deterministic first");
    }

    private async Task TestHotSwappableModelFallsBackAsync()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var providerFactory = new ProviderFactory(loggerFactory.CreateLogger<ProviderFactory>());

        var providerBacked = new ProviderBackedModel(providerFactory, loggerFactory.CreateLogger<ProviderBackedModel>());
        var model = new HotSwappableModel(providerBacked, loggerFactory.CreateLogger<HotSwappableModel>());

        // Force a provider that is typically unavailable in CI (no API key), ensuring fallback.
        await WithEnv("ASHLAR_MODEL_PROVIDER", "openai", async () =>
        {
            /// <summary>With env.</summary>
            /// <param name="(">(.</param>
            await WithEnv("OPENAI_API_KEY", null, async () =>
            {
                var input = new ModelInput(new List<(string role, string content)>
                {
                    ("system", "ashlar.model.provider=openai"),
                    ("user", "hello")
                });

                var outp = await model.CompleteAsync(input, CancellationToken.None);
                /// <summary>Assert equal.</summary>
                /// <param name="behavior"">Behavior".</param>
                AssertEqual("hello", outp.Text, "Should fall back to deterministic echo behavior");
            });
        });
    }

    private static BehaviorExecutor CreateExecutor(
        DomainBrick brick,
        bool providerAvailable,
        IAgenticBrickEngine? agenticBrickEngine = null,
        IMetricsCollector? metricsCollector = null)
    {
        var loggerFactory = LoggerFactory.Create(b => { });

        var registry = new SingleBrickRegistry(brick);
        var providerFactory = new FakeProviderFactory("{}") { Available = providerAvailable, OllamaReachable = true };
        var cache = new SemanticCache(loggerFactory.CreateLogger<SemanticCache>());

        return new BehaviorExecutor(
            registry,
            providerFactory,
            cache,
            new SequentialLoopKernel(),
            loggerFactory.CreateLogger<BehaviorExecutor>(),
            agenticBrickEngine,
            null,
            metricsCollector);
    }

    /// <summary>Tests for single brick registry.</summary>
    private sealed class SingleBrickRegistry : Ashlar.Core.Domain.Execution.IBrickRegistry
    {
        private readonly DomainBrick _brick;
        /// <summary>Single brick registry.</summary>
        /// <param name="brick">Brick.</param>
        public SingleBrickRegistry(DomainBrick brick) => _brick = brick;
        /// <summary>Gets brick.</summary>
        /// <param name="id">Id.</param>
        public DomainBrick? GetBrick(string id) => id == _brick.Id ? _brick : null;
        /// <summary>Gets all bricks.</summary>
        public IReadOnlyList<DomainBrick> GetAllBricks() => new[] { _brick };
    }


    /// <summary>Tests for flaky agentic brick.</summary>
    private sealed class FlakyAgenticBrick : DomainBrick
    {
        public FlakyAgenticBrick()
        {
            Id = "test.flaky";
            Name = "Flaky";
            Description = "Throws in agentic, succeeds in deterministic";
            Category = BrickCategory.Analysis;
            Implementations = new BrickImplementations
            {
                Deterministic = new DeterministicImplementation { Id = "d", Name = "d", Description = "d", Executor = "x" },
                Agentic = new AgenticImplementation { Id = "a", Name = "a", Description = "a" }
            };
            DefaultImplementation = ImplementationType.Agentic;
            FallbackChain = new[] { ImplementationType.Agentic, ImplementationType.Deterministic };
        }

        public override Task<BrickOutput> ExecuteAsync(BrickInput input, ImplementationType implementation, IExecutionContext context, CancellationToken cancellationToken = default)
        {
            if (implementation == ImplementationType.Agentic)
            {
                /// <summary>Invalid operation exception.</summary>
                /// <param name="failed"">Failed".</param>
                throw new InvalidOperationException("agentic failed");
            }

            return Task.FromResult(new BrickOutput
            {
                ["ok"] = true,
                Summary = "ok"
            });
        }
    }

    private static async Task WithEnv(string key, string? value, Func<Task> action)
    {
        var old = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, value);
            /// <summary>Action.</summary>
            await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, old);
        }
    }
}

