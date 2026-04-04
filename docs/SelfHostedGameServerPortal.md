# Self-Hosted Nexo Game Dev Server Portal

**Compose:** this page describes **`docker-compose.portal.yml`** (portal + API + Ollama, no default mounted-workspace agent cluster). For portal + **mounted repo** + background agents using Runtime Studio’s JSON, use **`docker-compose.agent-server.yml`** — `docs/SelfHostedAgentServer.md`. How those pieces relate: `apps/runtime-studio/README.md` → [How this fits](../apps/runtime-studio/README.md#how-runtime-studio-fits-with-nexo-api).

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

### Portal philosophy (personal software)

The Director UI is intentionally **personal and adaptive**: it assumes one human at a time, keeps **preferences in the browser** (local storage only), and lets you choose **what you need now** — shaping the next iteration, reviewing your trail of dailies, or exploring what this Nexo node reports about itself (`/api/status`, `/api/capabilities`). Palettes and greetings are for **your** comfort, not analytics.

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
