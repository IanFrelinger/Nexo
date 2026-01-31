# Networked Nexo Instances and Dynamic Brick Sharing

## Implementation Status (Phases 1–8)

Networked brick discovery and execution are implemented. A Nexo instance can expose a brick catalog and execute API; other instances can discover and run those bricks via `CompositeBrickRegistry` and `RemoteBrick`.

- **Catalog API**: `GET /api/bricks`, `GET /api/bricks/{id}` (Nexo.API `BricksController`).
- **Execute API**: `POST /api/bricks/{id}/execute` (request/response DTOs in Nexo.Brick.Contracts, namespace `Nexo.BrickContracts`).
- **Wire format**: DTOs and `BrickValueSerializer` (Nexo.Infrastructure) for BrickInput/BrickOutput with binary (base64) convention.
- **Remote brick proxy**: `RemoteBrick` (Nexo.Infrastructure) delegates `ExecuteAsync` to the execute API.
- **Composite registry**: `CompositeBrickRegistry` merges local `IBrickRegistry` with one or more `IRemoteBrickCatalog`; remote bricks are returned as `RemoteBrick`.
- **Configuration**: `BrickHostOptions` (section `BrickHost`) for remote catalog URLs and optional API key auth.
- **Central catalog (Phase 7)**: **Nexo.BrickCatalog** – deployable that aggregates brick catalogs from multiple instances (config-driven list of `InstanceBaseUrls`). Exposes GET /api/bricks and GET /api/bricks/{id}; each entry includes `HostBaseUrl` so callers execute on the correct instance. Callers can point `RemoteCatalogBaseUrls` to the central catalog URL only.

See [NETWORKED_BRICKS_IMPLEMENTATION_PLAN.md](NETWORKED_BRICKS_IMPLEMENTATION_PLAN.md) for the full plan.

---

## Previous State (Pre-Implementation)

### What Existed Before

1. **Brick registry is in-memory and local**
   - `IBrickRegistry` (Domain) / `BrickRegistry` (Infrastructure) holds a fixed set of `Brick` instances built at construction time.
   - `GetBrick(id)` and `GetAllBricks()` return only bricks that were passed into the registry constructor. There is no discovery from other processes or hosts.

2. **Bricks are executed in-process**
   - `WorkflowExecutor` and `BehaviorExecutor` resolve a brick via `IBrickRegistry.GetBrick(id)` and then call `brick.ExecuteAsync(...)` on the returned instance. Execution is always local; there is no RPC or HTTP call to another Nexo instance.

3. **No brick-related API**
   - Nexo.API exposes geospatial jobs (terrain, vector, world) and job status/webhooks. It does not expose brick catalog or brick execution endpoints. There is no protocol for “list bricks” or “run brick X with input Y” over the network.

4. **Registry is built per use in the CLI**
   - In GeoTerrain, GeoVector, and World commands, each invocation constructs a new `Brick[]` and `new BrickRegistry(bricks)` for that command. Bricks are not shared across commands or instances; they are local to the current process.

So: **no networked instances, no dynamic sharing of bricks.** All bricks are local and fixed at registration time.

---

## What Would Be Needed to Support Networked Bricks

To allow bricks to be “dynamically shared” between systems (e.g. instance A discovers and runs bricks hosted on instance B), you would add the following.

### 1. Discovery: How One Instance Learns About Remote Bricks

- **Brick catalog API**  
  Each Nexo instance (or a central catalog service) could expose something like:
  - `GET /bricks` – list brick metadata (Id, Name, Version, Category, Description, Interface schema, implementations).
  - Optional: `GET /bricks/{id}` – full metadata for one brick.

- **Registry that aggregates local + remote**  
  An `IBrickRegistry` implementation that:
  - Keeps the current in-memory/local bricks, and
  - Optionally queries one or more remote catalogs (or a discovery service) and treats “remote” bricks as proxies (see below).

- **Dynamic vs static**  
  “Dynamically shared” implies catalog endpoints are called at some interval or on demand so that new bricks (or new instances) appear without restarting the caller.

### 2. Remote Execution: Running a Brick on Another Instance

- **Execution API on the host**  
  The instance that owns the brick exposes an endpoint, e.g.:
  - `POST /bricks/{id}/execute`  
  - Body: serialized `BrickInput`, requested `ImplementationType`, and minimal execution context (e.g. correlation id, air-gapped flag).  
  - Response: serialized `BrickOutput` (and optionally `Summary`).

- **Wire format**  
  `BrickInput` and `BrickOutput` are dictionary-based (`string` → `object`). For RPC you need a stable serialization (e.g. JSON with a convention for binary blobs, or a schema per brick). This is feasible but must be defined so both sides agree.

- **Remote brick proxy**  
  A `Brick` implementation (e.g. `RemoteBrick` or `ProxyBrick`) that:
  - Has the same metadata (Id, Name, Interface, etc.) as the remote brick (from catalog).
  - Implements `ExecuteAsync` by serializing input, calling `POST /bricks/{id}/execute` on the appropriate host, and deserializing output into `BrickOutput`.

- **Registry returning proxies**  
  The same (or another) `IBrickRegistry` implementation would return:
  - Local bricks as today (concrete `Brick` instances).
  - Remote bricks as `RemoteBrick` (or similar) instances so that `WorkflowExecutor` and `BehaviorExecutor` keep calling `brick.ExecuteAsync(...)` without knowing whether the brick is local or remote.

### 3. Configuration and Routing

- **Per-brick or per-catalog base URL**  
  Configuration (e.g. appsettings or env) would specify where to find brick catalogs and where to execute remote bricks (e.g. base URL per Nexo instance, or a single gateway that routes by brick id).

- **Security and identity**  
  Authentication/authorization for catalog and execute endpoints (e.g. API key, OAuth, or mesh credentials) so that only allowed callers can list or run bricks.

### 4. Optional: Central Catalog

- A dedicated “brick catalog” service that:
  - Registers bricks from multiple Nexo instances (each instance calls “register my bricks” or the catalog scrapes instance `/bricks`).
  - Exposes `GET /bricks` (and optionally `GET /bricks/{id}`) for discovery.
  - Does not execute bricks; execution still goes to the instance that hosts the brick (or the catalog returns the instance URL so the caller can call that instance’s execute endpoint).

---

## Summary

| Capability                         | Today | To support networked bricks |
|------------------------------------|-------|-----------------------------|
| Brick discovery across instances   | No    | Brick catalog API + registry that queries it |
| Running a brick on another instance| No    | Execute API + RemoteBrick proxy + serialization |
| Dynamic sharing (new bricks/instances without restart) | No | Catalog/registry that refreshes or is queried on demand |
| Security (auth for catalog/execute) | N/A   | Add auth for new endpoints |

The **architecture** (interfaces like `IBrickRegistry`, abstract `Brick` with `ExecuteAsync`, dictionary-based `BrickInput`/`BrickOutput`) does not block this: you can add a remote registry and `RemoteBrick` without changing existing executors. The **current codebase** does not implement any of it; adding it would require the discovery API, execution API, serialization contract, and proxy implementation described above.

**Implementation plan**: [NETWORKED_BRICKS_IMPLEMENTATION_PLAN.md](NETWORKED_BRICKS_IMPLEMENTATION_PLAN.md). Phases 1–8 are implemented (including Phase 7 central catalog).
