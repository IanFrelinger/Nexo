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

## Next steps (later milestones)

- Import **`surfaceBindings`** into engine shaders/material slots.
- Cache tiles by **`z/x/y`** under a content folder keyed by aesthetic id.
- Add **`IMapVerificationService`** rules when you need CI fixtures on canned tiles.

## Related

- **`docs/architecture/forge-map-adaptation.md`** — Forge pipeline and SSRF policy.
