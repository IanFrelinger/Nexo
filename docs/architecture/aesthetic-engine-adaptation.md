# Aesthetic and engine adaptation glossary

Ashlar carries **portable visual intent** in `AestheticPack`. Game engines (Unity, Unreal, Godot, custom) **interpret** these fields and map them to native shaders, materials, and render features.

## Fields (engine-neutral)

| Field | Purpose |
| ----- | ------- |
| `GeometryStrategy` | How authored geometry is produced (voxel, low_poly, pbr, …). Use `GeometryStrategies` constants. |
| `RenderingPipelineKind` | Semantic render path (forward stylized, deferred PBR, …). Use `RenderingPipelineKinds`. Host maps to URP/HDRP, Unreal, Godot renderer settings. |
| `DefaultPaletteColors` | Hex colours for procedural assignment when textures are absent. |
| `LodLevels` | Ordered LOD tiers; `DetailFactor` meaning depends on `GeometryStrategy` (see `LodLevel` XML docs). |
| `PostProcessEffects` | Logical post names (bloom, vignette, …); host maps to engine post stack. |
| `EngineSurfaceBindings` | Per-engine optional rows: logical `Role` + `MaterialSurfaceId` + optional `AssetOrShaderHint` and `Parameters`. |

## Catalogs and validation

- **`AestheticAdaptationCatalog`** — known `RenderingPipelineKind` and `EngineId` values for strict checks.
- **`GeometryStrategies`**, **`RenderingSurfaceRoles`**, **`MaterialSurfaceIds`** — recommended strings; roles/surfaces outside documented sets get **informational** validation codes, not hard failures.
- **`AestheticPackValidation.Validate`** — returns `AestheticValidationIssue` list; **`AestheticPackValidation.IsValid`** treats `binding.undocumented_*` as non-blocking by default.

## Forge API

- **`POST /api/forge/aesthetic/apply`** — apply a **built-in** pack by id (unchanged).
- **`POST /api/forge/aesthetic/apply-pack`** — body: `ForgeApplyCustomAestheticPackRequest` (`Pack`, optional `Scope`, optional `RequireKnownEngineIds`). Validates then stores the full pack on the session.

## Mapbox tiles (shared kernel)

`Ashlar.Commercial.GameDomain` (`Ashlar.Commercial.GameDomain.Maps`) provides **`MapboxTileUrls`**, **`MapboxWebMercatorTileMath`**, and **`MapboxTileResponseValidators`** for host-agnostic URL construction and HTTP response checks. **Never** embed access tokens in source; use environment variables or secret stores.

## Precedence (recommended)

1. If `EngineSurfaceBindingResolver.TryGetBinding(pack, currentEngine, role)` returns a row, use its `MaterialSurfaceId` and hints.
2. Else derive from `GeometryStrategy` + `RenderingPipelineKind` + palette defaults.

## Unity-specific descriptors

Types under `Ashlar.Commercial.GameDomain` (`Ashlar.Commercial.GameDomain.Assets`, `Descriptors`, etc.) still mention Unity in comments. Treat them as **legacy host projections**; new cross-engine work should prefer `AestheticPack` + neutral descriptors or parallel DTOs in a consuming repo.
