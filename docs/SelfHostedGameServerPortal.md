# Self-Hosted Nexo Game Dev Server Portal

This setup gives you a remote web portal for a **directorial workflow**:

1. Provide direction (`goal`) for the next iteration.
2. Nexo orchestrates generation/tasks.
3. Validation can run automatically.
4. Results are persisted as **dailies**.
5. You review and continue from a prior daily ID.

The portal is served by `Nexo.API` at `/` and uses:

- `POST /api/director/run`
- `GET /api/director/dailies`
- `GET /api/director/dailies/{dailyId}`

## 1) Start on your own hardware (Docker Compose)

From repo root:

```bash
docker compose -f docker-compose.portal.yml up -d --build
```

Check service health:

```bash
curl http://localhost:8080/api/status
```

Open the portal:

- Local: `http://localhost:8080`
- Remote LAN: `http://<your-server-ip>:8080`

## 2) Directorial iteration flow

In the portal:

1. Enter a **Goal / direction**.
2. Optional: add **Notes**.
3. Optional: provide `Continue from daily ID` for iterative continuation.
4. Leave **Run validation** enabled for automatic test pass/fail data.
5. Click **Run iteration**.

Each run creates a JSON daily file in `NEXO_DAILIES_PATH` (`/data/dailies` in compose).

## 3) Remote access and hardening

For public Internet access, put a reverse proxy + TLS in front of port `8080` and restrict source IPs where possible.

Suggested baseline:

- Keep `8080` private on your LAN/VPN.
- Publish only 443/TLS externally.
- Require VPN or zero-trust access for director review sessions.
- Back up the `nexo-dailies` Docker volume.

## 4) API quick examples

Create one daily:

```bash
curl -X POST http://localhost:8080/api/director/run \
  -H "Content-Type: application/json" \
  -d '{
    "goal":"Build and test a new combat tuning pass with higher encounter readability",
    "notes":"Prioritize minute-10 retention signals",
    "runValidation":true
  }'
```

List dailies:

```bash
curl http://localhost:8080/api/director/dailies
```

Continue from a prior daily:

```bash
curl -X POST http://localhost:8080/api/director/run \
  -H "Content-Type: application/json" \
  -d '{
    "goal":"Tighten dodge timing and rebalance stamina economy",
    "continueFromDailyId":"<daily-id>",
    "runValidation":true
  }'
```

## 5) Stop services

```bash
docker compose -f docker-compose.portal.yml down
```
