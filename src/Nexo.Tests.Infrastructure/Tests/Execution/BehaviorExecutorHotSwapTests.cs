using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Workflows;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Models;
using Nexo.Core.Application.Common.Services;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class BehaviorExecutorHotSwapTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSwapsOnFailureAsync();
            await TestPrefersDeterministicViaRuntimeSpecAsync();
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

        await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, new Nexo.Core.Domain.Execution.BehaviorInput(), opts))
        {
            if (evt is StepStartedEvent s) impls.Add(s.Implementation);
            if (evt is StepErrorEvent) errors++;
            if (evt is StepCompletedEvent) completed = true;
        }

        AssertTrue(completed, "Step should complete via deterministic fallback");
        AssertTrue(errors >= 1, "Should emit a step error for the failing impl");
        AssertTrue(impls.Count >= 2, "Should attempt at least two implementations");
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
        await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, new Nexo.Core.Domain.Execution.BehaviorInput(), opts))
        {
            if (evt is StepStartedEvent s)
            {
                firstImpl ??= s.Implementation;
            }
        }

        AssertEqual(ImplementationType.Deterministic, firstImpl!.Value, "Runtime spec should force deterministic first");
    }

    private async Task TestHotSwappableModelFallsBackAsync()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var providerFactory = new ProviderFactory(loggerFactory.CreateLogger<ProviderFactory>());

        var providerBacked = new ProviderBackedModel(providerFactory, loggerFactory.CreateLogger<ProviderBackedModel>());
        var model = new HotSwappableModel(providerBacked, loggerFactory.CreateLogger<HotSwappableModel>());

        // Force a provider that is typically unavailable in CI (no API key), ensuring fallback.
        await WithEnv("NEXO_MODEL_PROVIDER", "openai", async () =>
        {
            await WithEnv("OPENAI_API_KEY", null, async () =>
            {
                var input = new ModelInput(new List<(string role, string content)>
                {
                    ("system", "nexo.model.provider=openai"),
                    ("user", "hello")
                });

                var outp = await model.CompleteAsync(input, CancellationToken.None);
                AssertEqual("hello", outp.Text, "Should fall back to deterministic echo behavior");
            });
        });
    }

    private static BehaviorExecutor CreateExecutor(
        Brick brick,
        bool providerAvailable,
        IAgenticBrickEngine? agenticBrickEngine = null,
        IMetricsCollector? metricsCollector = null)
    {
        var loggerFactory = LoggerFactory.Create(b => { });

        var registry = new SingleBrickRegistry(brick);
        var providerFactory = new StubProviderFactory(providerAvailable);
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

    private sealed class SingleBrickRegistry : Nexo.Core.Domain.Execution.IBrickRegistry
    {
        private readonly Brick _brick;
        public SingleBrickRegistry(Brick brick) => _brick = brick;
        public Brick? GetBrick(string id) => id == _brick.Id ? _brick : null;
        public IReadOnlyList<Brick> GetAllBricks() => new[] { _brick };
    }

    private sealed class StubProviderFactory : IProviderFactory
    {
        private readonly bool _available;
        public StubProviderFactory(bool available) => _available = available;
        public bool IsProviderAvailable(string provider) => _available;
        public Task<string> ExecuteLLMAsync(string provider, string systemPrompt, string userPrompt, object config, CancellationToken cancellationToken = default)
            => Task.FromResult("{}");
        public Task<string> ExecuteVisionAsync(string provider, string systemPrompt, string userPrompt, byte[] imageBytes, object config, CancellationToken cancellationToken = default)
            => Task.FromResult("{}");
        public Task<string> ExecuteVisionMultiFrameAsync(string provider, string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default)
            => Task.FromResult("{}");
        public Task<string> ExecuteVideoAsync(string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default)
            => Task.FromResult("{}");
        public Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FlakyAgenticBrick : Brick
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
            await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, old);
        }
    }
}

