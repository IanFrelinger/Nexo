# Private backup and restore (Phase 1.4)

Target recovery objectives for pilot Private customers:

| Metric | Pilot default | Notes |
|--------|---------------|-------|
| **RPO** | 24 hours | Daily volume snapshots unless contract specifies otherwise |
| **RTO** | 4 hours | Single-tenant compose stack on comparable hardware |

## What to back up

| Asset | Location (reference compose) | Method |
|-------|---------------------------|--------|
| Dailies / run artifacts | Docker volume `nexo-dailies` | `docker run --rm -v nexo-dailies:/data -v $(pwd):/backup alpine tar czf /backup/nexo-dailies.tgz /data` |
| Copilot persistence | Docker volume `nexo-copilot-data` | Same pattern as dailies |
| LiteDB audit / pattern stores | Docker volume `nexo-state` (`NEXO_STATE_DIR=/data/state` in the reference images), or the host path from `Nexo:PatternStorePath` / agent config when set (`docs/Configuration.md`, "Runtime state") | Same pattern as dailies, or filesystem copy while API stopped |
| License file | `NEXO_LICENSE_FILE` or `Nexo:PrivateLicense:LicenseFilePath` | Secure copy to secrets vault |
| Configuration | Compose env + `appsettings` overrides | Version in git or sealed customer config repo |

## Backup procedure (reference stack)

```bash
docker compose -f deploy/compose/docker-compose.private-single-tenant.yml stop nexo-api
docker run --rm \
  -v nexo-dailies:/data/dailies:ro \
  -v nexo-copilot-data:/data/copilot:ro \
  -v "$(pwd)/backups:/backup" \
  alpine sh -c 'tar czf /backup/nexo-private-$(date -u +%Y%m%dT%H%M%SZ).tgz /data'
docker compose -f deploy/compose/docker-compose.private-single-tenant.yml start nexo-api
```

Store archives off-host (S3-compatible object storage or customer backup appliance). Encrypt at rest.

## Restore procedure

1. Provision a clean host with Docker.
2. Load images (`docker load` for air-gap) or pull pinned tags.
3. Recreate volumes and extract archive:

```bash
docker volume create nexo-dailies
docker volume create nexo-copilot-data
docker run --rm -v nexo-dailies:/data/dailies -v nexo-copilot-data:/data/copilot -v "$(pwd)/backups:/backup" alpine sh -c 'tar xzf /backup/<archive>.tgz -C /'
```

4. Restore license file and environment variables.
5. `docker compose -f deploy/compose/docker-compose.private-single-tenant.yml up -d`
6. Verify: `/health`, `/api/support/diagnostics`, and one read-only copilot task list call.

## Test cadence

Run a full restore drill **quarterly** for paying Private customers. Record actual RTO in the ticket and update the support page if it diverges from the stated target.
