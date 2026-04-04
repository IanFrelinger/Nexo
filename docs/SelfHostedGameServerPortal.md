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

### Basic checklist (avoid the biggest risks)

The Nexo API is **powerful and mostly unauthenticated by default** — anyone who can open the HTTP port can call `/api/director/run`, `/api/orchestrate`, etc. You do not need “enterprise security” for a home lab, but these steps avoid common foot-guns:

1. **Shrink who can reach the port** — Prefer `127.0.0.1` / SSH or VPN to the host. Use `-ListenLan` / `--listen-lan` only on **Wi‑Fi you trust**; never on café/hotel networks. For **Tailscale**, see **`docs/TailscaleAndNexo.md`** (ACLs + `Nexo__Security__ExposureProfile=Tailnet` so logs and the portal advisory match your intent).
2. **Firewall** — On Windows/macOS/Linux, do not add a blanket “allow 8080 from anywhere” rule. If you must expose the API on the LAN, restrict to the LAN subnet; **do not port-forward 8080 to the public internet** without something stronger (below).
3. **Internet-facing use** — Put **TLS + authentication** (reverse proxy, Cloudflare Tunnel, Tailscale Funnel with auth, etc.) in front of the app; expose **443** only, not raw `8080`.
4. **Docker `ports:`** — Publishing `8080:8080` listens on **all interfaces** on the host. For local-only, use a compose override such as `"127.0.0.1:8080:8080"` so phones/LAN cannot hit it unless you intend that.
5. **Mounted repo** — Agent-server style stacks mount your project **read/write**; agents can change files under policy. Use a **read-only** bind (`:ro`) in an override if you only want experimentation without writes.
6. **Secrets** — Keep `OPENAI_*`, `AZURE_*`, and similar keys in **environment or secret stores**, not in git. Prefer `.env` files that are **gitignored**.
7. **Backups** — Back up the **dailies** volume or directory (`NEXO_DAILIES_PATH`) if you care about history.
8. **Updates** — Periodically pull newer **Ollama** and **API** base images / rebuild, and patch the host OS.

### Beyond the home LAN

For public Internet access, put a reverse proxy + TLS in front of port `8080` and restrict source IPs where possible.

Suggested baseline:

- Keep `8080` private on your LAN/VPN.
- Publish only 443/TLS externally.
- Require VPN or zero-trust access for director review sessions.
- Back up the `nexo-dailies` Docker volume.

### Exposing Nexo on the public Internet

`Nexo.API` does **not** ship with login or API-key gates on `/` and `/api/*` today. Treat **any** Internet reachability as “full access to whatever Nexo can do on that host,” unless you add a **front door**.

**Recommended patterns (pick one):**

| Approach | Idea | Tradeoff |
|----------|------|----------|
| **Private network only** | **Tailscale**, **WireGuard**, **Tailscale subnet router**, corporate VPN. Nexo stays on `127.0.0.1` or a private IP; **no** public `8080`. | Best security/cost ratio for individuals; users need the VPN app. |
| **TLS + auth reverse proxy** | **Caddy**, **nginx**, **Traefik**, or **Envoy** terminates **HTTPS** on **443**, enforces **Basic Auth**, **OAuth2/OIDC**, or **mTLS**; proxies to `http://127.0.0.1:8080` only. | You operate certs (Let’s Encrypt via Caddy/nginx) and identity; Nexo stays unmodified. |
| **Managed edge + policy** | **Cloudflare Tunnel** (or similar) to origin; optional **Cloudflare Access** / **Zero Trust** so only allowed identities hit the hostname. | Hides origin IP; dependency on vendor; configure policies carefully. |

**Do not:**

- Port-forward **plain HTTP 8080** from your router to Nexo (no TLS, no auth).
- Publish **Ollama** (`11434`) or **Docker API** to the Internet.
- Run **`docker-compose.agent-server.yml` with a read/write repo mount** against the open Internet without strong edge controls — that is arbitrary-code / arbitrary-change territory.

**Operational extras that help:**

- **Rate limiting** and **request size limits** at the proxy.
- **Separate machine or VM** for Internet-facing Nexo from your main dev PC.
- **Backups** of dailies and any persisted state; **updates** for proxy, OS, and images.

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
