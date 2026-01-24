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

namespace Nexo.CLI.Commands.GeoVector;

public sealed class GeoVectorCommand
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
            var geoBounds = ParseBounds(bounds);
            geoBounds.Validate();

            if (airGapped && string.Equals(provider?.Trim(), "mapbox", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Air-gapped mode cannot use the Mapbox provider. Use --vector-provider echo|osm|hybrid with an offline source.");
            }

            var origin = new GeoPoint
            {
                Latitude = new Latitude((geoBounds.MinLatitude.Degrees + geoBounds.MaxLatitude.Degrees) * 0.5),
                Longitude = new Longitude((geoBounds.MinLongitude.Degrees + geoBounds.MaxLongitude.Degrees) * 0.5)
            };

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
            var geoBounds = ParseBounds(bounds);
            geoBounds.Validate();

            if (airGapped && string.Equals(provider?.Trim(), "mapbox", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Air-gapped mode cannot use the Mapbox provider. Use --vector-provider echo|osm|hybrid with an offline source.");
            }

            var origin = new GeoPoint
            {
                Latitude = new Latitude((geoBounds.MinLatitude.Degrees + geoBounds.MaxLatitude.Degrees) * 0.5),
                Longitude = new Longitude((geoBounds.MinLongitude.Degrees + geoBounds.MaxLongitude.Degrees) * 0.5)
            };

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
            var geoBounds = ParseBounds(bounds);
            geoBounds.Validate();

            if (airGapped && string.Equals(provider?.Trim(), "mapbox", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Air-gapped mode cannot use the Mapbox provider. Use --vector-provider echo|osm|hybrid with an offline source.");
            }

            var origin = new GeoPoint
            {
                Latitude = new Latitude((geoBounds.MinLatitude.Degrees + geoBounds.MaxLatitude.Degrees) * 0.5),
                Longitude = new Longitude((geoBounds.MinLongitude.Degrees + geoBounds.MaxLongitude.Degrees) * 0.5)
            };

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

    private static GeoBounds ParseBounds(string text)
    {
        // Format: "minLat,minLon,maxLat,maxLon"
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("bounds is required (minLat,minLon,maxLat,maxLon).", nameof(text));

        var parts = text.Split(',');
        if (parts.Length != 4)
            throw new ArgumentException("bounds must be 'minLat,minLon,maxLat,maxLon'.", nameof(text));

        var minLat = double.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var minLon = double.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var maxLat = double.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var maxLon = double.Parse(parts[3].Trim(), System.Globalization.CultureInfo.InvariantCulture);

        return new GeoBounds
        {
            MinLatitude = new Latitude(minLat),
            MinLongitude = new Longitude(minLon),
            MaxLatitude = new Latitude(maxLat),
            MaxLongitude = new Longitude(maxLon)
        };
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
        provider = (provider ?? "echo").Trim().ToLowerInvariant();
        IVectorProvider offline = provider switch
        {
            "osm" or "hybrid" => string.IsNullOrWhiteSpace(osmPbfPath)
                ? throw new InvalidOperationException("OSM PBF path is required for --vector-provider osm|hybrid. Use --osm-pbf <path>.")
                : new OsmPbfVectorProvider(osmPbfPath, _loggerFactory.CreateLogger<OsmPbfVectorProvider>()),
            _ => new EchoVectorProvider()
        };

        if (provider == "osm")
        {
            return new CachedVectorProvider(offline);
        }

        if (provider == "echo")
        {
            return new CachedVectorProvider(offline);
        }

        MapboxVectorTileProvider? online = null;
        if (!airGapped)
        {
            online = new MapboxVectorTileProvider(
                _httpClientFactory.CreateClient("geovector.mapbox"),
                accessToken: ResolveMapboxToken(mapboxAccessToken),
                tilesetId: string.IsNullOrWhiteSpace(mapboxTileset) ? "mapbox.mapbox-streets-v8" : mapboxTileset!,
                zoom: mapboxZoom ?? 15,
                formatExtension: "mvt",
                logger: _loggerFactory.CreateLogger<MapboxVectorTileProvider>());
        }

        return provider switch
        {
            "mapbox" => online is null
                ? throw new InvalidOperationException("Mapbox provider is not available in air-gapped mode.")
                : new CachedVectorProvider(online),
            "hybrid" => new CachedVectorProvider(new HybridVectorProvider(offline, online, _loggerFactory.CreateLogger<HybridVectorProvider>())),
            _ => throw new InvalidOperationException($"Unknown vector provider '{provider}'. Use echo|osm|mapbox|hybrid.")
        };
    }

    private IElevationProvider BuildElevationProvider(
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
        bool airGapped)
    {
        provider = (provider ?? "echo").Trim().ToLowerInvariant();

        if (airGapped && provider is "http" or "srtmhttp" or "hybrid")
        {
            provider = "local";
        }

        IElevationProvider inner = provider switch
        {
            "echo" => new EchoElevationProvider(),
            "local" => new LocalFileElevationProvider(localRoot),
            "http" or "srtmhttp" => new SrtmHttpElevationProvider(
                _httpClientFactory.CreateClient("geoterrain.srtm"),
                srtmBaseUrl,
                _loggerFactory.CreateLogger<SrtmHttpElevationProvider>()),
            "hybrid" => BuildHybrid(localRoot, srtmBaseUrl, persistDownloads),
            _ => throw new InvalidOperationException($"Unknown elevation provider '{provider}'. Use echo|local|http|hybrid.")
        };

        return enableCache ? new CachedElevationProvider(inner) : inner;
    }

    private IElevationProvider BuildHybrid(string? localRoot, string? srtmBaseUrl, bool persistDownloads)
    {
        if (string.IsNullOrWhiteSpace(localRoot))
        {
            return new SrtmHttpElevationProvider(
                _httpClientFactory.CreateClient("geoterrain.srtm"),
                srtmBaseUrl,
                _loggerFactory.CreateLogger<SrtmHttpElevationProvider>());
        }

        var local = new LocalFileElevationProvider(localRoot);
        var http = new SrtmHttpElevationProvider(
            _httpClientFactory.CreateClient("geoterrain.srtm"),
            srtmBaseUrl,
            _loggerFactory.CreateLogger<SrtmHttpElevationProvider>());

        return new HybridLocalThenHttpElevationProvider(
            local,
            http,
            persistDownloads,
            _loggerFactory.CreateLogger<HybridLocalThenHttpElevationProvider>());
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

    private static string ResolveMapboxToken(string? token)
    {
        token ??= Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Mapbox access token is required. Pass --mapbox-token or set MAPBOX_ACCESS_TOKEN.");
        return token;
    }
}

