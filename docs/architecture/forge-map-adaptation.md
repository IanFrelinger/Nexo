# Forge map adaptation and engine manifests

Runtime types live in **`Nexo.GameDomain`**; HTTP surface in **`Nexo.API`** (`ForgeEndpoints`).

## Map adaptation plan

`MapAdaptationPlanner.Plan(session, builtInCatalog)` returns a **`MapAdaptationPlan`**: effective
`MapRenderingProfile`, geometry strategy, ordered **`PipelineStages`**, and notes. It resolves the
active aesthetic from the server-scoped `aesthetic` setting when set, otherwise the first pack on
the session.

## Dry-run and live pipeline

`POST /api/forge/map/pipeline/run` accepts **`MapPipelineRunRequest`**:

- **`dryRun: true`** — **`MapPipelineDryRun`** simulates every stage (no network).
- **`dryRun: false`** — **`MapPipelineRunner`** executes bounded HTTP **`fetch_vector`** /
  **`fetch_terrain`** when **`VectorDataUrl`** / **`TerrainDataUrl`** are set. URLs must pass
  **`ForgeMapFetchUrlValidator`**: https (or http only if **`AllowInsecureMapFetch`**), no userinfo,
  DNS must not resolve to loopback/private/link-local, and the host must match
  **`AllowedMapFetchHosts`** unless **`AllowMapFetchWhenAllowedHostsEmpty`** is true (dev/tests).
  Responses are capped by **`MaxFetchResponseBytes`**. **`VectorMapPayloadInspector`** adds a
  lightweight format guess on **`fetch_vector`**.
- When **`EnableVectorIntelligence`** is true, **`IVectorMapIntelligenceService`** runs on fetched
  vector bytes. The default implementation is **`ModelAugmentedVectorMapIntelligenceService`**, which
  uses **`HeuristicVectorMapIntelligenceService`** and optionally **`IModel`** when
  **`EnableVectorModel`** is true (bounded prompt size and **`VectorModelTimeoutMs`**).

## Multi-tenant isolation

Send **`X-Forge-Tenant`** (configurable via **`Nexo:ForgeSession:TenantHeaderName`**) to isolate
Forge session and macro state per tenant. With LiteDB, each tenant gets a file under
`<base>-tenants/<tenant>/forge.db` next to the configured root path.

## Engine manifest

`GET /api/forge/engine/{engineId}/aesthetic-manifest` returns **`ForgeEngineManifestResponse`**
with JSON from **`EngineAestheticManifestBuilder`**, including `EngineSurfaceBinding` entries filtered
by `engineId`.

## Persistence

Optional LiteDB for Forge session and macros: **`Nexo:ForgeSession:LiteDbPath`** (see
`docs/Persistence.md`). **`IForgeStateService`** is **`TenantPartitionedForgeStateService`**
in **`Program.cs`** (per-tenant in-memory or per-tenant LiteDB files).

## GitHub Actions

If workflows are manual-only, align branch protection with **`.github/workflows/README.md`**.
