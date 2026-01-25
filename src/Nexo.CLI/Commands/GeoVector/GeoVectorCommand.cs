using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Adapters.GeoVector.Providers;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Workflows;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Bricks;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.IO;
using Nexo.Orchestration.GeoTerrain.Ports;
using Nexo.Orchestration.GeoVector.Ports;
using Nexo.Adapters.GeoVector.Utilities;

namespace Nexo.CLI.Commands.GeoVector;

public class GeoVectorCommand : IGeoVectorCommand
{
    private readonly Nexo.Infrastructure.Execution.IProviderFactory _providerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<GeoVectorCommand> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoopKernel _loopKernel;

    public GeoVectorCommand(
        Nexo.Infrastructure.Execution.IProviderFactory providerFactory,
        ILoggerFactory loggerFactory,
        ILogger<GeoVectorCommand> logger,
        IHttpClientFactory httpClientFactory,
        ILoopKernel loopKernel)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loopKernel = loopKernel ?? throw new ArgumentNullException(nameof(loopKernel));
    }

    public async Task<int> BuildingsToObjAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        string? osmPbfPath,
        bool generateTexCoords,
        float uvMetersPerRepeat,
        bool alignToTerrain,
        string terrainProvider,
        string? terrainLocalRoot,
        string? terrainSrtmBaseUrl,
        bool terrainPersistDownloads,
        bool terrainEnableCache,
        bool terrainTreatNoDataAsZero,
        bool airGapped,
        bool forceAgenticFail,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (output is null) throw new ArgumentNullException(nameof(output));
            var geoBounds = GeoBounds.Parse(bounds);
            geoBounds.Validate();

            if (airGapped && string.Equals(provider?.Trim(), "mapbox", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Air-gapped mode cannot use the Mapbox provider. Use --vector-provider echo|osm|hybrid with an offline source.");
            }

            var origin = geoBounds.Center;

            var vectorProvider = BuildVectorProvider(provider ?? "echo", geoBounds, mapboxAccessToken, mapboxTileset, mapboxZoom, osmPbfPath, airGapped);

            ElevationGrid? terrainGrid = null;
            if (alignToTerrain)
            {
                var elevationProvider = BuildElevationProvider(
                    terrainProvider,
                    terrainLocalRoot,
                    terrainSrtmBaseUrl,
                    terrainPersistDownloads,
                    terrainEnableCache,
                    airGapped);

                terrainGrid = await BuildTerrainGridAsync(geoBounds, elevationProvider, ct);
            }

            var bricks = new Brick[]
            {
                new GeoVectorFetchFeaturesBrick(vectorProvider, _providerFactory, _loggerFactory.CreateLogger<GeoVectorFetchFeaturesBrick>()),
                new GeoVectorBuildingsToMeshBrick(_providerFactory, _loggerFactory.CreateLogger<GeoVectorBuildingsToMeshBrick>()),
                new GeoVectorObjFromMeshBrick(_loggerFactory.CreateLogger<GeoVectorObjFromMeshBrick>())
            };

            var brickRegistry = new BrickRegistry(bricks);
            var semanticCache = new SemanticCache(_loggerFactory.CreateLogger<SemanticCache>());
            var exec = new BehaviorExecutor(
                brickRegistry,
                _providerFactory,
                semanticCache,
                _loopKernel,
                _loggerFactory.CreateLogger<BehaviorExecutor>());

            var behavior = new Behavior
            {
                Id = "geovector.cli.buildings-to-obj",
                Name = "GeoVector: buildings -> OBJ",
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
                            ["generateTexCoords"] = "generateTexCoords",
                            ["uvMetersPerRepeat"] = "uvMetersPerRepeat",
                            ["alignToTerrain"] = "alignToTerrain",
                            ["terrainGrid"] = "terrainGrid",
                            ["terrainTreatNoDataAsZero"] = "terrainTreatNoDataAsZero",
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

            var agent = new AgentCard { Id = "geovector.cli", Name = "GeoVector CLI", Behaviors = [behavior.Id] };

            var options = new ExecutionOptions
            {
                Provider = "offline",
                IsAirGapped = airGapped,
                ImplementationMode = airGapped ? ImplementationMode.DeterministicOnly : ImplementationMode.Auto,
                SwapOnFailure = true
            };

            var inputData = new Dictionary<string, object>
            {
                ["bounds"] = geoBounds,
                ["origin"] = origin,
                ["kind"] = "building",
                ["generateTexCoords"] = generateTexCoords,
                ["uvMetersPerRepeat"] = uvMetersPerRepeat,
                ["alignToTerrain"] = alignToTerrain,
                ["terrainTreatNoDataAsZero"] = terrainTreatNoDataAsZero,
                ["forceAgenticFail"] = forceAgenticFail
            };
            if (alignToTerrain)
            {
                inputData["terrainGrid"] = terrainGrid ?? throw new InvalidOperationException("Terrain grid was not built.");
            }

            var input = new BehaviorInput(inputData);

            IReadOnlyDictionary<string, object> outputs = new Dictionary<string, object>();
            var steps = new List<object>();
            var errors = new List<string>();

            await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
            {
                switch (evt)
                {
                    case StepStartedEvent s:
                        steps.Add(new { type = "step_started", s.StepId, s.BrickId, s.Implementation, s.UsedFallback });
                        if (verbose && !json) Console.Out.WriteLine($"step:start id={s.StepId} brick={s.BrickId} impl={s.Implementation} fallback={s.UsedFallback}");
                        break;
                    case StepCompletedEvent c:
                        steps.Add(new { type = "step_completed", c.StepId, c.BrickId, c.Implementation, latencyMs = c.LatencyMs, c.Summary });
                        break;
                    case StepErrorEvent e:
                        errors.Add($"{e.StepId}: {e.Error}");
                        steps.Add(new { type = "step_error", e.StepId, e.Error, latencyMs = e.LatencyMs });
                        break;
                    case BehaviorCompletedEvent b:
                        outputs = b.Outputs;
                        steps.Add(new { type = "behavior_completed", b.Success });
                        break;
                }
            }

            var objText = outputs.TryGetValue("objText", out var o) ? o as string : null;
            if (string.IsNullOrWhiteSpace(objText))
                throw new InvalidOperationException("OBJ output was not produced.");

            DirectoryOps.EnsureParentDirectoryExists(output.FullName);
            await TextFile.WriteAllTextAsync(output.FullName, objText, ct);

            var result = new
            {
                ok = errors.Count == 0,
                bounds = geoBounds.ToString(),
                output = output.FullName,
                provider,
                alignToTerrain,
                airGapped,
                errors,
                steps
            };

            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(result));
            }
            else
            {
                Console.Out.WriteLine($"Wrote OBJ: {output.FullName}");
            }

            return errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoVector buildings-to-obj failed");
            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            }
            else
            {
                Console.Error.WriteLine(ex.Message);
            }
            return 1;
        }
    }

    public async Task<int> RoadsToObjAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        string? osmPbfPath,
        float widthMeters,
        bool generateTexCoords,
        float uvMetersPerRepeat,
        bool conformToTerrain,
        string terrainProvider,
        string? terrainLocalRoot,
        string? terrainSrtmBaseUrl,
        bool terrainPersistDownloads,
        bool terrainEnableCache,
        bool terrainTreatNoDataAsZero,
        bool airGapped,
        bool forceAgenticFail,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (output is null) throw new ArgumentNullException(nameof(output));
            var geoBounds = GeoBounds.Parse(bounds);
            geoBounds.Validate();

            if (airGapped && string.Equals(provider?.Trim(), "mapbox", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Air-gapped mode cannot use the Mapbox provider. Use --vector-provider echo|osm|hybrid with an offline source.");
            }

            var origin = geoBounds.Center;

            var vectorProvider = BuildVectorProvider(provider ?? "echo", geoBounds, mapboxAccessToken, mapboxTileset, mapboxZoom, osmPbfPath, airGapped);

            ElevationGrid? terrainGrid = null;
            if (conformToTerrain)
            {
                var elevationProvider = BuildElevationProvider(
                    terrainProvider,
                    terrainLocalRoot,
                    terrainSrtmBaseUrl,
                    terrainPersistDownloads,
                    terrainEnableCache,
                    airGapped);

                terrainGrid = await BuildTerrainGridAsync(geoBounds, elevationProvider, ct);
            }

            var bricks = new Brick[]
            {
                new GeoVectorFetchFeaturesBrick(vectorProvider, _providerFactory, _loggerFactory.CreateLogger<GeoVectorFetchFeaturesBrick>()),
                new GeoVectorRoadsToMeshBrick(_providerFactory, _loggerFactory.CreateLogger<GeoVectorRoadsToMeshBrick>()),
                new GeoVectorObjFromMeshBrick(_loggerFactory.CreateLogger<GeoVectorObjFromMeshBrick>())
            };

            var brickRegistry = new BrickRegistry(bricks);
            var semanticCache = new SemanticCache(_loggerFactory.CreateLogger<SemanticCache>());
            var exec = new BehaviorExecutor(
                brickRegistry,
                _providerFactory,
                semanticCache,
                _loopKernel,
                _loggerFactory.CreateLogger<BehaviorExecutor>());

            var behavior = new Behavior
            {
                Id = "geovector.cli.roads-to-obj",
                Name = "GeoVector: roads -> OBJ",
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
                        BrickId = "geovector.roads.to-mesh",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["features"] = "features",
                            ["origin"] = "origin",
                            ["widthMeters"] = "widthMeters",
                            ["generateTexCoords"] = "generateTexCoords",
                            ["uvMetersPerRepeat"] = "uvMetersPerRepeat",
                            ["conformToTerrain"] = "conformToTerrain",
                            ["terrainGrid"] = "terrainGrid",
                            ["terrainTreatNoDataAsZero"] = "terrainTreatNoDataAsZero",
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

            var agent = new AgentCard { Id = "geovector.cli", Name = "GeoVector CLI", Behaviors = [behavior.Id] };

            var options = new ExecutionOptions
            {
                Provider = "offline",
                IsAirGapped = airGapped,
                ImplementationMode = airGapped ? ImplementationMode.DeterministicOnly : ImplementationMode.Auto,
                SwapOnFailure = true
            };

            var inputData = new Dictionary<string, object>
            {
                ["bounds"] = geoBounds,
                ["origin"] = origin,
                ["kind"] = "road",
                ["widthMeters"] = widthMeters,
                ["generateTexCoords"] = generateTexCoords,
                ["uvMetersPerRepeat"] = uvMetersPerRepeat,
                ["conformToTerrain"] = conformToTerrain,
                ["terrainTreatNoDataAsZero"] = terrainTreatNoDataAsZero,
                ["forceAgenticFail"] = forceAgenticFail
            };
            if (conformToTerrain)
            {
                inputData["terrainGrid"] = terrainGrid ?? throw new InvalidOperationException("Terrain grid was not built.");
            }

            var input = new BehaviorInput(inputData);

            IReadOnlyDictionary<string, object> outputs = new Dictionary<string, object>();
            var steps = new List<object>();
            var errors = new List<string>();

            await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
            {
                switch (evt)
                {
                    case StepStartedEvent s:
                        steps.Add(new { type = "step_started", s.StepId, s.BrickId, s.Implementation, s.UsedFallback });
                        if (verbose && !json) Console.Out.WriteLine($"step:start id={s.StepId} brick={s.BrickId} impl={s.Implementation} fallback={s.UsedFallback}");
                        break;
                    case StepCompletedEvent c:
                        steps.Add(new { type = "step_completed", c.StepId, c.BrickId, c.Implementation, latencyMs = c.LatencyMs, c.Summary });
                        break;
                    case StepErrorEvent e:
                        errors.Add($"{e.StepId}: {e.Error}");
                        steps.Add(new { type = "step_error", e.StepId, e.Error, latencyMs = e.LatencyMs });
                        break;
                    case BehaviorCompletedEvent b:
                        outputs = b.Outputs;
                        steps.Add(new { type = "behavior_completed", b.Success });
                        break;
                }
            }

            var objText = outputs.TryGetValue("objText", out var o) ? o as string : null;
            if (string.IsNullOrWhiteSpace(objText))
                throw new InvalidOperationException("OBJ output was not produced.");

            DirectoryOps.EnsureParentDirectoryExists(output.FullName);
            await TextFile.WriteAllTextAsync(output.FullName, objText, ct);

            var result = new
            {
                ok = errors.Count == 0,
                bounds = geoBounds.ToString(),
                output = output.FullName,
                provider,
                conformToTerrain,
                airGapped,
                errors,
                steps
            };

            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(result));
            }
            else
            {
                Console.Out.WriteLine($"Wrote OBJ: {output.FullName}");
            }

            return errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoVector roads-to-obj failed");
            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            }
            else
            {
                Console.Error.WriteLine(ex.Message);
            }
            return 1;
        }
    }

    public async Task<int> WaterToObjAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        string? osmPbfPath,
        bool generateTexCoords,
        float uvMetersPerRepeat,
        bool conformToTerrain,
        float surfaceOffsetMeters,
        string terrainProvider,
        string? terrainLocalRoot,
        string? terrainSrtmBaseUrl,
        bool terrainPersistDownloads,
        bool terrainEnableCache,
        bool terrainTreatNoDataAsZero,
        bool airGapped,
        bool forceAgenticFail,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (output is null) throw new ArgumentNullException(nameof(output));
            var geoBounds = GeoBounds.Parse(bounds);
            geoBounds.Validate();

            if (airGapped && string.Equals(provider?.Trim(), "mapbox", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Air-gapped mode cannot use the Mapbox provider. Use --vector-provider echo|osm|hybrid with an offline source.");
            }

            var origin = geoBounds.Center;

            var vectorProvider = BuildVectorProvider(provider ?? "echo", geoBounds, mapboxAccessToken, mapboxTileset, mapboxZoom, osmPbfPath, airGapped);

            ElevationGrid? terrainGrid = null;
            if (conformToTerrain)
            {
                var elevationProvider = BuildElevationProvider(
                    terrainProvider,
                    terrainLocalRoot,
                    terrainSrtmBaseUrl,
                    terrainPersistDownloads,
                    terrainEnableCache,
                    airGapped);

                terrainGrid = await BuildTerrainGridAsync(geoBounds, elevationProvider, ct);
            }

            var bricks = new Brick[]
            {
                new GeoVectorFetchFeaturesBrick(vectorProvider, _providerFactory, _loggerFactory.CreateLogger<GeoVectorFetchFeaturesBrick>()),
                new GeoVectorWaterToMeshBrick(_providerFactory, _loggerFactory.CreateLogger<GeoVectorWaterToMeshBrick>()),
                new GeoVectorObjFromMeshBrick(_loggerFactory.CreateLogger<GeoVectorObjFromMeshBrick>())
            };

            var brickRegistry = new BrickRegistry(bricks);
            var semanticCache = new SemanticCache(_loggerFactory.CreateLogger<SemanticCache>());
            var exec = new BehaviorExecutor(
                brickRegistry,
                _providerFactory,
                semanticCache,
                _loopKernel,
                _loggerFactory.CreateLogger<BehaviorExecutor>());

            var behavior = new Behavior
            {
                Id = "geovector.cli.water-to-obj",
                Name = "GeoVector: water -> OBJ",
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
                        BrickId = "geovector.water.to-mesh",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["features"] = "features",
                            ["origin"] = "origin",
                            ["generateTexCoords"] = "generateTexCoords",
                            ["uvMetersPerRepeat"] = "uvMetersPerRepeat",
                            ["conformToTerrain"] = "conformToTerrain",
                            ["surfaceOffsetMeters"] = "surfaceOffsetMeters",
                            ["terrainGrid"] = "terrainGrid",
                            ["terrainTreatNoDataAsZero"] = "terrainTreatNoDataAsZero",
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

            var agent = new AgentCard { Id = "geovector.cli", Name = "GeoVector CLI", Behaviors = [behavior.Id] };

            var options = new ExecutionOptions
            {
                Provider = "offline",
                IsAirGapped = airGapped,
                ImplementationMode = airGapped ? ImplementationMode.DeterministicOnly : ImplementationMode.Auto,
                SwapOnFailure = true
            };

            var inputData = new Dictionary<string, object>
            {
                ["bounds"] = geoBounds,
                ["origin"] = origin,
                ["kind"] = "water",
                ["generateTexCoords"] = generateTexCoords,
                ["uvMetersPerRepeat"] = uvMetersPerRepeat,
                ["conformToTerrain"] = conformToTerrain,
                ["surfaceOffsetMeters"] = surfaceOffsetMeters,
                ["terrainTreatNoDataAsZero"] = terrainTreatNoDataAsZero,
                ["forceAgenticFail"] = forceAgenticFail
            };
            if (conformToTerrain)
            {
                inputData["terrainGrid"] = terrainGrid ?? throw new InvalidOperationException("Terrain grid was not built.");
            }

            var input = new BehaviorInput(inputData);

            IReadOnlyDictionary<string, object> outputs = new Dictionary<string, object>();
            var steps = new List<object>();
            var errors = new List<string>();

            await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
            {
                switch (evt)
                {
                    case StepStartedEvent s:
                        steps.Add(new { type = "step_started", s.StepId, s.BrickId, s.Implementation, s.UsedFallback });
                        if (verbose && !json) Console.Out.WriteLine($"step:start id={s.StepId} brick={s.BrickId} impl={s.Implementation} fallback={s.UsedFallback}");
                        break;
                    case StepCompletedEvent c:
                        steps.Add(new { type = "step_completed", c.StepId, c.BrickId, c.Implementation, latencyMs = c.LatencyMs, c.Summary });
                        break;
                    case StepErrorEvent e:
                        errors.Add($"{e.StepId}: {e.Error}");
                        steps.Add(new { type = "step_error", e.StepId, e.Error, latencyMs = e.LatencyMs });
                        break;
                    case BehaviorCompletedEvent b:
                        outputs = b.Outputs;
                        steps.Add(new { type = "behavior_completed", b.Success });
                        break;
                }
            }

            var objText = outputs.TryGetValue("objText", out var o) ? o as string : null;
            if (string.IsNullOrWhiteSpace(objText))
                throw new InvalidOperationException("OBJ output was not produced.");

            DirectoryOps.EnsureParentDirectoryExists(output.FullName);
            await TextFile.WriteAllTextAsync(output.FullName, objText, ct);

            var result = new
            {
                ok = errors.Count == 0,
                bounds = geoBounds.ToString(),
                output = output.FullName,
                provider,
                conformToTerrain,
                airGapped,
                errors,
                steps
            };

            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(result));
            }
            else
            {
                Console.Out.WriteLine($"Wrote OBJ: {output.FullName}");
            }

            return errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoVector water-to-obj failed");
            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            }
            else
            {
                Console.Error.WriteLine(ex.Message);
            }
            return 1;
        }
    }


    private IVectorProvider BuildVectorProvider(
        string provider,
        GeoBounds bounds,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        string? osmPbfPath,
        bool airGapped)
    {
        var factory = new VectorProviderFactory(_httpClientFactory, _loggerFactory);
        return factory.Build(provider, bounds, mapboxAccessToken, mapboxTileset, mapboxZoom, osmPbfPath, airGapped);
    }

    private IElevationProvider BuildElevationProvider(
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
        bool airGapped)
    {
        var factory = new ElevationProviderFactory(_httpClientFactory, _loggerFactory);
        return factory.Build(provider, localRoot, srtmBaseUrl, persistDownloads, enableCache, airGapped);
    }

    private async Task<ElevationGrid> BuildTerrainGridAsync(GeoBounds bounds, IElevationProvider elevationProvider, CancellationToken ct)
    {
        bounds.Validate();
        var tileIds = SrtmTileCoverage.TilesCovering(bounds);
        if (tileIds.Count == 0)
            throw new InvalidOperationException("No SRTM tiles cover the requested bounds.");

        var dict = new Dictionary<SrtmTileId, byte[]>(tileIds.Count);
        var gate = new object();

        await _loopKernel.ForEachAsync(
            tileIds,
            async (tileId, i, loopCt) =>
            {
                var tile = await elevationProvider.GetSrtmTileAsync(tileId, loopCt);
                lock (gate)
                {
                    dict[tileId] = tile.HgtBytes;
                }
                return LoopAction.Continue;
            },
            new LoopOptions { Name = "geovector.terrain.fetch-tiles", EnableParallel = true },
            ct);

        return SrtmMosaicBuilder.Build(dict);
    }

}

