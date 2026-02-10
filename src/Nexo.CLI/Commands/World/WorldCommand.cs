using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Adapters.GeoTerrain.Export;
using Nexo.Adapters.GeoTerrain.Processing;
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Adapters.GeoVector.Providers;
using Nexo.CLI.Commands;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Workflows;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Bricks;
using Nexo.GeoVector.Bricks;
using Nexo.GeoVector.Models;
using Nexo.GeoWorld;
using Nexo.GeoWorld.Bricks;
using Nexo.GeoWorld.Materials;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.IO;
using Nexo.Orchestration.GeoTerrain.Ports;
using Nexo.Orchestration.GeoVector.Ports;
using Nexo.Adapters.GeoVector.Utilities;

namespace Nexo.CLI.Commands.World;

public class WorldCommand : IWorldCommand
{
    private readonly Nexo.Infrastructure.Execution.IProviderFactory _providerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<WorldCommand> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoopKernel _loopKernel;

    public WorldCommand(
        Nexo.Infrastructure.Execution.IProviderFactory providerFactory,
        ILoggerFactory loggerFactory,
        ILogger<WorldCommand> logger,
        IHttpClientFactory httpClientFactory,
        ILoopKernel loopKernel)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loopKernel = loopKernel ?? throw new ArgumentNullException(nameof(loopKernel));
    }

    public async Task<int> BuildAsync(
        string bounds,
        DirectoryInfo outDir,
        string terrainElevationProvider,
        string? terrainLocalRoot,
        string? terrainSrtmBaseUrl,
        bool terrainPersistDownloads,
        bool terrainEnableCache,
        string vectorProvider,
        string? osmPbfPath,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        int terrainChunkSamples,
        string? terrainLodFactors,
        string? lodTriBudgets,
        int instancesChunkSamples,
        bool enableTerrainImagery,
        string? terrainImageryTileset,
        string? terrainImageryFormat,
        int? terrainImageryZoom,
        bool waterFlattenToTerrain,
        string meshFormat,
        string projection,
        bool generateVectorTextures,
        bool airGapped,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            if (outDir is null) throw new ArgumentNullException(nameof(outDir));
            var geoBounds = GeoBounds.Parse(bounds);
            geoBounds.Validate();

            var origin = geoBounds.Center;

            var projector = Nexo.GeoTerrain.Projection.CoordinateProjectorFactory.Create(projection, geoBounds);

            var elevationProvider = BuildElevationProvider(
                terrainElevationProvider,
                terrainLocalRoot,
                terrainSrtmBaseUrl,
                terrainPersistDownloads,
                terrainEnableCache,
                airGapped);

            var vector = BuildVectorProvider(vectorProvider, geoBounds, mapboxAccessToken, mapboxTileset, mapboxZoom, osmPbfPath, airGapped);

            // Compose bricks from existing modules.
            var bricks = new Brick[]
            {
                // Terrain
                new GeoTerrainFetchSrtmTilesBrick(
                    elevationProvider,
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainFetchSrtmTilesBrick>()),
                new GeoTerrainGridFromTilesBrick(projector, _loggerFactory.CreateLogger<GeoTerrainGridFromTilesBrick>()),
                new GeoTerrainMeshFromGridBrick(
                    _providerFactory,
                    _loggerFactory.CreateLogger<GeoTerrainMeshFromGridBrick>()),
                new GeoTerrainObjFromMeshBrick(_loggerFactory.CreateLogger<GeoTerrainObjFromMeshBrick>()),

                // Vector
                new GeoVectorFetchFeaturesBrick(vector, _providerFactory, _loggerFactory.CreateLogger<GeoVectorFetchFeaturesBrick>()),
                new GeoVectorBuildingsToMeshBrick(_providerFactory, projector, _loggerFactory.CreateLogger<GeoVectorBuildingsToMeshBrick>()),
                new GeoVectorRoadsToMeshBrick(_providerFactory, projector, _loggerFactory.CreateLogger<GeoVectorRoadsToMeshBrick>()),
                new GeoVectorWaterToMeshBrick(_providerFactory, projector, _loggerFactory.CreateLogger<GeoVectorWaterToMeshBrick>()),
                new GeoVectorObjFromMeshBrick(_loggerFactory.CreateLogger<GeoVectorObjFromMeshBrick>()),

                // Vegetation + exports
                new GeoWorldVegetationFromTerrainBrick(_providerFactory, projector, _loggerFactory.CreateLogger<GeoWorldVegetationFromTerrainBrick>()),
                new GeoWorldCarveTerrainForWaterBrick(_providerFactory, projector, _loggerFactory.CreateLogger<GeoWorldCarveTerrainForWaterBrick>()),
                new GeoTerrainInstancesJsonBrick(_loggerFactory.CreateLogger<GeoTerrainInstancesJsonBrick>()),
                new GeoWorldMaterialsJsonBrick(_loggerFactory.CreateLogger<GeoWorldMaterialsJsonBrick>()),
                new GeoWorldManifestJsonBrick(_loggerFactory.CreateLogger<GeoWorldManifestJsonBrick>())
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
                Id = "geoworld.cli.build",
                Name = "GeoWorld: build bundle",
                Steps =
                [
                    // Terrain pipeline (grid first): bounds -> tiles -> grid
                    new BehaviorStep
                    {
                        Id = "terrain_fetch_tiles",
                        BrickId = "geoterrain.fetch.srtm-tiles",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string> { ["bounds"] = "bounds" },
                        OutputMapping = new Dictionary<string, string> { ["terrainTiles"] = "tiles" }
                    },
                    new BehaviorStep
                    {
                        Id = "terrain_grid",
                        BrickId = "geoterrain.grid.from-tiles",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string> { ["tiles"] = "terrainTiles", ["bounds"] = "bounds" },
                        OutputMapping = new Dictionary<string, string> { ["terrainGrid"] = "grid" }
                    },

                    // Vector fetches (separate outputs)
                    new BehaviorStep
                    {
                        Id = "fetch_buildings",
                        BrickId = "geovector.fetch.features",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string> { ["bounds"] = "bounds", ["kind"] = "buildingsKind" },
                        OutputMapping = new Dictionary<string, string> { ["buildingsFeatures"] = "features" }
                    },
                    new BehaviorStep
                    {
                        Id = "fetch_roads",
                        BrickId = "geovector.fetch.features",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string> { ["bounds"] = "bounds", ["kind"] = "roadsKind" },
                        OutputMapping = new Dictionary<string, string> { ["roadsFeatures"] = "features" }
                    },
                    new BehaviorStep
                    {
                        Id = "fetch_water",
                        BrickId = "geovector.fetch.features",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string> { ["bounds"] = "bounds", ["kind"] = "waterKind" },
                        OutputMapping = new Dictionary<string, string> { ["waterFeatures"] = "features" }
                    },
                    new BehaviorStep
                    {
                        Id = "fetch_vegetation",
                        BrickId = "geovector.fetch.features",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string> { ["bounds"] = "bounds", ["kind"] = "vegetationKind" },
                        OutputMapping = new Dictionary<string, string> { ["vegetationFeatures"] = "features" }
                    },

                    // Hydrology: carve/flatten water into the terrain grid (overwrites terrainGrid).
                    new BehaviorStep
                    {
                        Id = "terrain_carve_water",
                        BrickId = "geoworld.terrain.carve-water",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["terrainGrid"] = "terrainGrid",
                            ["water"] = "waterFeatures",
                            ["origin"] = "origin",
                            ["enabled"] = "carveWaterEnabled",
                            ["treatNoDataAsZero"] = "terrainTreatNoDataAsZero",
                            ["shorelineSmoothIterations"] = "carveWaterSmoothIters",
                            ["shorelineSmoothBlend"] = "carveWaterSmoothBlend",
                            ["waterSurfaceOffsetMeters"] = "carveWaterOffsetMeters"
                        },
                        OutputMapping = new Dictionary<string, string> { ["terrainGrid"] = "grid" }
                    },

                    // Terrain export (after carving): grid -> mesh -> objText
                    new BehaviorStep
                    {
                        Id = "terrain_mesh",
                        BrickId = "geoterrain.mesh.from-grid",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string> { ["grid"] = "terrainGrid" },
                        OutputMapping = new Dictionary<string, string> { ["terrainMesh"] = "mesh" }
                    },
                    new BehaviorStep
                    {
                        Id = "terrain_obj",
                        BrickId = "geoterrain.export.obj-text",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string> { ["mesh"] = "terrainMesh" },
                        OutputMapping = new Dictionary<string, string> { ["terrainObjText"] = "obj" }
                    },

                    // Buildings -> mesh -> obj
                    new BehaviorStep
                    {
                        Id = "buildings_mesh",
                        BrickId = "geovector.buildings.to-mesh",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["features"] = "buildingsFeatures",
                            ["origin"] = "origin",
                            ["generateTexCoords"] = "buildingsGenerateUv",
                            ["uvMetersPerRepeat"] = "buildingsUvMetersPerRepeat",
                            ["alignToTerrain"] = "alignBuildingsToTerrain",
                            ["terrainGrid"] = "terrainGrid",
                            ["terrainTreatNoDataAsZero"] = "terrainTreatNoDataAsZero"
                        },
                        OutputMapping = new Dictionary<string, string> { ["buildingsMesh"] = "mesh" }
                    },
                    new BehaviorStep
                    {
                        Id = "buildings_obj",
                        BrickId = "geovector.export.obj-text",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string> { ["mesh"] = "buildingsMesh" },
                        OutputMapping = new Dictionary<string, string> { ["buildingsObjText"] = "objText" }
                    },

                    // Roads -> mesh -> obj
                    new BehaviorStep
                    {
                        Id = "roads_mesh",
                        BrickId = "geovector.roads.to-mesh",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["features"] = "roadsFeatures",
                            ["origin"] = "origin",
                            ["widthMeters"] = "roadsWidthMeters",
                            ["generateTexCoords"] = "roadsGenerateUv",
                            ["uvMetersPerRepeat"] = "roadsUvMetersPerRepeat",
                            ["conformToTerrain"] = "conformRoadsToTerrain",
                            ["terrainGrid"] = "terrainGrid",
                            ["terrainTreatNoDataAsZero"] = "terrainTreatNoDataAsZero"
                        },
                        OutputMapping = new Dictionary<string, string> { ["roadsMesh"] = "mesh" }
                    },
                    new BehaviorStep
                    {
                        Id = "roads_obj",
                        BrickId = "geovector.export.obj-text",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string> { ["mesh"] = "roadsMesh" },
                        OutputMapping = new Dictionary<string, string> { ["roadsObjText"] = "objText" }
                    },

                    // Water -> mesh -> obj
                    new BehaviorStep
                    {
                        Id = "water_mesh",
                        BrickId = "geovector.water.to-mesh",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["features"] = "waterFeatures",
                            ["origin"] = "origin",
                            ["generateTexCoords"] = "waterGenerateUv",
                            ["uvMetersPerRepeat"] = "waterUvMetersPerRepeat",
                            ["conformToTerrain"] = "conformWaterToTerrain",
                            ["flattenToTerrain"] = "flattenWaterToTerrain",
                            ["surfaceOffsetMeters"] = "waterSurfaceOffsetMeters",
                            ["terrainGrid"] = "terrainGrid",
                            ["terrainTreatNoDataAsZero"] = "terrainTreatNoDataAsZero"
                        },
                        OutputMapping = new Dictionary<string, string> { ["waterMesh"] = "mesh" }
                    },
                    new BehaviorStep
                    {
                        Id = "water_obj",
                        BrickId = "geovector.export.obj-text",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string> { ["mesh"] = "waterMesh" },
                        OutputMapping = new Dictionary<string, string> { ["waterObjText"] = "objText" }
                    },

                    // Vegetation instances -> instances json
                    new BehaviorStep
                    {
                        Id = "vegetation_instances",
                        BrickId = "geoworld.vegetation.from-terrain",
                        Implementation = ImplementationType.Auto,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["terrainGrid"] = "terrainGrid",
                            ["vegetation"] = "vegetationFeatures",
                            ["buildings"] = "buildingsFeatures",
                            ["roads"] = "roadsFeatures",
                            ["water"] = "waterFeatures",
                            ["origin"] = "origin",
                            ["seed"] = "vegSeed",
                            ["instancesPerSqKm"] = "vegDensity",
                            ["minSpacingMeters"] = "vegSpacing",
                            ["maxSlopeDegrees"] = "vegMaxSlope",
                            ["kind"] = "vegKind"
                        },
                        OutputMapping = new Dictionary<string, string> { ["vegInstances"] = "instances" }
                    },
                    new BehaviorStep
                    {
                        Id = "instances_json",
                        BrickId = "geoterrain.export.instances-json",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string> { ["instances"] = "vegInstances" },
                        OutputMapping = new Dictionary<string, string> { ["instancesJsonText"] = "jsonText" }
                    },

                    // materials.json
                    new BehaviorStep
                    {
                        Id = "materials_json",
                        BrickId = "geoworld.export.materials-json",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["buildings"] = "buildingsFeatures",
                            ["roads"] = "roadsFeatures",
                            ["water"] = "waterFeatures",
                            ["buildingsUvMetersPerRepeat"] = "buildingsUvMetersPerRepeat",
                            ["roadsUvMetersPerRepeat"] = "roadsUvMetersPerRepeat",
                            ["waterUvMetersPerRepeat"] = "waterUvMetersPerRepeat",
                            ["buildingsBaseColorTexturePath"] = "buildingsBaseColorTexturePath",
                            ["roadsBaseColorTexturePath"] = "roadsBaseColorTexturePath",
                            ["waterBaseColorTexturePath"] = "waterBaseColorTexturePath",
                            ["terrainBaseColorTexturePath"] = "terrainBaseColorTexturePath"
                        },
                        OutputMapping = new Dictionary<string, string> { ["materialsJsonText"] = "jsonText" }
                    },

                    // manifest.json (artifacts are passed as input computed in CLI)
                    new BehaviorStep
                    {
                        Id = "manifest_json",
                        BrickId = "geoworld.export.manifest-json",
                        Implementation = ImplementationType.Deterministic,
                        InputMapping = new Dictionary<string, string>
                        {
                            ["bounds"] = "bounds",
                            ["origin"] = "origin",
                            ["metersPerUnit"] = "metersPerUnit",
                            ["artifacts"] = "manifestArtifacts",
                            ["providerProvenance"] = "providerProvenance"
                        },
                        OutputMapping = new Dictionary<string, string> { ["manifestJsonText"] = "jsonText" }
                    }
                ]
            };

            var agent = new AgentCard { Id = "geoworld.cli", Name = "GeoWorld CLI", Behaviors = [behavior.Id] };

            var execOptions = new ExecutionOptions
            {
                Provider = "offline",
                IsAirGapped = airGapped,
                ImplementationMode = airGapped ? ImplementationMode.DeterministicOnly : ImplementationMode.Auto,
                SwapOnFailure = true
            };

            // Prepare output layout.
            var root = outDir.FullName;
            DirectoryOps.CreateDirectory(root);
            var exportSceneGltf = string.Equals(meshFormat?.Trim(), "gltf-scene", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(meshFormat?.Trim(), "glb", StringComparison.OrdinalIgnoreCase);
            var exportGlb = string.Equals(meshFormat?.Trim(), "glb", StringComparison.OrdinalIgnoreCase);
            var useGltf = string.Equals(meshFormat?.Trim(), "gltf", StringComparison.OrdinalIgnoreCase) || exportSceneGltf || exportGlb;

            var terrainRel = useGltf ? "terrain.gltf" : "terrain.obj";
            var terrainBinRel = useGltf ? "terrain.bin" : null;
            var terrainTextureRel = "terrain_texture.png";
            var buildingsTextureRel = "textures/buildings.png";
            var roadsTextureRel = "textures/roads.png";
            var waterTextureRel = "textures/water.png";
            var worldMtlRel = "world.mtl";
            var buildingsRel = useGltf ? "buildings.gltf" : "buildings.obj";
            var buildingsBinRel = useGltf ? "buildings.bin" : null;
            var roadsRel = useGltf ? "roads.gltf" : "roads.obj";
            var roadsBinRel = useGltf ? "roads.bin" : null;
            var waterRel = useGltf ? "water.gltf" : "water.obj";
            var waterBinRel = useGltf ? "water.bin" : null;
            var instancesRel = "instances.json";
            var materialsRel = "materials.json";
            var manifestRel = "manifest.json";
            var unityRel = "unity/IMPORT_INSTRUCTIONS.txt";

            var artifacts = useGltf
                ? new List<WorldArtifact>
                {
                    new WorldArtifact { Kind = "terrain_gltf", Path = terrainRel, ContentType = "gltf" },
                    new WorldArtifact { Kind = "terrain_bin", Path = terrainBinRel!, ContentType = "bin" },
                    new WorldArtifact { Kind = "buildings_gltf", Path = buildingsRel, ContentType = "gltf" },
                    new WorldArtifact { Kind = "buildings_bin", Path = buildingsBinRel!, ContentType = "bin" },
                    new WorldArtifact { Kind = "roads_gltf", Path = roadsRel, ContentType = "gltf" },
                    new WorldArtifact { Kind = "roads_bin", Path = roadsBinRel!, ContentType = "bin" },
                    new WorldArtifact { Kind = "water_gltf", Path = waterRel, ContentType = "gltf" },
                    new WorldArtifact { Kind = "water_bin", Path = waterBinRel!, ContentType = "bin" },
                    new WorldArtifact { Kind = "instances", Path = instancesRel, ContentType = "json" },
                    new WorldArtifact { Kind = "materials", Path = materialsRel, ContentType = "json" },
                    new WorldArtifact { Kind = "manifest", Path = manifestRel, ContentType = "json" }
                }
                : new List<WorldArtifact>
                {
                    new WorldArtifact { Kind = "terrain", Path = terrainRel, ContentType = "obj" },
                    new WorldArtifact { Kind = "world_mtl", Path = worldMtlRel, ContentType = "mtl" },
                    new WorldArtifact { Kind = "buildings", Path = buildingsRel, ContentType = "obj" },
                    new WorldArtifact { Kind = "roads", Path = roadsRel, ContentType = "obj" },
                    new WorldArtifact { Kind = "water", Path = waterRel, ContentType = "obj" },
                    new WorldArtifact { Kind = "instances", Path = instancesRel, ContentType = "json" },
                    new WorldArtifact { Kind = "materials", Path = materialsRel, ContentType = "json" },
                    new WorldArtifact { Kind = "manifest", Path = manifestRel, ContentType = "json" },
                    new WorldArtifact { Kind = "unity", Path = unityRel, ContentType = "text" }
                };

            // Scene exports (extra artifacts; currently only supported for non-chunked terrain).
            if (exportSceneGltf && terrainChunkSamples <= 0)
            {
                artifacts.Add(new WorldArtifact { Kind = "world_scene_gltf", Path = "world.gltf", ContentType = "gltf" });
                artifacts.Add(new WorldArtifact { Kind = "world_scene_bin", Path = "world.bin", ContentType = "bin" });
            }
            if (exportGlb && terrainChunkSamples <= 0)
            {
                artifacts.Add(new WorldArtifact { Kind = "world_glb", Path = "world.glb", ContentType = "glb" });
            }

            var provenance = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["terrainProvider"] = terrainElevationProvider,
                ["vectorProvider"] = vectorProvider
            };

            var inputData = new Dictionary<string, object>
            {
                ["bounds"] = geoBounds,
                ["origin"] = origin,
                ["coordinateFrame"] = new WorldCoordinateFrame
                {
                    ProjectionId = projector.ProjectionId,
                    Parameters = projector.Parameters
                },
                ["buildingsKind"] = "building",
                ["roadsKind"] = "road",
                ["waterKind"] = "water",
                ["vegetationKind"] = "vegetation",
                ["alignBuildingsToTerrain"] = true,
                ["terrainTreatNoDataAsZero"] = true,
                ["buildingsGenerateUv"] = true,
                ["buildingsUvMetersPerRepeat"] = 1.0f,
                ["buildingsBaseColorTexturePath"] = generateVectorTextures ? buildingsTextureRel : "",
                ["roadsWidthMeters"] = 4.0f,
                ["roadsGenerateUv"] = true,
                ["roadsUvMetersPerRepeat"] = 1.0f,
                ["roadsBaseColorTexturePath"] = generateVectorTextures ? roadsTextureRel : "",
                ["conformRoadsToTerrain"] = false,
                ["waterGenerateUv"] = true,
                ["waterUvMetersPerRepeat"] = 5.0f,
                ["waterBaseColorTexturePath"] = generateVectorTextures ? waterTextureRel : "",
                ["terrainBaseColorTexturePath"] = (enableTerrainImagery && !airGapped) ? terrainTextureRel : "",
                ["conformWaterToTerrain"] = false,
                ["flattenWaterToTerrain"] = waterFlattenToTerrain,
                ["waterSurfaceOffsetMeters"] = 0.0f,
                ["vegSeed"] = 12345,
                ["vegDensity"] = 250.0f,
                ["vegSpacing"] = 2.0f,
                ["vegMaxSlope"] = 35.0f,
                ["vegKind"] = "tree",
                ["carveWaterEnabled"] = true,
                ["carveWaterSmoothIters"] = 2,
                ["carveWaterSmoothBlend"] = 0.5f,
                ["carveWaterOffsetMeters"] = 0.0f,
                ["metersPerUnit"] = 1.0f,
                ["manifestArtifacts"] = artifacts.ToArray(),
                ["providerProvenance"] = provenance
            };

            var input = new BehaviorInput(inputData);

            IReadOnlyDictionary<string, object> outputs = new Dictionary<string, object>();
            var steps = new List<object>();
            var errors = new List<string>();

            await foreach (var evt in exec.ExecuteWithEventsAsync(agent, behavior, input, execOptions, ct))
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

            var terrainMeshObj = outputs.TryGetValue("terrainMesh", out var tm) ? tm as MeshData : null;
            var buildingsMeshObj = outputs.TryGetValue("buildingsMesh", out var bm) ? bm as MeshData : null;
            var roadsMeshObj = outputs.TryGetValue("roadsMesh", out var rm) ? rm as MeshData : null;
            var waterMeshObj = outputs.TryGetValue("waterMesh", out var wm) ? wm as MeshData : null;
            var instancesJson = outputs.TryGetValue("instancesJsonText", out var i0) ? i0 as string : null;
            var materialsJson = outputs.TryGetValue("materialsJsonText", out var m0) ? m0 as string : null;
            var manifestJson = outputs.TryGetValue("manifestJsonText", out var mj) ? mj as string : null;

            if (terrainMeshObj is null ||
                buildingsMeshObj is null ||
                roadsMeshObj is null ||
                waterMeshObj is null ||
                string.IsNullOrWhiteSpace(instancesJson) ||
                string.IsNullOrWhiteSpace(materialsJson) ||
                string.IsNullOrWhiteSpace(manifestJson))
            {
                throw new InvalidOperationException("World build did not produce all expected artifacts.");
            }

            // Write files.
            var manifestNeedsRegen = false;
            string? terrainTextureForMtl = null;
            string? buildingsTextureForMtl = null;
            string? roadsTextureForMtl = null;
            string? waterTextureForMtl = null;
            MeshData? terrainMeshForScene = null;

            // Establish a single shared local coordinate frame:
            // - Vector features are projected around `origin` (bounds center).
            // - Terrain grid is sampled in bounds space; we translate terrain+instances so the origin aligns at (0,0).
            if (!outputs.TryGetValue("terrainGrid", out var tg) || tg is not ElevationGrid terrainGrid)
                throw new InvalidOperationException("terrainGrid output was not available.");
            var terrainOriginOffset = ComputeGridOriginOffsetMeters(terrainGrid, origin);

            if (!outputs.TryGetValue("vegInstances", out var viAny))
                throw new InvalidOperationException("vegInstances output was not available.");
            var vegInstancesRaw = viAny as SceneryInstance[] ?? (viAny as IReadOnlyList<SceneryInstance>)?.ToArray();
            if (vegInstancesRaw is null)
                throw new InvalidOperationException("vegInstances had an unexpected type (expected SceneryInstance[]).");
            var vegInstances = TranslateInstances(vegInstancesRaw, -terrainOriginOffset.OffsetX, -terrainOriginOffset.OffsetZ);

            if (terrainChunkSamples <= 0)
            {
                // Export single terrain mesh (OBJ or glTF), with optional imagery UVs.
                var centered = TranslateMesh(terrainMeshObj, -terrainOriginOffset.OffsetX, -terrainOriginOffset.OffsetZ);
                terrainMeshForScene = centered;
                RasterMosaic? mosaic = null;

                if (enableTerrainImagery && !airGapped)
                {
                    var token = MapboxTokenResolver.Resolve(mapboxAccessToken);
                    var z = terrainImageryZoom ?? mapboxZoom ?? 15;
                    var tileset = string.IsNullOrWhiteSpace(terrainImageryTileset) ? "mapbox.satellite" : terrainImageryTileset!.Trim();
                    var fmt = string.IsNullOrWhiteSpace(terrainImageryFormat) ? "jpg90" : terrainImageryFormat!.Trim();

                    var dl = new MapboxRasterTileDownloader(_httpClientFactory.CreateClient("geovector.mapbox"), token, _loggerFactory.CreateLogger<MapboxRasterTileDownloader>());
                    var mosaicBuilder = new MapboxRasterMosaicBuilder(dl, _loggerFactory.CreateLogger<MapboxRasterMosaicBuilder>());
                    mosaic = await mosaicBuilder.BuildAsync(geoBounds, z, tileset, fmt, ct);

                    artifacts.Add(new WorldArtifact { Kind = "terrain_texture", Path = terrainTextureRel, ContentType = "png" });
                    manifestNeedsRegen = true;

                    await BinaryFile.WriteAllBytesAsync(Path.Combine(root, terrainTextureRel), mosaic.PngBytes, ct);
                    terrainTextureForMtl = terrainTextureRel;
                    inputData["terrainBaseColorTexturePath"] = terrainTextureRel;

                    centered = AddTerrainTexCoords(centered, terrainGrid, geoBounds, mosaic);
                }

                if (useGltf)
                {
                    var texUri = mosaic != null ? RelativeUriForFile(terrainRel, terrainTextureRel) : null;
                    var res = GltfMeshWriter.Write(
                        centered,
                        binUri: new Uri(Path.GetFileName(terrainBinRel!), UriKind.Relative),
                        new GltfMaterialSpec
                        {
                            Name = "terrain",
                            BaseColorTextureUri = texUri != null ? new Uri(texUri, UriKind.Relative) : null
                        });
                    await TextFile.WriteAllTextAsync(Path.Combine(root, terrainRel), res.GltfJson, ct);
                    await BinaryFile.WriteAllBytesAsync(Path.Combine(root, terrainBinRel!), res.BinBytes.Span.ToArray(), ct);
                }
                else
                {
                    var objText = InjectObjMaterial(ObjMeshWriter.Write(centered), worldMtlRel, "terrain");
                    await TextFile.WriteAllTextAsync(Path.Combine(root, terrainRel), objText, ct);
                }

                // Populate base terrain artifact metadata.
                UpsertArtifactMetadata(
                    artifacts,
                    kind: useGltf ? "terrain_gltf" : "terrain",
                    path: terrainRel,
                    centered,
                    lodLevel: null,
                    gridChunk: null);

                // Triangle-budget LODs (true decimation) for terrain mesh.
                var triBudgets = ParseIntList(lodTriBudgets);
                if (triBudgets.Count > 0)
                {
                    var lodDirRel = "mesh_lods";
                    DirectoryOps.CreateDirectory(Path.Combine(root, lodDirRel));

                    for (var i = 0; i < triBudgets.Count; i++)
                    {
                        var budget = triBudgets[i];
                        if (budget <= 0) continue;

                        var simplified = MeshDecimator.SimplifyToTargetTriangles(centered, budget);
                        var outName = $"terrain_tri{budget}";
                        if (useGltf)
                        {
                            var rel = $"{lodDirRel}/{outName}.gltf";
                            var binRel = $"{lodDirRel}/{outName}.bin";
                            var texUri = mosaic != null ? RelativeUriForFile(rel, terrainTextureRel) : null;
                            var res = GltfMeshWriter.Write(
                                simplified,
                                binUri: new Uri(Path.GetFileName(binRel), UriKind.Relative),
                                new GltfMaterialSpec { Name = outName, BaseColorTextureUri = texUri != null ? new Uri(texUri, UriKind.Relative) : null });
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), res.GltfJson, ct);
                            await BinaryFile.WriteAllBytesAsync(Path.Combine(root, binRel), res.BinBytes.Span.ToArray(), ct);
                            artifacts.Add(new WorldArtifact { Kind = $"terrain_tri{budget}_gltf", Path = rel, ContentType = "gltf", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: null) });
                            artifacts.Add(new WorldArtifact { Kind = $"terrain_tri{budget}_bin", Path = binRel, ContentType = "bin" });
                        }
                        else
                        {
                            var rel = $"{lodDirRel}/{outName}.obj";
                            var obj = InjectObjMaterial(ObjMeshWriter.Write(simplified), worldMtlRel, "terrain");
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), obj, ct);
                            artifacts.Add(new WorldArtifact { Kind = $"terrain_tri{budget}", Path = rel, ContentType = "obj", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: null) });
                        }
                        manifestNeedsRegen = true;
                    }
                }

                var lodFactors = ParseIntList(terrainLodFactors);
                if (lodFactors.Count > 0)
                {
                    var lodDirRel = "terrain_lods";
                    DirectoryOps.CreateDirectory(Path.Combine(root, lodDirRel));

                    for (var i = 0; i < lodFactors.Count; i++)
                    {
                        var factor = lodFactors[i];
                        if (factor <= 1) continue;
                        var lodGrid = ElevationGridLodBuilder.BuildLod(terrainGrid, factor);
                        var mesh = GridMeshGenerator.Generate(lodGrid, new MeshGenerationOptions { GenerateNormals = true, TreatNoDataAsZero = true });
                        var lodOffset = ComputeGridOriginOffsetMeters(lodGrid, origin);
                        mesh = TranslateMesh(mesh, -lodOffset.OffsetX, -lodOffset.OffsetZ);
                        if (mosaic != null)
                        {
                            mesh = AddTerrainTexCoords(mesh, lodGrid, geoBounds, mosaic);
                        }

                        if (useGltf)
                        {
                            var rel = $"{lodDirRel}/terrain_lod{factor}.gltf";
                            var binRel = $"{lodDirRel}/terrain_lod{factor}.bin";
                            var texUri = mosaic != null ? RelativeUriForFile(rel, terrainTextureRel) : null;
                            var res = GltfMeshWriter.Write(
                                mesh,
                                binUri: new Uri(Path.GetFileName(binRel), UriKind.Relative),
                                new GltfMaterialSpec { Name = $"terrain_lod{factor}", BaseColorTextureUri = texUri != null ? new Uri(texUri, UriKind.Relative) : null });
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), res.GltfJson, ct);
                            await BinaryFile.WriteAllBytesAsync(Path.Combine(root, binRel), res.BinBytes.Span.ToArray(), ct);
                            artifacts.Add(new WorldArtifact { Kind = $"terrain_lod{factor}_gltf", Path = rel, ContentType = "gltf" });
                            artifacts.Add(new WorldArtifact { Kind = $"terrain_lod{factor}_bin", Path = binRel, ContentType = "bin" });

                            artifacts[artifacts.Count - 2] = artifacts[artifacts.Count - 2] with
                            {
                                Metadata = BuildMeshMetadata(mesh, lodLevel: factor, gridChunk: null)
                            };
                        }
                        else
                        {
                            var obj = ObjMeshWriter.Write(mesh);
                            var rel = $"{lodDirRel}/terrain_lod{factor}.obj";
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), obj, ct);
                            artifacts.Add(new WorldArtifact { Kind = $"terrain_lod{factor}", Path = rel, ContentType = "obj" });

                            artifacts[artifacts.Count - 1] = artifacts[artifacts.Count - 1] with
                            {
                                Metadata = BuildMeshMetadata(mesh, lodLevel: factor, gridChunk: null)
                            };
                        }
                    }

                    if (lodFactors.Any(f => f > 1))
                        manifestNeedsRegen = true;
                }
            }
            else
            {
                // Chunk terrain mesh for large areas (basic hook).
                var chunkDirRel = "terrain_chunks";
                var chunkDir = Path.Combine(root, chunkDirRel);
                DirectoryOps.CreateDirectory(chunkDir);

                var chunks = BuildGridChunks(terrainGrid.Width, terrainGrid.Height, terrainChunkSamples);

                // Compute grid-space normals once so chunk seams are consistent.
                var normalsAll = GridNormalCalculator.ComputeNormals(terrainGrid, verticalScale: 1.0f, treatNoDataAsZero: true);

                RasterMosaic? mosaic = null;
                if (enableTerrainImagery && !airGapped)
                {
                    var token = MapboxTokenResolver.Resolve(mapboxAccessToken);
                    var z = terrainImageryZoom ?? mapboxZoom ?? 15;
                    var tileset = string.IsNullOrWhiteSpace(terrainImageryTileset) ? "mapbox.satellite" : terrainImageryTileset!.Trim();
                    var fmt = string.IsNullOrWhiteSpace(terrainImageryFormat) ? "jpg90" : terrainImageryFormat!.Trim();

                    var dl = new MapboxRasterTileDownloader(_httpClientFactory.CreateClient("geovector.mapbox"), token, _loggerFactory.CreateLogger<MapboxRasterTileDownloader>());
                    var mosaicBuilder = new MapboxRasterMosaicBuilder(dl, _loggerFactory.CreateLogger<MapboxRasterMosaicBuilder>());
                    mosaic = await mosaicBuilder.BuildAsync(geoBounds, z, tileset, fmt, ct);

                    artifacts.Add(new WorldArtifact { Kind = "terrain_texture", Path = terrainTextureRel, ContentType = "png" });
                    terrainTextureForMtl = terrainTextureRel;
                    manifestNeedsRegen = true;

                    await BinaryFile.WriteAllBytesAsync(Path.Combine(root, terrainTextureRel), mosaic.PngBytes, ct);
                    inputData["terrainBaseColorTexturePath"] = terrainTextureRel;
                }

                await _loopKernel.ForEachAsync(
                    chunks,
                    async (c, _, ct2) =>
                    {
                        var sub = SliceGrid(terrainGrid, c.x0, c.y0, c.w, c.h);
                        var mesh = GridMeshGenerator.Generate(sub, new MeshGenerationOptions { GenerateNormals = false, TreatNoDataAsZero = true });
                        mesh = mesh with { Normals = SliceNormals(normalsAll, terrainGrid.Width, c.x0, c.y0, c.w, c.h) };
                        if (mosaic != null)
                        {
                            mesh = AddTerrainTexCoordsChunk(mesh, terrainGrid, geoBounds, mosaic, c.x0, c.y0, c.w, c.h);
                        }
                        mesh = OffsetMesh(mesh, (float)(c.x0 * terrainGrid.Spacing.MetersX), (float)(c.y0 * terrainGrid.Spacing.MetersY));
                        mesh = TranslateMesh(mesh, -terrainOriginOffset.OffsetX, -terrainOriginOffset.OffsetZ);
                        if (useGltf)
                        {
                            var rel = $"{chunkDirRel}/terrain_{c.x0}_{c.y0}.gltf";
                            var binRel = $"{chunkDirRel}/terrain_{c.x0}_{c.y0}.bin";
                            var texUri = mosaic != null ? RelativeUriForFile(rel, terrainTextureRel) : null;
                            var res = GltfMeshWriter.Write(
                                mesh,
                                binUri: new Uri(Path.GetFileName(binRel), UriKind.Relative),
                                new GltfMaterialSpec { Name = "terrain", BaseColorTextureUri = texUri != null ? new Uri(texUri, UriKind.Relative) : null });
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), res.GltfJson, ct2);
                            await BinaryFile.WriteAllBytesAsync(Path.Combine(root, binRel), res.BinBytes.Span.ToArray(), ct2);

                            artifacts.Add(new WorldArtifact
                            {
                                Kind = "terrain_chunk_gltf",
                                Path = rel,
                                ContentType = "gltf",
                                Metadata = BuildMeshMetadata(mesh, lodLevel: null, gridChunk: new WorldGridChunkRef { X0 = c.x0, Y0 = c.y0, Width = c.w, Height = c.h })
                            });
                            artifacts.Add(new WorldArtifact { Kind = "terrain_chunk_bin", Path = binRel, ContentType = "bin" });
                        }
                        else
                        {
                            var obj = InjectObjMaterial(ObjMeshWriter.Write(mesh), worldMtlRel, "terrain");
                            var rel = $"{chunkDirRel}/terrain_{c.x0}_{c.y0}.obj";
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), obj, ct2);

                            artifacts.Add(new WorldArtifact
                            {
                                Kind = "terrain_chunk",
                                Path = rel,
                                ContentType = "obj",
                                Metadata = BuildMeshMetadata(mesh, lodLevel: null, gridChunk: new WorldGridChunkRef { X0 = c.x0, Y0 = c.y0, Width = c.w, Height = c.h })
                            });
                        }

                        // Optional: triangle-budget LODs per chunk (budget applies per chunk).
                        var triBudgets = ParseIntList(lodTriBudgets);
                        if (triBudgets.Count > 0)
                        {
                            for (var bi = 0; bi < triBudgets.Count; bi++)
                            {
                                var budget = triBudgets[bi];
                                if (budget <= 0) continue;
                                if (mesh.Indices.Count / 3 <= budget) continue;

                                var simplified = MeshDecimator.SimplifyToTargetTriangles(mesh, budget);
                                var subDir = $"terrain_chunks_tri/tri{budget}";
                                DirectoryOps.CreateDirectory(Path.Combine(root, subDir));

                                if (useGltf)
                                {
                                    var rel = $"{subDir}/terrain_{c.x0}_{c.y0}.gltf";
                                    var binRel = $"{subDir}/terrain_{c.x0}_{c.y0}.bin";
                                    var texUri = mosaic != null ? RelativeUriForFile(rel, terrainTextureRel) : null;
                                    var res = GltfMeshWriter.Write(
                                        simplified,
                                        binUri: new Uri(Path.GetFileName(binRel), UriKind.Relative),
                                        new GltfMaterialSpec { Name = $"terrain_tri{budget}", BaseColorTextureUri = texUri != null ? new Uri(texUri, UriKind.Relative) : null });
                                    await TextFile.WriteAllTextAsync(Path.Combine(root, rel), res.GltfJson, ct2);
                                    await BinaryFile.WriteAllBytesAsync(Path.Combine(root, binRel), res.BinBytes.Span.ToArray(), ct2);
                                    artifacts.Add(new WorldArtifact { Kind = $"terrain_chunk_tri{budget}_gltf", Path = rel, ContentType = "gltf", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: new WorldGridChunkRef { X0 = c.x0, Y0 = c.y0, Width = c.w, Height = c.h }) });
                                    artifacts.Add(new WorldArtifact { Kind = $"terrain_chunk_tri{budget}_bin", Path = binRel, ContentType = "bin" });
                                }
                                else
                                {
                                    var rel = $"{subDir}/terrain_{c.x0}_{c.y0}.obj";
                                    var obj2 = InjectObjMaterial(ObjMeshWriter.Write(simplified), worldMtlRel, "terrain");
                                    await TextFile.WriteAllTextAsync(Path.Combine(root, rel), obj2, ct2);
                                    artifacts.Add(new WorldArtifact { Kind = $"terrain_chunk_tri{budget}", Path = rel, ContentType = "obj", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: new WorldGridChunkRef { X0 = c.x0, Y0 = c.y0, Width = c.w, Height = c.h }) });
                                }
                                manifestNeedsRegen = true;
                            }
                        }
                        return LoopAction.Continue;
                    },
                    new LoopOptions { Name = "terrain-chunk-export" },
                    ct);

                // Replace single terrain artifact with chunked artifacts.
                if (useGltf)
                {
                    artifacts.RemoveAll(a =>
                        string.Equals(a.Kind, "terrain_gltf", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a.Kind, "terrain_bin", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    artifacts.RemoveAll(a => string.Equals(a.Kind, "terrain", StringComparison.OrdinalIgnoreCase));
                }
                manifestNeedsRegen = true;
                // Note: chunk artifacts are added during export loop (with metadata).

                // Optional: also generate LOD chunks when factors are provided.
                var lodFactors = ParseIntList(terrainLodFactors);
                if (lodFactors.Count > 0)
                {
                    for (var lf = 0; lf < lodFactors.Count; lf++)
                    {
                        var factor = lodFactors[lf];
                        if (factor <= 1) continue;

                        var lodGrid = ElevationGridLodBuilder.BuildLod(terrainGrid, factor);
                        var lodNormals = GridNormalCalculator.ComputeNormals(lodGrid, verticalScale: 1.0f, treatNoDataAsZero: true);
                        var lodOffset = ComputeGridOriginOffsetMeters(lodGrid, origin);

                        var lodDirRel = $"terrain_chunks_lods/lod{factor}";
                        DirectoryOps.CreateDirectory(Path.Combine(root, lodDirRel));

                        var lodChunks = BuildGridChunks(lodGrid.Width, lodGrid.Height, terrainChunkSamples);
                        for (var i = 0; i < lodChunks.Count; i++)
                        {
                            var c = lodChunks[i];
                            var sub = SliceGrid(lodGrid, c.x0, c.y0, c.w, c.h);
                            var mesh = GridMeshGenerator.Generate(sub, new MeshGenerationOptions { GenerateNormals = false, TreatNoDataAsZero = true });
                            mesh = mesh with { Normals = SliceNormals(lodNormals, lodGrid.Width, c.x0, c.y0, c.w, c.h) };
                            if (mosaic != null)
                            {
                                mesh = AddTerrainTexCoordsChunk(mesh, lodGrid, geoBounds, mosaic, c.x0, c.y0, c.w, c.h);
                            }
                            mesh = OffsetMesh(mesh, (float)(c.x0 * lodGrid.Spacing.MetersX), (float)(c.y0 * lodGrid.Spacing.MetersY));
                            mesh = TranslateMesh(mesh, -lodOffset.OffsetX, -lodOffset.OffsetZ);
                            if (useGltf)
                            {
                                var rel = $"{lodDirRel}/terrain_lod{factor}_{c.x0}_{c.y0}.gltf";
                                var binRel = $"{lodDirRel}/terrain_lod{factor}_{c.x0}_{c.y0}.bin";
                                var texUri = mosaic != null ? RelativeUriForFile(rel, terrainTextureRel) : null;
                                var res = GltfMeshWriter.Write(
                                    mesh,
                                    binUri: new Uri(Path.GetFileName(binRel), UriKind.Relative),
                                    new GltfMaterialSpec { Name = $"terrain_lod{factor}", BaseColorTextureUri = texUri != null ? new Uri(texUri, UriKind.Relative) : null });
                                await TextFile.WriteAllTextAsync(Path.Combine(root, rel), res.GltfJson, ct);
                                await BinaryFile.WriteAllBytesAsync(Path.Combine(root, binRel), res.BinBytes.Span.ToArray(), ct);
                                artifacts.Add(new WorldArtifact
                                {
                                    Kind = $"terrain_chunk_lod{factor}_gltf",
                                    Path = rel,
                                    ContentType = "gltf",
                                    Metadata = BuildMeshMetadata(mesh, lodLevel: factor, gridChunk: new WorldGridChunkRef { X0 = c.x0, Y0 = c.y0, Width = c.w, Height = c.h })
                                });
                                artifacts.Add(new WorldArtifact { Kind = $"terrain_chunk_lod{factor}_bin", Path = binRel, ContentType = "bin" });
                            }
                            else
                            {
                                var obj = InjectObjMaterial(ObjMeshWriter.Write(mesh), worldMtlRel, "terrain");
                                var rel = $"{lodDirRel}/terrain_lod{factor}_{c.x0}_{c.y0}.obj";
                                await TextFile.WriteAllTextAsync(Path.Combine(root, rel), obj, ct);
                                artifacts.Add(new WorldArtifact
                                {
                                    Kind = $"terrain_chunk_lod{factor}",
                                    Path = rel,
                                    ContentType = "obj",
                                    Metadata = BuildMeshMetadata(mesh, lodLevel: factor, gridChunk: new WorldGridChunkRef { X0 = c.x0, Y0 = c.y0, Width = c.w, Height = c.h })
                                });
                            }
                        }

                        manifestNeedsRegen = true;
                    }
                }
            }

            // Optional: generate simple procedural textures for vector meshes.
            if (generateVectorTextures)
            {
                DirectoryOps.EnsureParentDirectoryExists(Path.Combine(root, buildingsTextureRel));
                await BinaryFile.WriteAllBytesAsync(Path.Combine(root, buildingsTextureRel),
                    ProceduralTextureGenerator.MakeCheckerPng(256, 180, 180, 180, 255, 140, 140, 140, 255, 16),
                    ct);
                await BinaryFile.WriteAllBytesAsync(Path.Combine(root, roadsTextureRel), ProceduralTextureGenerator.MakeRoadAsphaltPng(256), ct);
                await BinaryFile.WriteAllBytesAsync(Path.Combine(root, waterTextureRel),
                    ProceduralTextureGenerator.MakeCheckerPng(256, 40, 90, 180, 200, 20, 60, 140, 200, 16),
                    ct);

                buildingsTextureForMtl = buildingsTextureRel;
                roadsTextureForMtl = roadsTextureRel;
                waterTextureForMtl = waterTextureRel;

                artifacts.Add(new WorldArtifact { Kind = "buildings_texture", Path = buildingsTextureRel, ContentType = "png" });
                artifacts.Add(new WorldArtifact { Kind = "roads_texture", Path = roadsTextureRel, ContentType = "png" });
                artifacts.Add(new WorldArtifact { Kind = "water_texture", Path = waterTextureRel, ContentType = "png" });
                manifestNeedsRegen = true;
            }

            // Optional: scene-level glTF / GLB export (non-chunked terrain only).
            if (exportSceneGltf && terrainChunkSamples <= 0 && terrainMeshForScene != null)
            {
                var nodes = new List<GltfSceneNode>(capacity: 4)
                {
                    new GltfSceneNode
                    {
                        Name = "terrain",
                        Mesh = terrainMeshForScene,
                        Material = new GltfMaterialSpec
                        {
                            Name = "terrain",
                            BaseColorTextureUri = !string.IsNullOrWhiteSpace(terrainTextureForMtl)
                                ? new Uri(RelativeUriForFile("world.gltf", terrainTextureForMtl), UriKind.Relative)
                                : null
                        }
                    },
                    new GltfSceneNode
                    {
                        Name = "buildings",
                        Mesh = buildingsMeshObj,
                        Material = new GltfMaterialSpec
                        {
                            Name = "buildings",
                            BaseColorR = 0.8f,
                            BaseColorG = 0.8f,
                            BaseColorB = 0.8f,
                            BaseColorTextureUri = !string.IsNullOrWhiteSpace(buildingsTextureForMtl)
                                ? new Uri(RelativeUriForFile("world.gltf", buildingsTextureForMtl), UriKind.Relative)
                                : null
                        }
                    },
                    new GltfSceneNode
                    {
                        Name = "roads",
                        Mesh = roadsMeshObj,
                        Material = new GltfMaterialSpec
                        {
                            Name = "roads",
                            BaseColorR = 0.15f,
                            BaseColorG = 0.15f,
                            BaseColorB = 0.15f,
                            BaseColorTextureUri = !string.IsNullOrWhiteSpace(roadsTextureForMtl)
                                ? new Uri(RelativeUriForFile("world.gltf", roadsTextureForMtl), UriKind.Relative)
                                : null
                        }
                    },
                    new GltfSceneNode
                    {
                        Name = "water",
                        Mesh = waterMeshObj,
                        Material = new GltfMaterialSpec
                        {
                            Name = "water",
                            BaseColorR = 0.1f,
                            BaseColorG = 0.3f,
                            BaseColorB = 0.7f,
                            DoubleSided = true,
                            BaseColorTextureUri = !string.IsNullOrWhiteSpace(waterTextureForMtl)
                                ? new Uri(RelativeUriForFile("world.gltf", waterTextureForMtl), UriKind.Relative)
                                : null
                        }
                    }
                };

                var scene = GltfSceneWriter.Write(nodes, binUri: new Uri("world.bin", UriKind.Relative));
                await TextFile.WriteAllTextAsync(Path.Combine(root, "world.gltf"), scene.GltfJson, ct);
                await BinaryFile.WriteAllBytesAsync(Path.Combine(root, "world.bin"), scene.BinBytes.Span.ToArray(), ct);
                manifestNeedsRegen = true;

                if (exportGlb)
                {
                    var glb = GlbWriter.Write(scene.GltfJson, scene.BinBytes);
                    await BinaryFile.WriteAllBytesAsync(Path.Combine(root, "world.glb"), glb, ct);
                    manifestNeedsRegen = true;
                }
            }

            if (useGltf)
            {
                // Vector + water as glTF.
                var buildingsRes = GltfMeshWriter.Write(
                    buildingsMeshObj,
                    binUri: new Uri(Path.GetFileName(buildingsBinRel!), UriKind.Relative),
                    new GltfMaterialSpec { Name = "buildings", BaseColorR = 0.8f, BaseColorG = 0.8f, BaseColorB = 0.8f });
                await TextFile.WriteAllTextAsync(Path.Combine(root, buildingsRel), buildingsRes.GltfJson, ct);
                await BinaryFile.WriteAllBytesAsync(Path.Combine(root, buildingsBinRel!), buildingsRes.BinBytes.Span.ToArray(), ct);

                var roadsRes = GltfMeshWriter.Write(
                    roadsMeshObj,
                    binUri: new Uri(Path.GetFileName(roadsBinRel!), UriKind.Relative),
                    new GltfMaterialSpec { Name = "roads", BaseColorR = 0.15f, BaseColorG = 0.15f, BaseColorB = 0.15f });
                await TextFile.WriteAllTextAsync(Path.Combine(root, roadsRel), roadsRes.GltfJson, ct);
                await BinaryFile.WriteAllBytesAsync(Path.Combine(root, roadsBinRel!), roadsRes.BinBytes.Span.ToArray(), ct);

                var waterRes = GltfMeshWriter.Write(
                    waterMeshObj,
                    binUri: new Uri(Path.GetFileName(waterBinRel!), UriKind.Relative),
                    new GltfMaterialSpec { Name = "water", BaseColorR = 0.1f, BaseColorG = 0.3f, BaseColorB = 0.7f, DoubleSided = true });
                await TextFile.WriteAllTextAsync(Path.Combine(root, waterRel), waterRes.GltfJson, ct);
                await BinaryFile.WriteAllBytesAsync(Path.Combine(root, waterBinRel!), waterRes.BinBytes.Span.ToArray(), ct);

                UpsertArtifactMetadata(artifacts, "buildings_gltf", buildingsRel, buildingsMeshObj, null, null);
                UpsertArtifactMetadata(artifacts, "roads_gltf", roadsRel, roadsMeshObj, null, null);
                UpsertArtifactMetadata(artifacts, "water_gltf", waterRel, waterMeshObj, null, null);
            }
            else
            {
                // Write world MTL once (includes optional terrain texture).
                await TextFile.WriteAllTextAsync(Path.Combine(root, worldMtlRel), BuildWorldMtl(terrainTextureForMtl, buildingsTextureForMtl, roadsTextureForMtl, waterTextureForMtl), ct);

                // Inject mtllib/usemtl into vector OBJs so material assignment works even without Unity libraries.
                await TextFile.WriteAllTextAsync(Path.Combine(root, buildingsRel), InjectObjMaterial(ObjMeshWriter.Write(buildingsMeshObj), worldMtlRel, "building_residential"), ct);
                await TextFile.WriteAllTextAsync(Path.Combine(root, roadsRel), InjectObjMaterial(ObjMeshWriter.Write(roadsMeshObj), worldMtlRel, "road_asphalt"), ct);
                await TextFile.WriteAllTextAsync(Path.Combine(root, waterRel), InjectObjMaterial(ObjMeshWriter.Write(waterMeshObj), worldMtlRel, "water"), ct);

                UpsertArtifactMetadata(artifacts, "buildings", buildingsRel, buildingsMeshObj, null, null);
                UpsertArtifactMetadata(artifacts, "roads", roadsRel, roadsMeshObj, null, null);
                UpsertArtifactMetadata(artifacts, "water", waterRel, waterMeshObj, null, null);
            }

            // Triangle-budget LODs for vector meshes (buildings/roads/water).
            var vecBudgets = ParseIntList(lodTriBudgets);
            if (vecBudgets.Count > 0)
            {
                var lodDirRel = "mesh_lods";
                DirectoryOps.CreateDirectory(Path.Combine(root, lodDirRel));

                for (var i = 0; i < vecBudgets.Count; i++)
                {
                    var budget = vecBudgets[i];
                    if (budget <= 0) continue;

                    // Buildings
                    if (buildingsMeshObj.Indices.Count / 3 > budget)
                    {
                        var simplified = MeshDecimator.SimplifyToTargetTriangles(buildingsMeshObj, budget);
                        var outName = $"buildings_tri{budget}";
                        if (useGltf)
                        {
                            var rel = $"{lodDirRel}/{outName}.gltf";
                            var binRel = $"{lodDirRel}/{outName}.bin";
                            var res = GltfMeshWriter.Write(
                                simplified,
                                binUri: new Uri(Path.GetFileName(binRel), UriKind.Relative),
                                new GltfMaterialSpec { Name = outName, BaseColorR = 0.8f, BaseColorG = 0.8f, BaseColorB = 0.8f });
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), res.GltfJson, ct);
                            await BinaryFile.WriteAllBytesAsync(Path.Combine(root, binRel), res.BinBytes.Span.ToArray(), ct);
                            artifacts.Add(new WorldArtifact { Kind = $"buildings_tri{budget}_gltf", Path = rel, ContentType = "gltf", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: null) });
                            artifacts.Add(new WorldArtifact { Kind = $"buildings_tri{budget}_bin", Path = binRel, ContentType = "bin" });
                        }
                        else
                        {
                            var rel = $"{lodDirRel}/{outName}.obj";
                            var obj = InjectObjMaterial(ObjMeshWriter.Write(simplified), worldMtlRel, "building_residential");
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), obj, ct);
                            artifacts.Add(new WorldArtifact { Kind = $"buildings_tri{budget}", Path = rel, ContentType = "obj", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: null) });
                        }
                        manifestNeedsRegen = true;
                    }

                    // Roads
                    if (roadsMeshObj.Indices.Count / 3 > budget)
                    {
                        var simplified = MeshDecimator.SimplifyToTargetTriangles(roadsMeshObj, budget);
                        var outName = $"roads_tri{budget}";
                        if (useGltf)
                        {
                            var rel = $"{lodDirRel}/{outName}.gltf";
                            var binRel = $"{lodDirRel}/{outName}.bin";
                            var res = GltfMeshWriter.Write(
                                simplified,
                                binUri: new Uri(Path.GetFileName(binRel), UriKind.Relative),
                                new GltfMaterialSpec { Name = outName, BaseColorR = 0.15f, BaseColorG = 0.15f, BaseColorB = 0.15f });
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), res.GltfJson, ct);
                            await BinaryFile.WriteAllBytesAsync(Path.Combine(root, binRel), res.BinBytes.Span.ToArray(), ct);
                            artifacts.Add(new WorldArtifact { Kind = $"roads_tri{budget}_gltf", Path = rel, ContentType = "gltf", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: null) });
                            artifacts.Add(new WorldArtifact { Kind = $"roads_tri{budget}_bin", Path = binRel, ContentType = "bin" });
                        }
                        else
                        {
                            var rel = $"{lodDirRel}/{outName}.obj";
                            var obj = InjectObjMaterial(ObjMeshWriter.Write(simplified), worldMtlRel, "road_asphalt");
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), obj, ct);
                            artifacts.Add(new WorldArtifact { Kind = $"roads_tri{budget}", Path = rel, ContentType = "obj", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: null) });
                        }
                        manifestNeedsRegen = true;
                    }

                    // Water
                    if (waterMeshObj.Indices.Count / 3 > budget)
                    {
                        var simplified = MeshDecimator.SimplifyToTargetTriangles(waterMeshObj, budget);
                        var outName = $"water_tri{budget}";
                        if (useGltf)
                        {
                            var rel = $"{lodDirRel}/{outName}.gltf";
                            var binRel = $"{lodDirRel}/{outName}.bin";
                            var res = GltfMeshWriter.Write(
                                simplified,
                                binUri: new Uri(Path.GetFileName(binRel), UriKind.Relative),
                                new GltfMaterialSpec { Name = outName, BaseColorR = 0.1f, BaseColorG = 0.3f, BaseColorB = 0.7f, DoubleSided = true });
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), res.GltfJson, ct);
                            await BinaryFile.WriteAllBytesAsync(Path.Combine(root, binRel), res.BinBytes.Span.ToArray(), ct);
                            artifacts.Add(new WorldArtifact { Kind = $"water_tri{budget}_gltf", Path = rel, ContentType = "gltf", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: null) });
                            artifacts.Add(new WorldArtifact { Kind = $"water_tri{budget}_bin", Path = binRel, ContentType = "bin" });
                        }
                        else
                        {
                            var rel = $"{lodDirRel}/{outName}.obj";
                            var obj = InjectObjMaterial(ObjMeshWriter.Write(simplified), worldMtlRel, "water");
                            await TextFile.WriteAllTextAsync(Path.Combine(root, rel), obj, ct);
                            artifacts.Add(new WorldArtifact { Kind = $"water_tri{budget}", Path = rel, ContentType = "obj", Metadata = BuildMeshMetadata(simplified, lodLevel: budget, gridChunk: null) });
                        }
                        manifestNeedsRegen = true;
                    }
                }
            }

            if (instancesChunkSamples <= 0)
            {
                var payload = new
                {
                    kind = "sceneryInstances",
                    units = "meters",
                    instances = vegInstances.Select(i => new
                    {
                        kind = i.Kind,
                        position = new { x = i.Position.X, y = i.Position.Y, z = i.Position.Z },
                        yawDegrees = i.YawDegrees,
                        uniformScale = i.UniformScale
                    }).ToArray()
                };
                await TextFile.WriteAllTextAsync(Path.Combine(root, instancesRel), JsonSerializer.Serialize(payload), ct);
                UpsertArtifactInstancesMetadata(artifacts, "instances", instancesRel, vegInstances.Length);
            }
            else
            {
                var stride = Math.Max(1, instancesChunkSamples - 1);
                var chunkDirRel = "instances_chunks";
                DirectoryOps.CreateDirectory(Path.Combine(root, chunkDirRel));

                var byChunk = new Dictionary<(int x0, int y0), List<SceneryInstance>>();
                for (var i = 0; i < vegInstances.Length; i++)
                {
                    // Bucket by the underlying terrain-grid sample indices.
                    var p = vegInstances[i].Position;
                    var sampleX = (int)Math.Round((p.X + terrainOriginOffset.OffsetX) / (float)terrainGrid.Spacing.MetersX);
                    var sampleY = (int)Math.Round((p.Z + terrainOriginOffset.OffsetZ) / (float)terrainGrid.Spacing.MetersY);
                    if (sampleX < 0) sampleX = 0;
                    if (sampleY < 0) sampleY = 0;
                    if (sampleX >= terrainGrid.Width) sampleX = terrainGrid.Width - 1;
                    if (sampleY >= terrainGrid.Height) sampleY = terrainGrid.Height - 1;

                    var cx = sampleX / stride;
                    var cy = sampleY / stride;
                    var x0 = cx * stride;
                    var y0 = cy * stride;
                    var key = (x0, y0);
                    if (!byChunk.TryGetValue(key, out var list))
                    {
                        list = new List<SceneryInstance>();
                        byChunk[key] = list;
                    }
                    list.Add(vegInstances[i]);
                }

                // Replace single instances artifact with chunked artifacts.
                artifacts.RemoveAll(a => string.Equals(a.Kind, "instances", StringComparison.OrdinalIgnoreCase));
                manifestNeedsRegen = true;

                foreach (var kvp in byChunk)
                {
                    var key = kvp.Key;
                    var list = kvp.Value;
                    var payload = new
                    {
                        kind = "sceneryInstances",
                        units = "meters",
                        instances = list.Select(i => new
                        {
                            kind = i.Kind,
                            position = new { x = i.Position.X, y = i.Position.Y, z = i.Position.Z },
                            yawDegrees = i.YawDegrees,
                            uniformScale = i.UniformScale
                        }).ToArray()
                    };
                    var jsonText = JsonSerializer.Serialize(payload);
                    var rel = $"{chunkDirRel}/instances_{key.x0}_{key.y0}.json";
                    await TextFile.WriteAllTextAsync(Path.Combine(root, rel), jsonText, ct);
                    artifacts.Add(new WorldArtifact
                    {
                        Kind = "instances_chunk",
                        Path = rel,
                        ContentType = "json",
                        Metadata = new WorldArtifactMetadata
                        {
                            InstanceCount = list.Count
                        }
                    });
                }
            }
            await TextFile.WriteAllTextAsync(Path.Combine(root, materialsRel), materialsJson, ct);

            if (manifestNeedsRegen)
            {
                // Regenerate manifest after terrain artifacts change.
                manifestJson = WorldBundleWriters.WriteManifest(new WorldBundleManifest
                {
                    Version = "0.1.0",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    Bounds = geoBounds,
                    Origin = origin,
                    MetersPerUnit = 1.0f,
                    Artifacts = artifacts,
                    ProviderProvenance = provenance
                });
            }
            await TextFile.WriteAllTextAsync(Path.Combine(root, manifestRel), manifestJson, ct);

            if (!useGltf)
            {
                DirectoryOps.EnsureParentDirectoryExists(Path.Combine(root, unityRel));
                await TextFile.WriteAllTextAsync(Path.Combine(root, unityRel),
                    "Copy the Unity importer scripts from this repo under `src/Nexo.Core.UI.Unity/Frameworks/Unity/GeoWorld/` into your Unity project, then run the importer.\n",
                    ct);
            }

            var result = new
            {
                ok = errors.Count == 0,
                outDir = root,
                artifacts = artifacts.Select(a => a.Path).ToArray(),
                errors,
                steps
            };

            if (json)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(result));
            }
            else
            {
                Console.Out.WriteLine($"Wrote world bundle: {root}");
            }

            return errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "world build failed");
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

    public async Task<int> ValidateAsync(DirectoryInfo bundleDir, bool json, bool verbose, CancellationToken ct)
    {
        try
        {
            if (bundleDir is null) throw new ArgumentNullException(nameof(bundleDir));
            var root = bundleDir.FullName;
            var errors = new List<string>();
            var warnings = new List<string>();

            var manifestPath = Path.Combine(root, "manifest.json");
            if (!BinaryFile.Exists(manifestPath))
            {
                errors.Add("manifest.json not found");
            }

            WorldBundleManifest? manifest = null;
            if (errors.Count == 0)
            {
                var txt = await TextFile.ReadAllTextAsync(manifestPath, ct);
                manifest = JsonSerializer.Deserialize<WorldBundleManifest>(txt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (manifest == null) errors.Add("Failed to parse manifest.json");
            }

            if (manifest != null)
            {
                // Basic sanity.
                try { manifest.Bounds.Validate(); } catch (Exception ex) { errors.Add("Bounds invalid: " + ex.Message); }
                if (manifest.MetersPerUnit <= 0) errors.Add("MetersPerUnit must be > 0");

                // Artifact existence.
                for (var i = 0; i < manifest.Artifacts.Count; i++)
                {
                    var a = manifest.Artifacts[i];
                    if (string.IsNullOrWhiteSpace(a.Path))
                    {
                        errors.Add($"Artifact[{i}] has empty Path");
                        continue;
                    }

                    var abs = Path.Combine(root, a.Path);
                    if (!BinaryFile.Exists(abs))
                    {
                        errors.Add($"Missing artifact file: {a.Path}");
                    }
                }

                // materials.json texture references
                var materialsArtifact = manifest.Artifacts.FirstOrDefault(a => string.Equals(a.Kind, "materials", StringComparison.OrdinalIgnoreCase));
                if (materialsArtifact != null && !string.IsNullOrWhiteSpace(materialsArtifact.Path))
                {
                    var materialsPath = Path.Combine(root, materialsArtifact.Path);
                    if (BinaryFile.Exists(materialsPath))
                    {
                        try
                        {
                            var raw = await TextFile.ReadAllTextAsync(materialsPath, ct);
                            var list = JsonSerializer.Deserialize<List<Nexo.GeoWorld.Materials.MaterialAssignment>>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (list != null)
                            {
                                for (var i = 0; i < list.Count; i++)
                                {
                                    var t = list[i].BaseColorTexturePath;
                                    if (string.IsNullOrWhiteSpace(t)) continue;
                                    var texAbs = Path.Combine(root, t);
                                    if (!BinaryFile.Exists(texAbs)) errors.Add($"Missing texture referenced by materials.json: {t}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            warnings.Add("Failed to parse materials.json for texture validation: " + ex.Message);
                        }
                    }
                }
            }

            var result = new { ok = errors.Count == 0, bundleDir = root, errors, warnings };
            if (json) Console.Out.WriteLine(JsonSerializer.Serialize(result));
            else Console.Out.WriteLine(errors.Count == 0 ? $"World bundle OK: {root}" : $"World bundle INVALID: {root}");

            return errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "world validate failed");
            if (json) Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else Console.Error.WriteLine(ex.Message);
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
        return new EchoElevationProvider();
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

    private static List<(int x0, int y0, int w, int h)> BuildGridChunks(int width, int height, int chunkSamples)
    {
        if (chunkSamples <= 1) return new List<(int, int, int, int)> { (0, 0, width, height) };

        var chunks = new List<(int x0, int y0, int w, int h)>();
        for (var y0 = 0; y0 < height; y0 += (chunkSamples - 1))
        {
            for (var x0 = 0; x0 < width; x0 += (chunkSamples - 1))
            {
                var w = Math.Min(chunkSamples, width - x0);
                var h = Math.Min(chunkSamples, height - y0);
                if (w >= 2 && h >= 2) chunks.Add((x0, y0, w, h));
            }
        }
        return chunks;
    }

    private static ElevationGrid SliceGrid(ElevationGrid src, int x0, int y0, int w, int h)
    {
        var heights = new float[w * h];
        var ii = 0;
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                heights[ii++] = src.GetHeightMeters(x0 + x, y0 + y);
            }
        }

        // Bounds are not used for mesh generation positions; keep same bounds for portability.
        return new ElevationGrid(w, h, src.Bounds, src.Spacing, heights);
    }

    private static MeshData OffsetMesh(MeshData mesh, float offsetX, float offsetZ)
    {
        if (offsetX == 0f && offsetZ == 0f) return mesh;
        var v = mesh.Vertices.ToArray();
        for (var i = 0; i < v.Length; i++)
        {
            v[i] = new Vector3(v[i].X + offsetX, v[i].Y, v[i].Z + offsetZ);
        }
        return mesh with { Vertices = v };
    }

    private static List<int> ParseIntList(string? csv)
    {
        var list = new List<int>();
        if (string.IsNullOrWhiteSpace(csv)) return list;
        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i].Trim(), out var v))
            {
                list.Add(v);
            }
        }
        return list;
    }

    private static string BuildWorldMtl(string? terrainTextureFile, string? buildingsTextureFile, string? roadsTextureFile, string? waterTextureFile)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Nexo.GeoWorld MTL");

        sb.AppendLine("newmtl terrain");
        sb.AppendLine("Ka 1 1 1");
        sb.AppendLine("Kd 1 1 1");
        sb.AppendLine("Ks 0 0 0");
        sb.AppendLine("d 1.0");
        sb.AppendLine("illum 1");
        if (!string.IsNullOrWhiteSpace(terrainTextureFile))
        {
            sb.AppendLine("map_Kd " + terrainTextureFile);
        }

        // Basic defaults for other layers (no textures provided by default).
        sb.AppendLine();
        sb.AppendLine("newmtl building_residential");
        sb.AppendLine("Ka 0.7 0.7 0.7");
        sb.AppendLine("Kd 0.7 0.7 0.7");
        sb.AppendLine("Ks 0 0 0");
        sb.AppendLine("d 1.0");
        sb.AppendLine("illum 1");
        if (!string.IsNullOrWhiteSpace(buildingsTextureFile))
        {
            sb.AppendLine("map_Kd " + buildingsTextureFile);
        }

        sb.AppendLine();
        sb.AppendLine("newmtl building_commercial");
        sb.AppendLine("Ka 0.6 0.6 0.65");
        sb.AppendLine("Kd 0.6 0.6 0.65");
        sb.AppendLine("Ks 0 0 0");
        sb.AppendLine("d 1.0");
        sb.AppendLine("illum 1");
        if (!string.IsNullOrWhiteSpace(buildingsTextureFile))
        {
            sb.AppendLine("map_Kd " + buildingsTextureFile);
        }

        sb.AppendLine();
        sb.AppendLine("newmtl building_industrial");
        sb.AppendLine("Ka 0.55 0.55 0.55");
        sb.AppendLine("Kd 0.55 0.55 0.55");
        sb.AppendLine("Ks 0 0 0");
        sb.AppendLine("d 1.0");
        sb.AppendLine("illum 1");
        if (!string.IsNullOrWhiteSpace(buildingsTextureFile))
        {
            sb.AppendLine("map_Kd " + buildingsTextureFile);
        }

        sb.AppendLine();
        sb.AppendLine("newmtl road_asphalt");
        sb.AppendLine("Ka 0.12 0.12 0.12");
        sb.AppendLine("Kd 0.12 0.12 0.12");
        sb.AppendLine("Ks 0 0 0");
        sb.AppendLine("d 1.0");
        sb.AppendLine("illum 1");
        if (!string.IsNullOrWhiteSpace(roadsTextureFile))
        {
            sb.AppendLine("map_Kd " + roadsTextureFile);
        }

        sb.AppendLine();
        sb.AppendLine("newmtl water");
        sb.AppendLine("Ka 0.0 0.2 0.3");
        sb.AppendLine("Kd 0.0 0.2 0.3");
        sb.AppendLine("Ks 0 0 0");
        sb.AppendLine("d 0.9");
        sb.AppendLine("illum 1");
        if (!string.IsNullOrWhiteSpace(waterTextureFile))
        {
            sb.AppendLine("map_Kd " + waterTextureFile);
        }

        return sb.ToString();
    }

    private static string InjectObjMaterial(string obj, string mtllib, string materialName)
    {
        // Insert mtllib + usemtl after header and group.
        var lines = obj.Split('\n');
        var sb = new System.Text.StringBuilder(obj.Length + 64);
        var inserted = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            sb.Append(line).Append('\n');
            if (!inserted && line.StartsWith("g ", StringComparison.Ordinal))
            {
                sb.Append("mtllib ").Append(mtllib).Append('\n');
                sb.Append("usemtl ").Append(materialName).Append('\n');
                inserted = true;
            }
        }
        return sb.ToString();
    }

    private static MeshData AddTerrainTexCoords(MeshData mesh, ElevationGrid grid, GeoBounds bounds, RasterMosaic mosaic)
    {
        var w = grid.Width;
        var h = grid.Height;
        if (mesh.Vertices.Count != w * h)
            return mesh; // Unexpected; don't mutate.

        var tex = new Vector2[mesh.Vertices.Count];
        var minLon = bounds.MinLongitude.Degrees;
        var maxLon = bounds.MaxLongitude.Degrees;
        var minLat = bounds.MinLatitude.Degrees;
        var maxLat = bounds.MaxLatitude.Degrees;

        var mosaicW = mosaic.TilesWide * mosaic.TileSizePixels;
        var mosaicH = mosaic.TilesHigh * mosaic.TileSizePixels;

        for (var row = 0; row < h; row++)
        {
            var tY = h <= 1 ? 0.0 : (double)row / (double)(h - 1);
            var lat = maxLat - (tY * (maxLat - minLat));
            for (var col = 0; col < w; col++)
            {
                var tX = w <= 1 ? 0.0 : (double)col / (double)(w - 1);
                var lon = minLon + (tX * (maxLon - minLon));

                var (px, py) = WebMercatorTileMath.LonLatToGlobalPixelXY(mosaic.Zoom, lon, lat, mosaic.TileSizePixels);
                var localX = px - (mosaic.MinTileX * mosaic.TileSizePixels);
                var localY = py - (mosaic.MinTileY * mosaic.TileSizePixels);

                var u = (float)(localX / mosaicW);
                var v = 1f - (float)(localY / mosaicH);
                tex[row * w + col] = new Vector2(u, v);
            }
        }

        return mesh with { TexCoords = tex };
    }

    private static MeshData AddTerrainTexCoordsChunk(MeshData mesh, ElevationGrid fullGrid, GeoBounds bounds, RasterMosaic mosaic, int x0, int y0, int w, int h)
    {
        if (mesh.Vertices.Count != w * h) return mesh;

        var fullW = fullGrid.Width;
        var fullH = fullGrid.Height;

        var minLon = bounds.MinLongitude.Degrees;
        var maxLon = bounds.MaxLongitude.Degrees;
        var minLat = bounds.MinLatitude.Degrees;
        var maxLat = bounds.MaxLatitude.Degrees;

        var mosaicW = mosaic.TilesWide * mosaic.TileSizePixels;
        var mosaicH = mosaic.TilesHigh * mosaic.TileSizePixels;

        var tex = new Vector2[mesh.Vertices.Count];
        for (var row = 0; row < h; row++)
        {
            var globalY = y0 + row;
            var tY = fullH <= 1 ? 0.0 : (double)globalY / (double)(fullH - 1);
            var lat = maxLat - (tY * (maxLat - minLat));
            for (var col = 0; col < w; col++)
            {
                var globalX = x0 + col;
                var tX = fullW <= 1 ? 0.0 : (double)globalX / (double)(fullW - 1);
                var lon = minLon + (tX * (maxLon - minLon));

                var (px, py) = WebMercatorTileMath.LonLatToGlobalPixelXY(mosaic.Zoom, lon, lat, mosaic.TileSizePixels);
                var localX = px - (mosaic.MinTileX * mosaic.TileSizePixels);
                var localY = py - (mosaic.MinTileY * mosaic.TileSizePixels);

                var u = (float)(localX / mosaicW);
                var v = 1f - (float)(localY / mosaicH);
                tex[row * w + col] = new Vector2(u, v);
            }
        }

        return mesh with { TexCoords = tex };
    }

    private static Vector3[] SliceNormals(Vector3[] normalsAll, int srcWidth, int x0, int y0, int w, int h)
    {
        var sliced = new Vector3[w * h];
        var ii = 0;
        for (var y = 0; y < h; y++)
        {
            var srcRow = (y0 + y) * srcWidth;
            for (var x = 0; x < w; x++)
            {
                sliced[ii++] = normalsAll[srcRow + (x0 + x)];
            }
        }
        return sliced;
    }

    private static MeshData TranslateMesh(MeshData mesh, double dxMeters, double dzMeters)
    {
        if (dxMeters == 0.0 && dzMeters == 0.0) return mesh;
        var v = mesh.Vertices.ToArray();
        var dx = (float)dxMeters;
        var dz = (float)dzMeters;
        for (var i = 0; i < v.Length; i++)
        {
            v[i] = new Vector3(v[i].X + dx, v[i].Y, v[i].Z + dz);
        }
        return mesh with { Vertices = v };
    }

    private static SceneryInstance[] TranslateInstances(SceneryInstance[] instances, double dxMeters, double dzMeters)
    {
        if (instances.Length == 0) return instances;
        if (dxMeters == 0.0 && dzMeters == 0.0) return instances;
        var dx = (float)dxMeters;
        var dz = (float)dzMeters;
        var outArr = new SceneryInstance[instances.Length];
        for (var i = 0; i < instances.Length; i++)
        {
            var s = instances[i];
            outArr[i] = s with { Position = new Vector3(s.Position.X + dx, s.Position.Y, s.Position.Z + dz) };
        }
        return outArr;
    }

    private sealed record GridOriginOffset
    {
        public required double OffsetX { get; init; }
        public required double OffsetZ { get; init; }
    }

    private static GridOriginOffset ComputeGridOriginOffsetMeters(ElevationGrid grid, GeoPoint origin)
    {
        // Convert origin geo point into grid sample index, then into meters.
        // y=0 is north edge (MaxLatitude), x=0 is west edge (MinLongitude).
        var lon0 = grid.Bounds.MinLongitude.Degrees;
        var lon1 = grid.Bounds.MaxLongitude.Degrees;
        var lat0 = grid.Bounds.MinLatitude.Degrees;
        var lat1 = grid.Bounds.MaxLatitude.Degrees;

        var tX = (origin.Longitude.Degrees - lon0) / (lon1 - lon0);
        var tY = (lat1 - origin.Latitude.Degrees) / (lat1 - lat0);

        var sx = tX * (grid.Width - 1);
        var sy = tY * (grid.Height - 1);

        return new GridOriginOffset
        {
            OffsetX = sx * grid.Spacing.MetersX,
            OffsetZ = sy * grid.Spacing.MetersY
        };
    }

    private static string RelativeUriForFile(string fromRelFile, string toRelFile)
    {
        // Both inputs are bundle-relative paths using '/' separators.
        // glTF URIs should always use '/' separators.
        if (string.IsNullOrWhiteSpace(fromRelFile)) return toRelFile.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(toRelFile)) return toRelFile;

        var from = fromRelFile.Replace('\\', '/');
        var to = toRelFile.Replace('\\', '/');
        var lastSlash = from.LastIndexOf('/');
        if (lastSlash < 0) return to;

        var dir = from.Substring(0, lastSlash);
        if (dir.Length == 0) return to;

        var depth = 1;
        for (var i = 0; i < dir.Length; i++)
        {
            if (dir[i] == '/') depth++;
        }

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < depth; i++) sb.Append("../");
        sb.Append(to);
        return sb.ToString();
    }

    private static void UpsertArtifactMetadata(
        List<WorldArtifact> artifacts,
        string kind,
        string path,
        MeshData mesh,
        int? lodLevel,
        WorldGridChunkRef? gridChunk)
    {
        for (var i = 0; i < artifacts.Count; i++)
        {
            if (!string.Equals(artifacts[i].Kind, kind, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.Equals(artifacts[i].Path, path, StringComparison.OrdinalIgnoreCase)) continue;

            artifacts[i] = artifacts[i] with
            {
                Metadata = BuildMeshMetadata(mesh, lodLevel, gridChunk)
            };
            return;
        }
    }

    private static void UpsertArtifactInstancesMetadata(List<WorldArtifact> artifacts, string kind, string path, int instanceCount)
    {
        for (var i = 0; i < artifacts.Count; i++)
        {
            if (!string.Equals(artifacts[i].Kind, kind, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.Equals(artifacts[i].Path, path, StringComparison.OrdinalIgnoreCase)) continue;

            artifacts[i] = artifacts[i] with
            {
                Metadata = new WorldArtifactMetadata { InstanceCount = instanceCount }
            };
            return;
        }
    }

    private static WorldArtifactMetadata BuildMeshMetadata(MeshData mesh, int? lodLevel, WorldGridChunkRef? gridChunk)
    {
        var aabb = ComputeAabb(mesh.Vertices);
        return new WorldArtifactMetadata
        {
            LodLevel = lodLevel,
            GridChunk = gridChunk,
            VertexCount = mesh.Vertices.Count,
            TriangleCount = mesh.Indices.Count / 3,
            LocalBoundsMeters = aabb
        };
    }

    private static WorldAabb ComputeAabb(IReadOnlyList<Vector3> vertices)
    {
        if (vertices.Count == 0)
        {
            return new WorldAabb { MinX = 0, MinY = 0, MinZ = 0, MaxX = 0, MaxY = 0, MaxZ = 0 };
        }

        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var minZ = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;
        var maxZ = float.NegativeInfinity;

        for (var i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            if (v.X < minX) minX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.Z < minZ) minZ = v.Z;
            if (v.X > maxX) maxX = v.X;
            if (v.Y > maxY) maxY = v.Y;
            if (v.Z > maxZ) maxZ = v.Z;
        }

        return new WorldAabb
        {
            MinX = minX,
            MinY = minY,
            MinZ = minZ,
            MaxX = maxX,
            MaxY = maxY,
            MaxZ = maxZ
        };
    }

}

