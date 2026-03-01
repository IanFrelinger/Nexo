using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Adapters.GeoVector.Providers;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Workflows;
using Nexo.Core.Application.Common.Services;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Bricks;
using Nexo.Infrastructure.Execution;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class GeoVectorBehaviorTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestFallbackToDeterministicAsync(cancellationToken);
            await TestDeterministicOnlyAirgapAsync(cancellationToken);

            return new TestResult
            {
                Name = nameof(GeoVectorBehaviorTests),
                Category = "Execution",
                Passed = true,
                Message = "GeoVector behavior tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(GeoVectorBehaviorTests),
                Category = "Execution",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private static Behavior BuildBehavior()
        => new()
        {
            Id = "geovector.behavior.buildings.obj",
            Name = "GeoVector Buildings → OBJ",
            Steps =
            [
                new BehaviorStep
                {
                    Id = "fetch",
                    BrickId = "geovector.fetch.features",
                    Implementation = ImplementationType.Auto,
                    InputMapping = new Dictionary<string, string>
                    {
                        ["bounds"] = "bounds",
                        ["kind"] = "kind",
                        ["forceAgenticFail"] = "forceAgenticFail"
                    },
                    OutputMapping = new Dictionary<string, string>
                    {
                        ["features"] = "features"
                    }
                },
                new BehaviorStep
                {
                    Id = "mesh",
                    BrickId = "geovector.buildings.to-mesh",
                    Implementation = ImplementationType.Auto,
                    InputMapping = new Dictionary<string, string>
                    {
                        ["features"] = "features",
                        ["origin"] = "origin",
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
                    BrickId = "geovector.export.obj-text",
                    Implementation = ImplementationType.Deterministic,
                    InputMapping = new Dictionary<string, string>
                    {
                        ["mesh"] = "mesh"
                    },
                    OutputMapping = new Dictionary<string, string>
                    {
                        ["objText"] = "objText"
                    }
                }
            ]
        };

    private static (BrickRegistry Registry, BehaviorExecutor Executor) BuildExecutor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Use offline model provider via existing factory wiring in infrastructure tests
        services.AddSingleton<Nexo.Infrastructure.Execution.IProviderFactory, Nexo.Infrastructure.Execution.ProviderFactory>();
        services.AddSingleton<Nexo.Abstractions.IModel>(sp => new Nexo.Infrastructure.Execution.Models.ProviderBackedModel(
            sp.GetRequiredService<Nexo.Infrastructure.Execution.IProviderFactory>(),
            sp.GetRequiredService<ILogger<Nexo.Infrastructure.Execution.Models.ProviderBackedModel>>()));

        var sp = services.BuildServiceProvider();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        IVectorProvider vectorProvider = new EchoVectorProvider();
        var providerFactory = sp.GetRequiredService<Nexo.Infrastructure.Execution.IProviderFactory>();

        var bricks = new Brick[]
        {
            new GeoVectorFetchFeaturesBrick(vectorProvider, providerFactory, loggerFactory.CreateLogger<GeoVectorFetchFeaturesBrick>()),
            new GeoVectorBuildingsToMeshBrick(providerFactory, loggerFactory.CreateLogger<GeoVectorBuildingsToMeshBrick>()),
            new GeoVectorObjFromMeshBrick(loggerFactory.CreateLogger<GeoVectorObjFromMeshBrick>())
        };

        var reg = new BrickRegistry(bricks);
        var cache = new SemanticCache(loggerFactory.CreateLogger<SemanticCache>());
        var exec = new BehaviorExecutor(reg, providerFactory, cache, new SequentialLoopKernel(), loggerFactory.CreateLogger<BehaviorExecutor>());
        return (reg, exec);
    }

    private async Task TestFallbackToDeterministicAsync(CancellationToken ct)
    {
        var (_, exec) = BuildExecutor();
        var behavior = BuildBehavior();

        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MinLongitude = new Longitude(0),
            MaxLatitude = new Latitude(0.001),
            MaxLongitude = new Longitude(0.001)
        };

        var origin = new GeoPoint { Latitude = new Latitude(0.0005), Longitude = new Longitude(0.0005) };

        var agent = new AgentCard { Id = "geovector.test", Name = "GeoVectorTest", Behaviors = [behavior.Id] };
        var options = new ExecutionOptions { Provider = "offline", SwapOnFailure = true, ImplementationMode = ImplementationMode.Auto };

        var input = new BehaviorInput(new Dictionary<string, object>
        {
            ["bounds"] = bounds,
            ["origin"] = origin,
            ["kind"] = "building",
            ["forceAgenticFail"] = true
        });

        var sawObj = false;
        await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
        {
            if (evt is BehaviorCompletedEvent done)
            {
                AssertTrue(done.Outputs.ContainsKey("objText"), "Expected objText output");
                sawObj = true;
            }
        }

        AssertTrue(sawObj, "Expected behavior completion with obj output even when agentic fails");
    }

    private async Task TestDeterministicOnlyAirgapAsync(CancellationToken ct)
    {
        var (_, exec) = BuildExecutor();
        var behavior = BuildBehavior();

        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MinLongitude = new Longitude(0),
            MaxLatitude = new Latitude(0.001),
            MaxLongitude = new Longitude(0.001)
        };

        var origin = new GeoPoint { Latitude = new Latitude(0.0005), Longitude = new Longitude(0.0005) };

        var agent = new AgentCard { Id = "geovector.test", Name = "GeoVectorTest", Behaviors = [behavior.Id] };
        var options = new ExecutionOptions
        {
            Provider = "offline",
            SwapOnFailure = true,
            ImplementationMode = ImplementationMode.DeterministicOnly,
            IsAirGapped = true
        };

        var input = new BehaviorInput(new Dictionary<string, object>
        {
            ["bounds"] = bounds,
            ["origin"] = origin,
            ["kind"] = "building",
            ["forceAgenticFail"] = false
        });

        await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
        {
            if (evt is BehaviorCompletedEvent done)
            {
                AssertTrue(done.Success, "Expected deterministic-only behavior success");
                AssertTrue(done.Outputs.ContainsKey("objText"), "Expected objText output");
            }
        }
    }
}

