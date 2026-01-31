# Implementation Plan: Networked Nexo Instances and Dynamic Brick Sharing

This plan implements discovery and execution of bricks across Nexo instances so that bricks can be dynamically shared between systems. It is based on [NETWORKED_BRICKS.md](NETWORKED_BRICKS.md).

---

## Goals

- **Discovery**: Any Nexo instance can list brick metadata (catalog) from one or more remote instances (or a central catalog).
- **Execution**: A caller can run a brick on a remote instance via a well-defined execute API; executors (WorkflowExecutor, BehaviorExecutor) remain unchanged.
- **Dynamic sharing**: New bricks or new instances can appear without restarting the caller (on-demand or periodic catalog refresh).
- **Incremental**: Existing in-process usage stays as-is; remote is opt-in via configuration and registry implementation.

---

## Phases Overview

| Phase | Focus | Deliverables |
|-------|--------|---------------|
| 1 | Wire format & serialization | DTOs, serialization for BrickInput/BrickOutput/context |
| 2 | Brick catalog API | GET /bricks, GET /bricks/{id} on a brick host |
| 3 | Brick execute API | POST /bricks/{id}/execute on same host |
| 4 | Remote brick proxy | RemoteBrick class + HTTP client |
| 5 | Composite brick registry | IBrickRegistry that merges local + remote catalogs |
| 6 | Configuration & auth | Config model, optional auth for catalog/execute |
| 7 | (Optional) Central catalog | Service that aggregates multiple instance catalogs |
| 8 | Tests & documentation | Integration tests, docs, and CLI/API wiring |

---

## Phase 1: Wire Format and Serialization

**Objective**: Define a stable, versioned wire format for brick metadata, execute request/response, and execution context so all instances can interoperate.

### 1.1 Shared contracts project

- **New project**: `Nexo.Brick.Contracts` (or `Nexo.Abstractions.Bricks`).
  - References: `Nexo.Core.Domain` (for BrickCategory, ImplementationType, and alignment with existing types).
  - Purpose: DTOs and serialization used by both brick host (API) and brick client (caller). No business logic.

### 1.2 Catalog DTOs

- **BrickCatalogEntryDto**: Id, Name, Version, Icon, Category (string or int), Description, HostBaseUrl (optional; for direct execution URL), Interface (inputs/outputs definitions), Implementations (deterministic/agentic flags or summary), Metadata (author, license, etc.).
- **BrickCatalogResponseDto**: List of BrickCatalogEntryDto, optional ContinuationToken for paging.
- All fields must be JSON-serializable; avoid circular refs. Use simple types and small nested DTOs (e.g. BrickInputDefinitionDto, BrickOutputDefinitionDto) that mirror Domain types.

### 1.3 Execute request/response DTOs

- **BrickExecuteRequestDto**:
  - BrickId (string)
  - Implementation (string: "Deterministic" | "Agentic")
  - Input: dictionary or key-value list (serializable representation of BrickInput)
  - ExecutionContext: CorrelationId, AgentId, BehaviorId, IsAirGapped, AuditMode, Provider, Variables (serializable)
- **BrickExecuteResponseDto**:
  - Success (bool)
  - Summary (string, optional)
  - Output: dictionary or key-value list (serializable representation of BrickOutput)
  - Error (string or code, optional, when Success is false)

### 1.4 Serialization for BrickInput / BrickOutput / IExecutionContext

- **BrickInput** and **BrickOutput** are `Dictionary<string, object>`. Values can be primitives, strings, byte arrays, or nested structures.
- Define a **BrickValueSerializer** (in Contracts or Infrastructure):
  - **Serialize**: BrickInput/BrickOutput → JSON-friendly structure. Convention for binary: base64-encode byte[] and tag with a type hint (e.g. `{"__type":"bytes","base64":"..."}`) so deserializer can reconstruct.
  - **Deserialize**: JSON → BrickInput/BrickOutput (or DTO that host can map back to BrickInput/BrickOutput).
- Execution context: map IExecutionContext to a small DTO (CorrelationId, AgentId, BehaviorId, IsAirGapped, AuditMode, Provider, Variables as key-value). Host builds a concrete context from this for Brick.ExecuteAsync.

### 1.5 Versioning

- Add **ApiVersion** or **WireFormatVersion** (e.g. "2025-01") to catalog and execute payloads so future changes can be backward-compatible.

**Exit criteria**: Contracts package builds; unit tests serialize/deserialize sample BrickInput and BrickOutput (including a byte[] value) and round-trip execute request/response DTOs.

---

## Phase 2: Brick Catalog API

**Objective**: A Nexo instance that hosts bricks exposes a catalog API so other instances can discover available bricks.

### 2.1 Brick host capability in Nexo.API

- **Option A (recommended for first cut)**: Add brick hosting to existing Nexo.API.
  - Register an **IBrickRegistry** in the API host with a fixed set of bricks (start with a small subset; see 2.2).
  - New **BricksController** (or BrickCatalogController):
    - `GET /api/bricks` → returns BrickCatalogResponseDto (list all bricks from registry).
    - `GET /api/bricks/{id}` → returns BrickCatalogEntryDto for one brick or 404.
  - Controller reads from IBrickRegistry.GetAllBricks() / GetBrick(id), maps each Brick to BrickCatalogEntryDto using the Contracts DTOs.
- **Option B**: New project **Nexo.BrickHost** (ASP.NET Core minimal API or MVC) that only exposes brick catalog and execute; can be run standalone or composed later. Prefer Option A to avoid extra deployable until needed.

### 2.2 Registering bricks in the API host

- API must construct real Brick instances (same as CLI) so that execute (Phase 3) can run them. That requires:
  - References from Nexo.API to Nexo.GeoTerrain.Bricks, Nexo.GeoVector.Bricks, Nexo.GeoWorld.Bricks (and any other brick packages).
  - Registration of dependencies (elevation provider, vector provider, IProviderFactory, ILoopKernel, loggers, etc.) already present or added to API host.
  - A **BrickRegistryBuilder** or **AddNexoBrickHost** extension that:
    - Builds a list of Brick instances (e.g. one GeoTerrain brick, one GeoVector brick, or a minimal set for smoke test).
    - Registers **IBrickRegistry** as singleton (or scoped) with that list.
- **Incremental**: Start with 1–2 bricks (e.g. GeoTerrainFetchSrtmTileBrick + GeoTerrainObjFromMeshBrick) to validate the pipeline; expand to full set in a follow-up.

### 2.3 OpenAPI

- Document GET /api/bricks and GET /api/bricks/{id} in Swagger; use response types from Contracts so client generators stay aligned.

**Exit criteria**: Running Nexo.API with brick host enabled; GET /api/bricks returns at least one brick; GET /api/bricks/{id} returns that brick’s metadata.

---

## Phase 3: Brick Execute API

**Objective**: The same brick host exposes an endpoint to execute a brick by id with serialized input and context; returns serialized output.

### 3.1 Execute endpoint

- **BricksController** (or same controller as catalog):
  - `POST /api/bricks/{id}/execute`
  - Body: BrickExecuteRequestDto (JSON).
  - Behavior:
    1. Resolve brick by id from IBrickRegistry.GetBrick(id). If null → 404.
    2. Deserialize request body to BrickExecuteRequestDto; map to BrickInput and execution context (using BrickValueSerializer and context DTO).
    3. Call brick.ExecuteAsync(input, implementationType, context, cancellationToken).
    4. Map BrickOutput and result to BrickExecuteResponseDto; return 200 with JSON body.
  - Errors: 400 (bad request), 404 (brick not found), 500 (execution failure); include error detail in BrickExecuteResponseDto or problem details.

### 3.2 Execution context on the host

- Create a small **ExecutionContextDto** → **IExecutionContext** adapter (or a concrete ExecutionContext type that implements IExecutionContext) so the host can pass a valid context into Brick.ExecuteAsync. Use CorrelationId, IsAirGapped, AuditMode, Provider, Variables from the DTO.

### 3.3 Timeouts and cancellation

- Honor request cancellation (CancellationToken from HTTP request). Optionally enforce a max duration per execute (e.g. configurable timeout) to avoid long-running requests holding connections.

**Exit criteria**: POST /api/bricks/{id}/execute with valid request body runs the brick on the host and returns BrickExecuteResponseDto with correct output; invalid id returns 404; invalid body returns 400.

---

## Phase 4: Remote Brick Proxy

**Objective**: Caller-side Brick implementation that delegates ExecuteAsync to the remote execute API so WorkflowExecutor and BehaviorExecutor can use remote bricks without code changes.

### 4.1 RemoteBrick class

- **Location**: Nexo.Infrastructure (or new Nexo.Adapters.BrickClient if you want to keep Infrastructure free of HTTP).
  - **RemoteBrick** extends **Brick** (Core.Domain).
  - Constructor (or factory) parameters: Id, Name, Version, Category, Description, Interface, Implementations, Metadata (from catalog), and **executeBaseUrl** (base URL of the host that serves POST /api/bricks/{id}/execute).
  - Override **ExecuteAsync**: serialize BrickInput and context to BrickExecuteRequestDto, POST to executeBaseUrl + "/api/bricks/" + Id + "/execute", deserialize BrickExecuteResponseDto to BrickOutput (and Summary); on failure throw or return error according to policy.

### 4.2 HTTP client

- Use **IHttpClientFactory** (or HttpClient) with a named or typed client for “brick host” calls. Configure base address from executeBaseUrl; add optional auth headers (Phase 6).
- **IBrickHostClient** interface (optional): ExecuteAsync(brickId, requestDto, ct) → BrickExecuteResponseDto. Implementation calls HTTP. This keeps RemoteBrick focused on mapping Domain types to DTOs and back.

### 4.3 Error handling and retries

- Map HTTP errors (timeout, 5xx) to exceptions or to a failed BrickOutput; optionally use Polly for retry (transient only). Document behavior in NETWORKED_BRICKS.md.

**Exit criteria**: Unit test: RemoteBrick with a mock HTTP handler that returns a successful BrickExecuteResponseDto; ExecuteAsync returns BrickOutput that matches the mock. No change to WorkflowExecutor or BehaviorExecutor yet.

---

## Phase 5: Composite Brick Registry

**Objective**: An IBrickRegistry implementation that merges local bricks with bricks discovered from one or more remote catalogs; returns RemoteBrick instances for remote bricks so executors see a unified set of bricks.

### 5.1 CompositeBrickRegistry

- **Location**: Nexo.Infrastructure.
  - **CompositeBrickRegistry** implements **IBrickRegistry** (use the Domain interface from Nexo.Core.Domain.Execution so Application/WorkflowExecutor stays on Domain).
  - Dependencies: local **IBrickRegistry** (or IEnumerable<Brick>), and one or more **IRemoteBrickCatalog** (or a single service that aggregates multiple catalogs).
  - **GetBrick(id)**:
    1. Try local registry first; if found, return that Brick.
    2. Else query remote catalog(s) for brick with id; if found, return new RemoteBrick(metadata, executeBaseUrl) (or get from a cache; see 5.2).
  - **GetAllBricks()**: union of local GetAllBricks() and remote catalog entries converted to RemoteBrick (dedupe by id; local wins if both have same id).

### 5.2 Remote catalog client

- **IRemoteBrickCatalog**: interface with GetBrickMetadata(id) and GetAllBrickMetadata() returning catalog DTOs (or BrickCatalogEntryDto). Implementation does GET /api/bricks and GET /api/bricks/{id} against a configured base URL.
- **Caching**: To support “dynamic” sharing without hammering the catalog, add a short TTL cache (e.g. in-memory, 30–60 seconds) for catalog responses. Optional: background refresh so first request after TTL doesn’t block on network.

### 5.3 Configuration for remote catalog URLs

- **BrickHostOptions** (or similar): list of BaseUrls for brick catalogs (e.g. "https://nexo-instance-a.example.com", "https://nexo-instance-b.example.com"). CompositeBrickRegistry (or a factory) uses these to build IRemoteBrickCatalog instances. Execute base URL: if catalog entry includes HostBaseUrl, use that for RemoteBrick; else use the same base URL as the catalog (catalog and execute on same host).

**Exit criteria**: Integration test: local registry with one brick; one remote catalog (e.g. Nexo.API with brick host) with one brick. CompositeBrickRegistry.GetAllBricks() returns both; GetBrick(remoteId) returns a RemoteBrick; calling ExecuteAsync on that RemoteBrick triggers POST to the API and returns correct output.

---

## Phase 6: Configuration and Auth

**Objective**: Make remote catalog and execute URLs configurable; optionally secure catalog and execute endpoints.

### 6.1 Configuration model

- **BrickHostOptions** (or **NetworkedBricksOptions**):
  - RemoteCatalogBaseUrls: string[]
  - DefaultExecuteBaseUrl (optional; used when catalog entry has no HostBaseUrl)
  - CatalogCacheTtlSeconds (optional)
  - UseAuth (bool), ApiKeyHeader, ApiKeyValue (or use existing API key middleware)
- Bind from appsettings (e.g. "BrickHost") or environment variables. Register in DI; inject into CompositeBrickRegistry and RemoteBrick (or IBrickHostClient).

### 6.2 Auth for catalog and execute

- **Catalog**: If UseAuth is true, add ApiKey (or Bearer) header to GET /api/bricks requests. Reuse existing API key middleware on Nexo.API for /api/bricks if desired.
- **Execute**: Same header for POST /api/bricks/{id}/execute. Ensure Nexo.API’s existing auth applies to the new brick endpoints (or add a dedicated scheme for brick host).
- Document in CONFIGURATION_GUIDE.md and NETWORKED_BRICKS.md.

**Exit criteria**: Configure two base URLs in appsettings; CompositeBrickRegistry discovers bricks from both. With auth enabled, requests without key are 401; with key, 200.

---

## Phase 7: (Optional) Central Catalog Service

**Objective**: A single service that aggregates brick metadata from multiple Nexo instances so callers can discover all bricks from one endpoint without configuring every instance URL.

### 7.1 Central catalog API

- New deployable (e.g. **Nexo.BrickCatalog**): minimal API or MVC.
  - **GET /bricks**: returns aggregated list of BrickCatalogEntryDto from all registered instance catalogs. Each entry should include HostBaseUrl (or InstanceId) so the client can call the correct host for execute.
  - **GET /bricks/{id}**: returns one brick’s metadata; if multiple instances advertise the same id, return one (e.g. first) or document resolution strategy (e.g. by instance priority).

### 7.2 Registration of instances

- **Option A**: Config-driven. Central catalog has a list of instance base URLs; on startup or on a schedule, it calls GET /api/bricks on each and merges results.
- **Option B**: Push. Each brick host calls POST /catalog/register with its base URL and list of brick ids (or full metadata); central catalog stores and serves merged view.
- Start with Option A for simplicity.

### 7.3 Client use

- Callers configure a single “central catalog” URL instead of many instance URLs. CompositeBrickRegistry (or a new CentralCatalogBrickRegistry) talks to GET /bricks and GET /bricks/{id}; execute still goes to HostBaseUrl per brick.

**Exit criteria**: Central catalog deployed; two brick hosts registered; GET /bricks returns bricks from both; caller using only central catalog URL can discover and execute bricks on both hosts.

---

## Phase 8: Tests and Documentation

**Objective**: Automated tests and clear docs so the feature is safe to evolve and operators know how to use it.

### 8.1 Unit tests

- Contracts: serialization/deserialization of BrickInput, BrickOutput, BrickExecuteRequestDto, BrickExecuteResponseDto (including binary value).
- RemoteBrick: mock HTTP; verify request shape and response mapping.
- CompositeBrickRegistry: mock local registry + mock remote catalog; verify GetBrick/GetAllBricks merge and return RemoteBrick for remote ids.

### 8.2 Integration tests

- Nexo.API (or BrickHost): start host with one brick registered; GET /api/bricks and POST /api/bricks/{id}/execute with real brick; assert response.
- End-to-end: start API with brick host; run a test that uses CompositeBrickRegistry (with API base URL as remote catalog), resolve a remote brick, ExecuteAsync, and assert output. Use TestServer or real HTTP.

### 8.3 Documentation updates

- **NETWORKED_BRICKS.md**: add “Implementation status” section; link to this plan; document config keys, auth, and wire format version.
- **CONFIGURATION_GUIDE.md**: add BrickHost / NetworkedBricks section (remote catalog URLs, auth, cache TTL).
- **API_REFERENCE.md** (or OpenAPI): document GET /api/bricks, GET /api/bricks/{id}, POST /api/bricks/{id}/execute (request/response schemas from Contracts).
- **docs/architecture.md**: short subsection on “Networked bricks” (discovery + remote execute, composite registry).

### 8.4 CLI / API wiring (optional)

- **CLI**: Add a flag or config so the GeoTerrain/GeoVector/World commands (or a new “workflow” command) can use CompositeBrickRegistry when remote catalog URLs are configured; otherwise keep current local-only BrickRegistry. This allows dynamic brick sharing from the CLI without changing default behavior.
- **Nexo.API**: When running as brick host, ensure bricks are registered and catalog/execute endpoints are enabled; document in README or QUICK_START.

**Exit criteria**: All new tests pass; docs updated; a reader can configure and run a remote brick from the CLI or from another service using the docs.

---

## Dependencies Between Phases

```
Phase 1 (Wire format) ─────────────────────────────────────────────────────────┐
       │                                                                        │
       ├──► Phase 2 (Catalog API) ──► Phase 3 (Execute API)                     │
       │           │                         │                                  │
       │           │                         ├──► Phase 4 (RemoteBrick)          │
       │           │                         │            │                     │
       │           └─────────────────────────┴────────────┼──► Phase 5 (Composite registry)
       │                                                  │            │
       │                                                  │            ├──► Phase 6 (Config & auth)
       │                                                  │            │
       └──────────────────────────────────────────────────┴────────────┴──► Phase 8 (Tests & docs)
                                                                        │
                                                          Phase 7 (Central catalog) [optional]
                                                                        │
                                                                        └──► Phase 8
```

---

## Project and Package Summary

| Item | Type | Purpose |
|------|------|---------|
| Nexo.Brick.Contracts | New project | DTOs and wire format for catalog + execute |
| Nexo.API | Existing | Add BricksController, brick registration, catalog + execute endpoints |
| Nexo.Infrastructure | Existing | BrickValueSerializer, RemoteBrick, CompositeBrickRegistry, IRemoteBrickCatalog impl |
| Nexo.Core.Domain | Existing | No change; IBrickRegistry, Brick, BrickInput, BrickOutput, IExecutionContext unchanged |
| Nexo.Core.Application | Existing | No change; WorkflowExecutor, BehaviorExecutor use IBrickRegistry as today |
| Nexo.BrickCatalog | New (optional) | Central catalog service in Phase 7 |

---

## Risk and Mitigation

- **Serialization of arbitrary object in BrickInput/BrickOutput**: Some bricks may pass non-JSON-friendly types (e.g. streams, custom types). Mitigation: define a clear convention (e.g. primitives + byte[] as base64); document limits; extend convention later for specific types if needed.
- **Version skew**: Old client vs new host (or vice versa). Mitigation: WireFormatVersion in payloads; host and client check version and return clear error or fallback behavior.
- **Latency and failures**: Remote execute is slower and can fail (network, timeout). Mitigation: timeouts, retries (transient only), and clear errors; document in NETWORKED_BRICKS.md so workflow authors can handle failures (e.g. fallback behavior or user message).

---

## Success Criteria (Overall)

1. A Nexo instance can expose a brick catalog (GET /api/bricks) and execute (POST /api/bricks/{id}/execute). **Done** (Nexo.API BricksController).
2. Another instance (or CLI) can configure one or more remote catalog URLs and use CompositeBrickRegistry to get a unified brick set; remote bricks are executed via HTTP without changing WorkflowExecutor or BehaviorExecutor. **Done** (CompositeBrickRegistry, RemoteBrick, IRemoteBrickCatalog).
3. New bricks or new instances appear for callers after catalog refresh (cache TTL or on-demand) without restart. **Done** (on-demand catalog query in GetBrick/GetAllBricks).
4. Configuration and optional auth are documented and tested. **Done** (BrickHostOptions, CONFIGURATION_GUIDE, BrickValueSerializerTests).

**Implementation status:** Phases 1–8 are implemented. Phase 7 central catalog: **Nexo.BrickCatalog** (config-driven aggregation from `CentralCatalog:InstanceBaseUrls`; GET /api/bricks and GET /api/bricks/{id}; entries include HostBaseUrl for execute).
