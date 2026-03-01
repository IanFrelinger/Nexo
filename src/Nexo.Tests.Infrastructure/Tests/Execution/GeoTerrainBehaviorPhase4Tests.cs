using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Infrastructure.Execution;
using Nexo.GeoTerrain.Bricks;
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Core.Domain.Workflows;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class GeoTerrainBehaviorPhase4Tests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSwapsOnFailureAsync();
            await TestAirGappedForcesDeterministicAsync();

            return new TestResult
            {
                Name = nameof(GeoTerrainBehaviorPhase4Tests),
                Category = "Infrastructure",
                Passed = true,
                Message = "GeoTerrain Phase 4 behavior tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(GeoTerrainBehaviorPhase4Tests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSwapsOnFailureAsync()
    {
        var (exec, behavior, agent) = CreateGeoTerrainBehaviorExecutor(providerAvailable: true);

        var opts = new ExecutionOptions
        {
            IsAirGapped = false,
            AuditMode = false,
            Provider = "offline",
            ImplementationMode = ImplementationMode.Auto,
            SwapOnFailure = true
        };

        var impls = new List<ImplementationType>();
        var hadError = false;
        var completed = false;

        var input = new BehaviorInput(new Dictionary<string, object>
        {
            ["tile"] = "N00E000.hgt",
            ["forceAgenticFail"] = true
        });

        await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, opts))
        {
            if (evt is StepStartedEvent s) impls.Add(s.Implementation);
            if (evt is StepErrorEvent) hadError = true;
            if (evt is BehaviorCompletedEvent) completed = true;
        }

        AssertTrue(completed, "Behavior should complete via deterministic fallback");
        AssertTrue(hadError, "Should emit an error for the failing agentic impl");
        AssertTrue(impls.Contains(ImplementationType.Agentic), "Should attempt agentic first");
        AssertTrue(impls.Contains(ImplementationType.Deterministic), "Should fall back to deterministic");
    }

    private async Task TestAirGappedForcesDeterministicAsync()
    {
        var (exec, behavior, agent) = CreateGeoTerrainBehaviorExecutor(providerAvailable: true);

        var opts = new ExecutionOptions
        {
            IsAirGapped = true,
            AuditMode = false,
            Provider = "offline",
            ImplementationMode = ImplementationMode.AgenticPreferred,
            SwapOnFailure = true
        };

        var firstImpl = (ImplementationType?)null;
        var input = new BehaviorInput(new Dictionary<string, object>
        {
            ["tile"] = "N00E000.hgt"
        });

        await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, opts))
        {
            if (evt is StepStartedEvent s)
            {
                firstImpl ??= s.Implementation;
                break;
            }
        }

        AssertEqual(ImplementationType.Deterministic, firstImpl!.Value, "Air-gapped must force deterministic");
    }

    private static (BehaviorExecutor exec, Behavior behavior, AgentCard agent) CreateGeoTerrainBehaviorExecutor(bool providerAvailable)
    {
        var loggerFactory = LoggerFactory.Create(_ => { });

        // Provider returns a tiny 2x2 tile (fits our parser).
        var elevationProvider = new EchoElevationProvider();

        var providerFactory = new StubProviderFactory(providerAvailable);

        var bricks = new Brick[]
        {
            new GeoTerrainFetchSrtmTileBrick(elevationProvider, providerFactory, loggerFactory.CreateLogger<GeoTerrainFetchSrtmTileBrick>()),
            new GeoTerrainMeshFromHgtBrick(providerFactory, loggerFactory.CreateLogger<GeoTerrainMeshFromHgtBrick>()),
            new GeoTerrainObjFromMeshBrick(loggerFactory.CreateLogger<GeoTerrainObjFromMeshBrick>())
        };

        var registry = new BrickRegistry(bricks);
        var cache = new SemanticCache(loggerFactory.CreateLogger<SemanticCache>());

        var exec = new BehaviorExecutor(
            registry,
            providerFactory,
            cache,
            new SequentialLoopKernel(),
            loggerFactory.CreateLogger<BehaviorExecutor>());

        var behavior = new Behavior
        {
            Id = "geoterrain.behavior.obj",
            Name = "GeoTerrain: tile -> OBJ text",
            Description = "Fetch an SRTM tile, mesh it, and export OBJ text.",
            Steps = new[]
            {
                new BehaviorStep
                {
                    Id = "fetch",
                    BrickId = "geoterrain.fetch.srtm-tile",
                    Implementation = ImplementationType.Auto,
                    InputMapping = new Dictionary<string, string>
                    {
                        ["tile"] = "tile",
                        ["forceAgenticFail"] = "forceAgenticFail"
                    },
                    OutputMapping = new Dictionary<string, string>
                    {
                        ["tileId"] = "tileId",
                        ["hgtBytes"] = "hgtBytes"
                    }
                },
                new BehaviorStep
                {
                    Id = "mesh",
                    BrickId = "geoterrain.mesh.from-hgt",
                    Implementation = ImplementationType.Auto,
                    InputMapping = new Dictionary<string, string>
                    {
                        ["tileId"] = "tileId",
                        ["hgtBytes"] = "hgtBytes",
                        ["forceAgenticFail"] = "forceAgenticFail"
                    },
                    OutputMapping = new Dictionary<string, string>
                    {
                        ["mesh"] = "mesh"
                    }
                },
                new BehaviorStep
                {
                    Id = "obj",
                    BrickId = "geoterrain.export.obj-text",
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string>
                    {
                        ["mesh"] = "mesh"
                    },
                    OutputMapping = new Dictionary<string, string>
                    {
                        ["objText"] = "obj"
                    }
                }
            },
            OnStepFailure = FailurePolicy.Abort
        };

        var agent = new AgentCard
        {
            Id = "geoterrain.agent",
            Name = "GeoTerrain Agent",
            Description = "Test agent",
            Behaviors = new[] { behavior.Id }
        };

        return (exec, behavior, agent);
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
}

