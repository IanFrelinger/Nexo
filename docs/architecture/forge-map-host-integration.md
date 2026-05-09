# Forge map host integration (milestones)

This document tracks **host-side** work that sits next to runtime types in **`Nexo.GameDomain`** and HTTP endpoints in **`Nexo.API`**.

## M1 — Engine aesthetic manifest

**Goal:** The game host loads **`GET /api/forge/engine/{engineId}/aesthetic-manifest`** and maps JSON to materials / LOD / effects.

**Implemented in-repo:**

- Reference **`docs/samples/ForgeMapHostSample`** unwraps `ForgeEngineManifestResponse.json` and prints core fields.
- Production hosts should deserialize into engine-specific objects (Unity ScriptableObjects, Unreal data tables, etc.) using the same JSON schema produced by **`EngineAestheticManifestBuilder`**.

## M2 — Tile orchestration

**Goal:** Deterministic **z/x/y** and HTTPS URLs for vector (and later terrain) fetches.

**Implemented in-repo:**

- **`WebMercatorTileMath`** — lon/lat → tile index (slippy map / XYZ).
- **`VectorTileUrlBuilder.MapboxVectorTileUrl`** — Mapbox `v4` vector tile URL.
- Sample wires **`POST /api/forge/map/pipeline/run`** with **`mvtTileX` / `mvtTileY`** aligned to the built URL.

**Server-side:** configure **`Nexo:ForgeSession:AllowedMapFetchHosts`** for providers you use (e.g. `api.mapbox.com`).

## M3 — Minimal geometry slice

**Goal:** First vertical path from bytes → engine primitives.

**Implemented in-repo (hints only):**

- **`MapHostImportHints`** returns actionable bullet text per **`VectorMapParseSummary.ParserKind`** (`geojson`, `osm_xml`, `mvt`).
- Actual tessellation / voxel fill remains **in the engine**; hints narrow what importers should prioritize.

## M4 — LOD tile pyramid (implemented)

**Goal:** Deterministic **zoom per aesthetic LOD tier** so hosts prefetch/stream the right tiles by distance.

**Implemented in-repo:**

- **`MapLodPyramidPlanner`** + **`MapTilePyramidTier`** — given **`LodLevels`** and **`finestZoom`**, yields zoom **14→13→12…** (one step per LOD level).
- **`GET /api/forge/map/tile-pyramid?finestZoom=14`** — JSON `{ finestZoom, tiers[] }` for the active aesthetic.
- Sample prints tiers after the manifest step (**`PYRAMID_FINEST_ZOOM`** env).

## M5 — Map verification (implemented)

**Goal:** Rule-based sanity checks on parsed vector payloads for LOD / QA workflows.

**Implemented in-repo:**

- **`IMapVerificationService`**, **`HeuristicMapVerificationService`**, **`MapVerificationResult`** / **`MapVerificationIssue`**.
- **`MapPipelineRunner`** appends **`verify=…`** after parse when **`EnableMapVerification`** is true (**`Nexo:ForgeSession`**).
- Optional **`MapVerificationFailsPipeline`**: when **`true`**, verification **Warning+** marks **`fetch_vector`** as **`error`** (`Success=false` on the run).
- Unit tests in **`HeuristicMapVerificationServiceTests`** and pipeline strict-mode tests.

## M6 — Material / aesthetic assist (implemented)

**Goal:** Surface procedural colours and shader-parameter hints aligned with **`EngineSurfaceBinding`** roles without requiring tessellation in Nexo.API.

**Implemented in-repo:**

- **`IMaterialIntelligenceService`**, **`HeuristicMaterialIntelligenceService`**, **`MaterialSuggestionResult`** / **`MaterialSurfaceHint`**.
- **`GET /api/forge/map/material-hints`** — JSON hints for the active aesthetic (optional **`parseKind`** query).
- Unit tests in **`HeuristicMaterialIntelligenceServiceTests`** and API coverage in **`ForgeEndpointsTests`**.

## Tile cache and reproducibility (phase B)

**Goal:** Stable on-disk identity for raw tile bytes so hosts can prefetch once and replay imports.

**Implemented in-repo:**

- **`MapTileCacheKey`** — sanitised path segments from aesthetic id, provider id, and **`z/x/y`**.
- **`MapTileDiskCache`** — async read/write under a root directory.
- Sample: set **`NEXO_TILE_CACHE_DIR`** in **`ForgeMapHostSample`** to exercise cache after building a Mapbox URL.

## Engine bridge (phase C)

**Goal:** Copy-paste starters for Unity/Godot that consume the same HTTP contracts as the sample.

**Implemented in-repo:**

- **`docs/engine-bridge/README.md`** — overview.
- **`docs/engine-bridge/snippets/UnitySample.cs`** — manifest + material hints via **`HttpClient`**.
- **`docs/engine-bridge/snippets/GodotTileBridge.gd`** — tile pyramid JSON via **`HTTPRequest`**.

## Next steps (optional)

- Optional model-augmented material suggestions (bounded latency; off by default).
- Deeper engine-specific importers (addressables, GLTF export, etc.).

## Related

- **`docs/architecture/forge-map-adaptation.md`** — Forge pipeline and SSRF policy.
