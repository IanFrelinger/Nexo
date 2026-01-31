# Nexo Central Brick Catalog

Aggregates brick catalogs from multiple Nexo brick host instances into a single discovery endpoint. Callers can point to one URL (the central catalog) instead of configuring every instance URL.

## Configuration

Bind `CentralCatalog` in appsettings or environment:

| Setting | Description |
|---------|-------------|
| `CentralCatalog:InstanceBaseUrls` | List of brick host base URLs (e.g. `["http://localhost:5000", "https://nexo-b.example.com"]`). Each must expose `GET /api/bricks` and `GET /api/bricks/{id}`. |
| `CentralCatalog:CacheTtlSeconds` | Cache TTL for aggregated catalog (default 60). Use 0 to fetch on every request. |

Example `appsettings.json`:

```json
{
  "CentralCatalog": {
    "InstanceBaseUrls": ["http://localhost:5000", "http://localhost:5001"],
    "CacheTtlSeconds": 60
  }
}
```

## Endpoints

- **GET /api/bricks** – Returns aggregated list of `BrickCatalogEntryDto` from all instances. Each entry has `HostBaseUrl` set to the instance that provided it so callers know where to execute.
- **GET /api/bricks/{id}** – Returns one brick by id (first instance that advertises it).

Paths match the instance API so existing `HttpRemoteBrickCatalog` works when pointed at the central catalog base URL.

## Client use

Configure callers (e.g. `BrickHostOptions:RemoteCatalogBaseUrls`) with **only** the central catalog URL instead of many instance URLs. `CompositeBrickRegistry` and `HttpRemoteBrickCatalog` will call GET /api/bricks on the central catalog; the response includes `HostBaseUrl` per entry, so `RemoteBrick` executes on the correct instance.

## Run

```bash
dotnet run --project src/Nexo.BrickCatalog
```

Set `CentralCatalog:InstanceBaseUrls` (e.g. via appsettings or env) to the brick host instance URLs to aggregate.
