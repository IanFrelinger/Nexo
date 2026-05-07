# Forge map adaptation and engine manifests

Runtime types live in **`Nexo.GameDomain`**; HTTP surface in **`Nexo.API`** (`ForgeEndpoints`).

## Map adaptation plan

`MapAdaptationPlanner.Plan(session, builtInCatalog)` returns a **`MapAdaptationPlan`**: effective
`MapRenderingProfile`, geometry strategy, ordered **`PipelineStages`**, and notes. It resolves the
active aesthetic from the server-scoped `aesthetic` setting when set, otherwise the first pack on
the session. The API uses **`BuiltInAestheticPacks.Catalog`** as the default built-in list.

## Dry-run and live pipeline

`POST /api/forge/map/pipeline/run` accepts **`MapPipelineRunRequest`**:

- **`dryRun: true`** — **`MapPipelineDryRun`** simulates every stage (no network).
- **`dryRun: false`** — **`MapPipelineRunner`** executes bounded HTTP **`fetch_vector`** /
  **`fetch_terrain`** when **`VectorDataUrl`** / **`TerrainDataUrl`** are set. URLs must pass
  **`ForgeMapFetchUrlValidator`**: https (or http only if **`AllowInsecureMapFetch`**), no userinfo,
  DNS must not resolve to loopback/private/link-local, and the host must match
  **`AllowedMapFetchHosts`** unless **`AllowMapFetchWhenAllowedHostsEmpty`** is true (dev/tests).
  The **`forge-map`** **`HttpClient`** uses **`SocketsHttpHandler.ConnectCallback`** so the TCP connection
  is opened only to addresses that pass the same private-range checks (mitigates DNS rebinding).
  Responses are capped by **`MaxFetchResponseBytes`**. **`VectorMapPayloadInspector`** adds a
  lightweight format guess on **`fetch_vector`**. When **`EnableVectorPayloadParsing`** is true (default),
  **`VectorMapPayloadSummarizer`** parses GeoJSON, OSM XML, or Mapbox MVT just far enough to record
  feature counts and layer names in the stage detail (full tessellation stays host-side). For MVT,
  **`VectorTileUrlParser`** reads **z/x/y** from common tile URLs (path segments or **`?z=&x=&y=`**); you can
  override with **`MvtTileX`** / **`MvtTileY`** plus **`MvtTileZoom`** when the URL has no tile indices.
  Non-fetch stages (**`resolve_*`**, **`emit_host_manifest`**, geometry hints) return actionable detail text;
  **`emit_host_manifest`** includes active aesthetic id and manifest size from **`EngineAestheticManifestBuilder`**.
- When **`EnableVectorIntelligence`** is true, **`IVectorMapIntelligenceService`** runs on fetched
  vector bytes. The default implementation is **`ModelAugmentedVectorMapIntelligenceService`**, which
  uses **`HeuristicVectorMapIntelligenceService`** and optionally **`IModel`** when
  **`EnableVectorModel`** is true (bounded prompt size and **`VectorModelTimeoutMs`**).

## LOD tile pyramid

`GET /api/forge/map/tile-pyramid?finestZoom=14` returns **`ForgeTilePyramidResponse`**: zoom steps per **`LodLevel`** for streaming/prefetch (see **`MapLodPyramidPlanner`**).

## Multi-tenant isolation

Send **`X-Forge-Tenant`** (configurable via **`Nexo:ForgeSession:TenantHeaderName`**) to isolate
Forge session and macro state per tenant. With LiteDB, each tenant gets a file under
`<base>-tenants/<tenant>/forge.db` next to the configured root path.

With **`BindTenantFromClaims`** and **`TenantClaimType`**, authenticated callers use the tenant id from
that claim. When claims binding is enabled, unauthenticated callers cannot fall back to the header unless
**`AllowTenantHeaderWhenClaimsBindingEnabled`** is true (otherwise **401**). Set **`RequireForgeAuthentication`**
to require Nexo built-in auth for all **`/api/forge`** routes.

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

## Host integration (engines)

See **`docs/architecture/forge-map-host-integration.md`** and the **`docs/samples/ForgeMapHostSample`** project for milestone M1–M4 wiring.
