# Private single-tenant reference deployment (Phase 0.4)

This is the **smallest production-shaped** stack for Nexo Private pilots: one tenant, one API host, local Ollama, and entitlements driven from environment/config (no multi-tenant control plane).

## Prerequisites

- Docker and Docker Compose
- ~8 GB RAM for Ollama + API (model-dependent)

## Quick start

```bash
export OLLAMA_MODEL=llama3.1:latest
export NEXO_DEFAULT_TENANT_ID=acme-pilot
docker compose -f docker-compose.private-single-tenant.yml up --build -d
```

Wait for health:

```bash
curl -sS http://127.0.0.1:8080/health
curl -sS http://127.0.0.1:8080/api/status
```

## Tenant and entitlements

| Variable / config | Purpose |
|-------------------|---------|
| `NEXO_DEFAULT_TENANT_ID` | Default `X-Nexo-Tenant` when header omitted |
| `Nexo__Product__AllowedTenantIds__0` | Allow-list (single tenant for Private) |
| `Nexo__Entitlements__MaxCopilotSubmissionsPerHour` | Hourly copilot quota (`0` = unlimited) |
| `Nexo__Entitlements__DeploymentMode` | License/profile hint (`Private`) |
| `Nexo__Entitlements__Seats` / `MaxConcurrency` | Plan hooks (enforcement TBD) |

Example copilot call with explicit tenant:

```bash
curl -sS -X POST http://127.0.0.1:8080/api/copilot/task \
  -H 'Content-Type: application/json' \
  -H 'X-Nexo-Tenant: acme-pilot' \
  -d '{"task":"Summarize open PRs"}'
```

Usage summary (last 24h for resolved tenant):

```bash
curl -sS 'http://127.0.0.1:8080/api/usage/summary?hours=24' \
  -H 'X-Nexo-Tenant: acme-pilot'
```

## Volumes

| Volume | Mount | Data |
|--------|-------|------|
| `nexo-dailies` | `/data/dailies` | Director dailies / run artifacts |
| `nexo-copilot-data` | `/data/copilot` | Reserved for copilot persistence profiles |
| `ollama-models` | Ollama | Model weights |

## Upgrade / teardown

```bash
docker compose -f docker-compose.private-single-tenant.yml pull
docker compose -f docker-compose.private-single-tenant.yml up -d --build
docker compose -f docker-compose.private-single-tenant.yml down
```

For air-gapped installs, build images on a connected machine, `docker save`, transfer, and `docker load` on the target host.

## Related

- [`docs/ProductFleetImplementationRoadmap.md`](../ProductFleetImplementationRoadmap.md) — Phase 0 exit criteria
- [`docker-compose.portal.yml`](../../docker-compose.portal.yml) — portal + Ollama without Private entitlements defaults
