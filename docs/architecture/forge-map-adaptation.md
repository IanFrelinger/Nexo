# Forge map adaptation and engine manifests

Runtime types live in **`Nexo.GameDomain`**; HTTP surface in **`Nexo.API`** (`ForgeEndpoints`).

## Map adaptation plan

`MapAdaptationPlanner.Plan(session, builtInCatalog)` returns a **`MapAdaptationPlan`**: effective
`MapRenderingProfile`, geometry strategy, ordered **`PipelineStages`**, and notes. It resolves the
active aesthetic from the server-scoped `aesthetic` setting when set, otherwise the first pack on
the session.

## Dry-run pipeline

`POST /api/forge/map/pipeline/run` accepts **`MapPipelineRunRequest`** (`dryRun`, `timeoutMs`).
Only **`dryRun: true`** is implemented in the reference API: **`MapPipelineDryRun`** marks each
stage as simulated without network or geometry work. Hosts replace this with real orchestration.

## Engine manifest

`GET /api/forge/engine/{engineId}/aesthetic-manifest` returns **`ForgeEngineManifestResponse`**
with JSON from **`EngineAestheticManifestBuilder`**, including `EngineSurfaceBinding` entries filtered
by `engineId`.

## Persistence

Optional LiteDB for Forge session and macros: **`Nexo:ForgeSession:LiteDbPath`** (see
`docs/Persistence.md`). **`IForgeStateService`** is registered in **`Program.cs`**.

## GitHub Actions

If workflows are manual-only, align branch protection with **`.github/workflows/README.md`**.
