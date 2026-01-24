using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Bricks;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.IO;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.CLI.Commands.GeoTerrain;

public sealed class GeoTerrainCommand
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<GeoTerrainCommand> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoopKernel _loopKernel;

    public GeoTerrainCommand(
        IProviderFactory providerFactory,
        ILoggerFactory loggerFactory,
        ILogger<GeoTerrainCommand> logger,
        IHttpClientFactory httpClientFactory,
        ILoopKernel loopKernel)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loopKernel = loopKernel ?? throw new ArgumentNullException(nameof(loopKernel));
    }

    public async Task<int> TileToObjAsync(
        string tile,
        FileInfo output,
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
        bool airGapped,
        bool forceAgenticFail,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (output is null) throw new ArgumentNullException(nameof(output));
            if (string.IsNullOrWhiteSpace(tile)) throw new ArgumentException("tile is required", nameof(tile));

            var elevationProvider = BuildElevationProvider(
                provider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                enableCache,
                airGapped);

            // Build bricks + registries for behavior execution (evented, swap-on-failure aware).
            var bricks = new Brick[]
            {
                new GeoTerrainFetchSrtmTileBrick(
                    elevationProvider,
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainFetchSrtmTileBrick>()),
                new GeoTerrainMeshFromHgtBrick(
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainMeshFromHgtBrick>()),
                new GeoTerrainObjFromMeshBrick(
                    _loggerFactory.CreateLogger<GeoTerrainObjFromMeshBrick>())
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
                Id = "geoterrain.cli.tile-to-obj",
                Name = "GeoTerrain: tile -> OBJ",
                Description = "Fetch a tile (local/http/hybrid), mesh it, and export OBJ text.",
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
                            ["mesh"] = "mesh",
                            ["quality"] = "quality"
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
                }
            };

            var agent = new AgentCard
            {
                Id = "geoterrain.cli",
                Name = "GeoTerrain CLI",
                Description = "CLI runner",
                Behaviors = new[] { behavior.Id }
            };

            var options = new ExecutionOptions
            {
                IsAirGapped = airGapped,
                Provider = airGapped ? "offline" : "offline", // safe default; can be made configurable later
                ImplementationMode = airGapped ? Nexo.Core.Domain.Workflows.ImplementationMode.DeterministicOnly : Nexo.Core.Domain.Workflows.ImplementationMode.Auto,
                SwapOnFailure = true
            };

            var outputs = new Dictionary<string, object>();
            var steps = new List<object>();
            var errors = new List<string>();

            var input = new BehaviorInput(new Dictionary<string, object>
            {
                ["tile"] = tile,
                ["forceAgenticFail"] = forceAgenticFail
            });

            await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
            {
                switch (evt)
                {
                    case StepStartedEvent s:
                        if (verbose && !json)
                        {
                            Console.Out.WriteLine($"step:start id={s.StepId} brick={s.BrickId} impl={s.Implementation} fallback={s.UsedFallback}");
                        }
                        steps.Add(new { type = "step_started", s.StepId, s.BrickId, s.Implementation, s.UsedFallback });
                        break;
                    case StepCompletedEvent c:
                        steps.Add(new { type = "step_completed", c.StepId, c.BrickId, c.Implementation, latencyMs = c.LatencyMs, c.Summary });
                        break;
                    case StepErrorEvent e:
                        errors.Add($"{e.StepId}: {e.Error}");
                        steps.Add(new { type = "step_error", e.StepId, e.Error, latencyMs = e.LatencyMs });
                        break;
                    case BehaviorCompletedEvent b:
                        outputs = new Dictionary<string, object>(b.Outputs);
                        steps.Add(new { type = "behavior_completed", b.Success });
                        break;
                }
            }

            var objText = outputs.TryGetValue("objText", out var objObj) ? objObj as string : null;
            if (string.IsNullOrWhiteSpace(objText))
            {
                throw new InvalidOperationException("OBJ output was not produced by the pipeline.");
            }

            DirectoryOps.EnsureParentDirectoryExists(output.FullName);
            await TextFile.WriteAllTextAsync(output.FullName, objText, ct);

            var result = new
            {
                ok = errors.Count == 0,
                tile,
                output = output.FullName,
                provider,
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
                if (errors.Count > 0)
                {
                    Console.Out.WriteLine("Errors:");
                    foreach (var e in errors) Console.Out.WriteLine($"- {e}");
                }
            }

            return errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoTerrain tile-to-obj failed");
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

    public async Task<int> BoundsToObjAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
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

            var elevationProvider = BuildElevationProvider(
                provider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                enableCache,
                airGapped);

            var bricks = new Brick[]
            {
                new GeoTerrainFetchSrtmTilesBrick(
                    elevationProvider,
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainFetchSrtmTilesBrick>()),
                new GeoTerrainGridFromTilesBrick(
                    _loggerFactory.CreateLogger<GeoTerrainGridFromTilesBrick>()),
                new GeoTerrainMeshFromGridBrick(
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainMeshFromGridBrick>()),
                new GeoTerrainObjFromMeshBrick(
                    _loggerFactory.CreateLogger<GeoTerrainObjFromMeshBrick>())
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
                Id = "geoterrain.cli.bounds-to-obj",
                Name = "GeoTerrain: bounds -> OBJ",
                Description = "Fetch all tiles covering bounds, stitch, mesh, export OBJ text.",
                Steps =
                [
                    new BehaviorStep
                    {
                        Id = "fetch",
                        BrickId = "geoterrain.fetch.srtm-tiles",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["bounds"] = "bounds",
                            ["forceAgenticFail"] = "forceAgenticFail"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["tiles"] = "tiles"
                        }
                    },
                    new BehaviorStep
                    {
                        Id = "grid",
                        BrickId = "geoterrain.grid.from-tiles",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["tiles"] = "tiles"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["grid"] = "grid"
                        }
                    },
                    new BehaviorStep
                    {
                        Id = "mesh",
                        BrickId = "geoterrain.mesh.from-grid",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["grid"] = "grid",
                            ["forceAgenticFail"] = "forceAgenticFail"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["mesh"] = "mesh",
                            ["quality"] = "quality"
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
                ]
            };

            var agent = new AgentCard
            {
                Id = "geoterrain.cli",
                Name = "GeoTerrain CLI",
                Description = "CLI runner",
                Behaviors = [behavior.Id]
            };

            var options = new ExecutionOptions
            {
                IsAirGapped = airGapped,
                Provider = airGapped ? "offline" : "offline",
                ImplementationMode = airGapped ? Nexo.Core.Domain.Workflows.ImplementationMode.DeterministicOnly : Nexo.Core.Domain.Workflows.ImplementationMode.Auto,
                SwapOnFailure = true
            };

            IReadOnlyDictionary<string, object> outputs = new Dictionary<string, object>();
            var steps = new List<object>();
            var errors = new List<string>();

            var input = new BehaviorInput(new Dictionary<string, object>
            {
                ["bounds"] = geoBounds,
                ["forceAgenticFail"] = forceAgenticFail
            });

            await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
            {
                switch (evt)
                {
                    case StepStartedEvent s:
                        if (verbose && !json)
                            Console.Out.WriteLine($"step:start id={s.StepId} brick={s.BrickId} impl={s.Implementation} fallback={s.UsedFallback}");
                        steps.Add(new { type = "step_started", s.StepId, s.BrickId, s.Implementation, s.UsedFallback });
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
                throw new InvalidOperationException("OBJ output was not produced by the pipeline.");

            DirectoryOps.EnsureParentDirectoryExists(output.FullName);
            await TextFile.WriteAllTextAsync(output.FullName, objText, ct);

            var result = new
            {
                ok = errors.Count == 0,
                bounds = geoBounds.ToString(),
                output = output.FullName,
                provider,
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
            _logger.LogError(ex, "GeoTerrain bounds-to-obj failed");
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

    public async Task<int> TileToContoursAsync(
        string tile,
        FileInfo output,
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
        bool airGapped,
        bool forceAgenticFail,
        double intervalMeters,
        double? minElevationMeters,
        double? maxElevationMeters,
        float verticalScale,
        bool treatNoDataAsZero,
        bool includeElevation,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (output is null) throw new ArgumentNullException(nameof(output));
            if (string.IsNullOrWhiteSpace(tile)) throw new ArgumentException("tile is required", nameof(tile));

            var elevationProvider = BuildElevationProvider(
                provider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                enableCache,
                airGapped);

            var bricks = new Brick[]
            {
                new GeoTerrainFetchSrtmTileBrick(
                    elevationProvider,
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainFetchSrtmTileBrick>()),
                new GeoTerrainContoursFromHgtBrick(
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainContoursFromHgtBrick>()),
                new GeoTerrainGeoJsonFromContoursBrick(
                    _loggerFactory.CreateLogger<GeoTerrainGeoJsonFromContoursBrick>())
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
                Id = "geoterrain.cli.tile-to-contours",
                Name = "GeoTerrain: tile -> contours (GeoJSON)",
                Description = "Fetch a tile (local/http/hybrid), extract contours, and export GeoJSON text.",
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
                        Id = "contours",
                        BrickId = "geoterrain.contours.from-hgt",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["tileId"] = "tileId",
                            ["hgtBytes"] = "hgtBytes",
                            ["intervalMeters"] = "intervalMeters",
                            ["minElevationMeters"] = "minElevationMeters",
                            ["maxElevationMeters"] = "maxElevationMeters",
                            ["verticalScale"] = "verticalScale",
                            ["treatNoDataAsZero"] = "treatNoDataAsZero",
                            ["forceAgenticFail"] = "forceAgenticFail"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["grid"] = "grid",
                            ["contours"] = "contours"
                        }
                    },
                    new BehaviorStep
                    {
                        Id = "geojson",
                        BrickId = "geoterrain.export.contours-geojson",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["grid"] = "grid",
                            ["contours"] = "contours",
                            ["includeElevation"] = "includeElevation"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["geojsonText"] = "geojson"
                        }
                    }
                }
            };

            var agent = new AgentCard
            {
                Id = "geoterrain.cli",
                Name = "GeoTerrain CLI",
                Description = "CLI runner",
                Behaviors = new[] { behavior.Id }
            };

            var options = new ExecutionOptions
            {
                IsAirGapped = airGapped,
                Provider = airGapped ? "offline" : "offline",
                ImplementationMode = airGapped ? Nexo.Core.Domain.Workflows.ImplementationMode.DeterministicOnly : Nexo.Core.Domain.Workflows.ImplementationMode.Auto,
                SwapOnFailure = true
            };

            var outputs = new Dictionary<string, object>();
            var steps = new List<object>();
            var errors = new List<string>();

            var inputData = new Dictionary<string, object>
            {
                ["tile"] = tile,
                ["forceAgenticFail"] = forceAgenticFail,
                ["intervalMeters"] = intervalMeters,
                ["verticalScale"] = verticalScale,
                ["treatNoDataAsZero"] = treatNoDataAsZero,
                ["includeElevation"] = includeElevation
            };
            if (minElevationMeters.HasValue) inputData["minElevationMeters"] = minElevationMeters.Value;
            if (maxElevationMeters.HasValue) inputData["maxElevationMeters"] = maxElevationMeters.Value;

            var input = new BehaviorInput(inputData);

            await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
            {
                switch (evt)
                {
                    case StepStartedEvent s:
                        if (verbose && !json)
                        {
                            Console.Out.WriteLine($"step:start id={s.StepId} brick={s.BrickId} impl={s.Implementation} fallback={s.UsedFallback}");
                        }
                        steps.Add(new { type = "step_started", s.StepId, s.BrickId, s.Implementation, s.UsedFallback });
                        break;
                    case StepCompletedEvent c:
                        steps.Add(new { type = "step_completed", c.StepId, c.BrickId, c.Implementation, latencyMs = c.LatencyMs, c.Summary });
                        break;
                    case StepErrorEvent e:
                        errors.Add($"{e.StepId}: {e.Error}");
                        steps.Add(new { type = "step_error", e.StepId, e.Error, latencyMs = e.LatencyMs });
                        break;
                    case BehaviorCompletedEvent b:
                        outputs = new Dictionary<string, object>(b.Outputs);
                        steps.Add(new { type = "behavior_completed", b.Success });
                        break;
                }
            }

            var geojsonText = outputs.TryGetValue("geojsonText", out var gj) ? gj as string : null;
            if (string.IsNullOrWhiteSpace(geojsonText))
            {
                throw new InvalidOperationException("GeoJSON output was not produced by the pipeline.");
            }

            DirectoryOps.EnsureParentDirectoryExists(output.FullName);
            await TextFile.WriteAllTextAsync(output.FullName, geojsonText, ct);

            var result = new
            {
                ok = errors.Count == 0,
                tile,
                output = output.FullName,
                provider,
                airGapped,
                intervalMeters,
                minElevationMeters,
                maxElevationMeters,
                errors,
                steps
            };

            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(result));
            }
            else
            {
                Console.Out.WriteLine($"Wrote GeoJSON: {output.FullName}");
                if (errors.Count > 0)
                {
                    Console.Out.WriteLine("Errors:");
                    foreach (var e in errors) Console.Out.WriteLine($"- {e}");
                }
            }

            return errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoTerrain tile-to-contours failed");
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

    public async Task<int> BoundsToContoursAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
        bool airGapped,
        bool forceAgenticFail,
        double intervalMeters,
        double? minElevationMeters,
        double? maxElevationMeters,
        float verticalScale,
        bool treatNoDataAsZero,
        bool includeElevation,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (output is null) throw new ArgumentNullException(nameof(output));
            var geoBounds = ParseBounds(bounds);
            geoBounds.Validate();

            var elevationProvider = BuildElevationProvider(
                provider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                enableCache,
                airGapped);

            var bricks = new Brick[]
            {
                new GeoTerrainFetchSrtmTilesBrick(
                    elevationProvider,
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainFetchSrtmTilesBrick>()),
                new GeoTerrainGridFromTilesBrick(
                    _loggerFactory.CreateLogger<GeoTerrainGridFromTilesBrick>()),
                new GeoTerrainContoursFromGridBrick(
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainContoursFromGridBrick>()),
                new GeoTerrainGeoJsonFromContoursBrick(
                    _loggerFactory.CreateLogger<GeoTerrainGeoJsonFromContoursBrick>())
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
                Id = "geoterrain.cli.bounds-to-contours",
                Name = "GeoTerrain: bounds -> contours (GeoJSON)",
                Description = "Fetch all tiles covering bounds, stitch, extract contours, export GeoJSON text.",
                Steps =
                [
                    new BehaviorStep
                    {
                        Id = "fetch",
                        BrickId = "geoterrain.fetch.srtm-tiles",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["bounds"] = "bounds",
                            ["forceAgenticFail"] = "forceAgenticFail"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["tiles"] = "tiles"
                        }
                    },
                    new BehaviorStep
                    {
                        Id = "grid",
                        BrickId = "geoterrain.grid.from-tiles",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["tiles"] = "tiles"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["grid"] = "grid"
                        }
                    },
                    new BehaviorStep
                    {
                        Id = "contours",
                        BrickId = "geoterrain.contours.from-grid",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["grid"] = "grid",
                            ["intervalMeters"] = "intervalMeters",
                            ["minElevationMeters"] = "minElevationMeters",
                            ["maxElevationMeters"] = "maxElevationMeters",
                            ["verticalScale"] = "verticalScale",
                            ["treatNoDataAsZero"] = "treatNoDataAsZero",
                            ["forceAgenticFail"] = "forceAgenticFail"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["contours"] = "contours"
                        }
                    },
                    new BehaviorStep
                    {
                        Id = "geojson",
                        BrickId = "geoterrain.export.contours-geojson",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["grid"] = "grid",
                            ["contours"] = "contours",
                            ["includeElevation"] = "includeElevation"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["geojsonText"] = "geojson"
                        }
                    }
                ]
            };

            var agent = new AgentCard
            {
                Id = "geoterrain.cli",
                Name = "GeoTerrain CLI",
                Description = "CLI runner",
                Behaviors = [behavior.Id]
            };

            var options = new ExecutionOptions
            {
                IsAirGapped = airGapped,
                Provider = airGapped ? "offline" : "offline",
                ImplementationMode = airGapped ? Nexo.Core.Domain.Workflows.ImplementationMode.DeterministicOnly : Nexo.Core.Domain.Workflows.ImplementationMode.Auto,
                SwapOnFailure = true
            };

            IReadOnlyDictionary<string, object> outputs = new Dictionary<string, object>();
            var steps = new List<object>();
            var errors = new List<string>();

            var inputData = new Dictionary<string, object>
            {
                ["bounds"] = geoBounds,
                ["forceAgenticFail"] = forceAgenticFail,
                ["intervalMeters"] = intervalMeters,
                ["verticalScale"] = verticalScale,
                ["treatNoDataAsZero"] = treatNoDataAsZero,
                ["includeElevation"] = includeElevation
            };
            if (minElevationMeters.HasValue) inputData["minElevationMeters"] = minElevationMeters.Value;
            if (maxElevationMeters.HasValue) inputData["maxElevationMeters"] = maxElevationMeters.Value;

            var input = new BehaviorInput(inputData);

            await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
            {
                switch (evt)
                {
                    case StepStartedEvent s:
                        if (verbose && !json)
                            Console.Out.WriteLine($"step:start id={s.StepId} brick={s.BrickId} impl={s.Implementation} fallback={s.UsedFallback}");
                        steps.Add(new { type = "step_started", s.StepId, s.BrickId, s.Implementation, s.UsedFallback });
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

            var geojsonText = outputs.TryGetValue("geojsonText", out var gj) ? gj as string : null;
            if (string.IsNullOrWhiteSpace(geojsonText))
                throw new InvalidOperationException("GeoJSON output was not produced by the pipeline.");

            DirectoryOps.EnsureParentDirectoryExists(output.FullName);
            await TextFile.WriteAllTextAsync(output.FullName, geojsonText, ct);

            var result = new
            {
                ok = errors.Count == 0,
                bounds = geoBounds.ToString(),
                output = output.FullName,
                provider,
                airGapped,
                intervalMeters,
                minElevationMeters,
                maxElevationMeters,
                errors,
                steps
            };

            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(result));
            }
            else
            {
                Console.Out.WriteLine($"Wrote GeoJSON: {output.FullName}");
            }

            return errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoTerrain bounds-to-contours failed");
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

    public async Task<int> BoundsToTreeInstancesAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
        bool airGapped,
        float treesPerSqKm,
        int seed,
        bool treatNoDataAsZero,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (output is null) throw new ArgumentNullException(nameof(output));
            var geoBounds = ParseBounds(bounds);
            geoBounds.Validate();

            var elevationProvider = BuildElevationProvider(
                provider,
                localRoot,
                srtmBaseUrl,
                persistDownloads,
                enableCache,
                airGapped);

            var bricks = new Brick[]
            {
                new GeoTerrainFetchSrtmTilesBrick(
                    elevationProvider,
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainFetchSrtmTilesBrick>()),
                new GeoTerrainGridFromTilesBrick(
                    _loggerFactory.CreateLogger<GeoTerrainGridFromTilesBrick>()),
                new GeoTerrainTreesFromGridBrick(
                    _loggerFactory.CreateLogger<GeoTerrainTreesFromGridBrick>()),
                new GeoTerrainInstancesJsonBrick(
                    _loggerFactory.CreateLogger<GeoTerrainInstancesJsonBrick>())
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
                Id = "geoterrain.cli.bounds-to-tree-instances",
                Name = "GeoTerrain: bounds -> tree instances (JSON)",
                Description = "Fetch tiles covering bounds, stitch, place tree instances, export JSON text.",
                Steps =
                [
                    new BehaviorStep
                    {
                        Id = "fetch",
                        BrickId = "geoterrain.fetch.srtm-tiles",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["bounds"] = "bounds"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["tiles"] = "tiles"
                        }
                    },
                    new BehaviorStep
                    {
                        Id = "grid",
                        BrickId = "geoterrain.grid.from-tiles",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["tiles"] = "tiles"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["grid"] = "grid"
                        }
                    },
                    new BehaviorStep
                    {
                        Id = "trees",
                        BrickId = "geoterrain.trees.from-grid",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["grid"] = "grid",
                            ["bounds"] = "bounds",
                            ["treesPerSqKm"] = "treesPerSqKm",
                            ["seed"] = "seed",
                            ["treatNoDataAsZero"] = "treatNoDataAsZero"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["instances"] = "instances"
                        }
                    },
                    new BehaviorStep
                    {
                        Id = "json",
                        BrickId = "geoterrain.export.instances-json",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["instances"] = "instances"
                        },
                        OutputMapping = new Dictionary<string, string>
                        {
                            ["jsonText"] = "jsonText"
                        }
                    }
                ]
            };

            var agent = new AgentCard
            {
                Id = "geoterrain.cli",
                Name = "GeoTerrain CLI",
                Description = "CLI runner",
                Behaviors = [behavior.Id]
            };

            var options = new ExecutionOptions
            {
                IsAirGapped = airGapped,
                Provider = airGapped ? "offline" : "offline",
                ImplementationMode = airGapped ? Nexo.Core.Domain.Workflows.ImplementationMode.DeterministicOnly : Nexo.Core.Domain.Workflows.ImplementationMode.Auto,
                SwapOnFailure = true
            };

            IReadOnlyDictionary<string, object> outputs = new Dictionary<string, object>();
            var steps = new List<object>();
            var errors = new List<string>();

            var input = new BehaviorInput(new Dictionary<string, object>
            {
                ["bounds"] = geoBounds,
                ["treesPerSqKm"] = treesPerSqKm,
                ["seed"] = seed,
                ["treatNoDataAsZero"] = treatNoDataAsZero
            });

            await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, options, ct))
            {
                switch (evt)
                {
                    case StepStartedEvent s:
                        if (verbose && !json)
                            Console.Out.WriteLine($"step:start id={s.StepId} brick={s.BrickId} impl={s.Implementation} fallback={s.UsedFallback}");
                        steps.Add(new { type = "step_started", s.StepId, s.BrickId, s.Implementation, s.UsedFallback });
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

            var jsonText = outputs.TryGetValue("jsonText", out var t) ? t as string : null;
            if (string.IsNullOrWhiteSpace(jsonText))
                throw new InvalidOperationException("Instances JSON was not produced by the pipeline.");

            DirectoryOps.EnsureParentDirectoryExists(output.FullName);
            await TextFile.WriteAllTextAsync(output.FullName, jsonText, ct);

            var result = new
            {
                ok = errors.Count == 0,
                bounds = geoBounds.ToString(),
                output = output.FullName,
                provider,
                airGapped,
                treesPerSqKm,
                seed,
                errors,
                steps
            };

            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(result));
            }
            else
            {
                Console.Out.WriteLine($"Wrote instances JSON: {output.FullName}");
            }

            return errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoTerrain bounds-to-tree-instances failed");
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

    public async Task<int> TerrainRgbTileToObjAsync(
        int z,
        int x,
        int y,
        FileInfo outputObj,
        FileInfo? outputTexturePng,
        string? mapboxToken,
        string? tilesetId,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (outputObj is null) throw new ArgumentNullException(nameof(outputObj));

            var token = ResolveMapboxToken(mapboxToken);
            var tileset = string.IsNullOrWhiteSpace(tilesetId) ? "mapbox.terrain-rgb" : tilesetId!.Trim();

            var provider = new MapboxTerrainRgbGridProvider(
                _httpClientFactory.CreateClient(),
                token,
                tileset,
                _loggerFactory.CreateLogger<MapboxTerrainRgbGridProvider>());

            var (grid, png) = await provider.GetTileGridAsync(z, x, y, ct);
            var mesh = GridMeshGenerator.Generate(grid, new MeshGenerationOptions { GenerateNormals = true, TreatNoDataAsZero = true });

            // Add simple UVs for the tile texture (0..1).
            var tex = new Vector2[mesh.Vertices.Count];
            var w = grid.Width;
            var h = grid.Height;
            for (var row = 0; row < h; row++)
            {
                for (var col = 0; col < w; col++)
                {
                    var u = w <= 1 ? 0f : (float)col / (float)(w - 1);
                    var v = h <= 1 ? 0f : 1f - ((float)row / (float)(h - 1));
                    tex[row * w + col] = new Vector2(u, v);
                }
            }
            mesh = mesh with { TexCoords = tex };

            var obj = ObjMeshWriter.Write(mesh);

            DirectoryOps.EnsureParentDirectoryExists(outputObj.FullName);
            await TextFile.WriteAllTextAsync(outputObj.FullName, obj, ct);

            if (outputTexturePng != null)
            {
                DirectoryOps.EnsureParentDirectoryExists(outputTexturePng.FullName);
                await BinaryFile.WriteAllBytesAsync(outputTexturePng.FullName, png, ct);
            }

            var result = new
            {
                ok = true,
                z,
                x,
                y,
                outputObj = outputObj.FullName,
                outputTexturePng = outputTexturePng?.FullName,
                tileset = tileset
            };

            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(result));
            }
            else
            {
                Console.Out.WriteLine($"Wrote OBJ: {outputObj.FullName}");
                if (outputTexturePng != null) Console.Out.WriteLine($"Wrote texture: {outputTexturePng.FullName}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoTerrain terrain-rgb-tile-to-obj failed");
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

    public async Task<int> MapboxRasterTileAsync(
        int z,
        int x,
        int y,
        FileInfo output,
        string? mapboxToken,
        string? tilesetId,
        string? format,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (output is null) throw new ArgumentNullException(nameof(output));
            var token = ResolveMapboxToken(mapboxToken);
            var tileset = string.IsNullOrWhiteSpace(tilesetId) ? "mapbox.satellite" : tilesetId!.Trim();
            var fmt = string.IsNullOrWhiteSpace(format) ? "jpg90" : format!.Trim();

            var dl = new MapboxRasterTileDownloader(
                _httpClientFactory.CreateClient(),
                token,
                _loggerFactory.CreateLogger<MapboxRasterTileDownloader>());

            var bytes = await dl.DownloadAsync(tileset, z, x, y, fmt, ct);
            DirectoryOps.EnsureParentDirectoryExists(output.FullName);
            await BinaryFile.WriteAllBytesAsync(output.FullName, bytes, ct);

            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, z, x, y, tileset, format = fmt, output = output.FullName, bytes = bytes.Length }));
            }
            else
            {
                Console.Out.WriteLine($"Wrote raster tile: {output.FullName} ({bytes.Length} bytes)");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoTerrain mapbox-raster-tile failed");
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
            // Air-gapped: do not attempt network. Force local.
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
            _ => throw new InvalidOperationException($"Unknown provider '{provider}'. Use echo|local|http|hybrid.")
        };

        return enableCache ? new CachedElevationProvider(inner) : inner;
    }

    private IElevationProvider BuildHybrid(string? localRoot, string? srtmBaseUrl, bool persistDownloads)
    {
        if (string.IsNullOrWhiteSpace(localRoot))
        {
            // If no local root was provided, fall back to pure HTTP mode.
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

    private static string ResolveMapboxToken(string? token)
    {
        token = string.IsNullOrWhiteSpace(token) ? Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN") : token;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Mapbox token is required (set MAPBOX_ACCESS_TOKEN or pass --mapbox-token).");
        return token.Trim();
    }
}

