# GeoTerrain Implementation Plan (phased, dogfooding Nexo)

This plan follows Nexo’s layering and patterns discovered in `ANALYSIS.md`.

## Phase 1: Domain (no external dependencies) — **First PR**

Goal: establish **portable core models + deterministic algorithms** that can run in air-gapped and Unity-era runtimes.

1. **Domain models** in `src/Nexo.GeoTerrain/` (multi-target `netstandard2.0;net8.0`)
   - [ ] `GeoBounds` (lat/lon rectangle)
   - [ ] `GeoPoint` (lat/lon)
   - [ ] `ElevationGrid` / `Heightmap` (samples + cell size + bounds + “no data”)
   - [ ] `MeshData` (vertices, indices, normals)
   - [ ] `MeshQualityReport` (basic metrics)
2. **Value objects (no enums in domain)**
   - [ ] `ElevationUnit` (meters/feet)
   - [ ] `MeshTopology` (grid triangles, etc.)
   - [ ] `MeshExportFormat` (OBJ now; glTF later)
3. **Deterministic algorithms**
   - [ ] Height normalization helpers
   - [ ] Grid-to-mesh triangulation
   - [ ] Normal computation
   - [ ] Basic validation (bounds, NaNs, dimensions, triangle count)
4. **Unit tests** in `src/Nexo.Tests.GeoTerrain/` using Nexo’s test harness (`UnitTestBase`)
   - [ ] Value object parsing + equality
   - [ ] Mesh generation for tiny grids (2×2, 3×3)
   - [ ] Normal correctness for flat plane
   - [ ] Validation failures (bad dims, null samples)

## Phase 2: Tools (atomic operations, top-layer I/O)

Goal: keep raw I/O in `Nexo.Tools.GeoTerrain`, while algorithms remain in domain.

1. [ ] `geoterrain.parse.hgt` tool (reads SRTM HGT from disk → emits `Heightmap` payload + delta logs)
2. [ ] `geoterrain.export.obj` tool (writes OBJ to disk from `MeshData`)
3. [ ] Add Roslyn-style consistency gate if we generate any CLI commands via self-extend demo

## Phase 3: Adapters (external providers + caching)

Goal: online download providers behind ports, with offline local cache.

1. [ ] Port: `IElevationProvider` (in a GeoTerrain orchestration/ports project)
2. [ ] `SrtmProviderAdapter` (download tiles)
3. [ ] `MapboxTerrainRgbAdapter`
4. [ ] `LocalCacheElevationAdapter`

## Phase 4: Agentic mode (AI-assisted tuning/orchestration)

Goal: demonstrate hot-swappable behavior: deterministic pipeline works offline; agentic augments when available.

1. [ ] `TerrainGeneratorAgent` (suggests parameters / LOD strategies)
2. [ ] Bricks for: “choose parameters”, “generate mesh”, “validate mesh”, with fallback chain

## Phase 5: CLI integration + E2E

1. [ ] `nexo geoterrain generate --input ... --output ... --airgap`
2. [ ] `nexo geoterrain fetch --bounds ... --provider ...`
3. [ ] E2E tests similar to `DemoCliE2ETests`

