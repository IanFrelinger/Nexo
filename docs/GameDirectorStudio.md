# Ashlar Game Director Studio

Self-hosted, MCP-exposed AI sidecar for game studios. Monitors balance drift and map health, enforces audit trails on AI suggestions, and runs air-gapped with local Ollama.

## Architecture

- **Host**: `commercial/src/Ashlar.Commercial.GameDirector.Host` — `AddAshlarSdk` → `AddAshlar` → Ashlar API + Forge + `/mcp`
- **Bricks**: `balance.analysis`, `map.flow`, `content.generation`
- **Agents**: `balance-watcher`, `map-validator` (hosted services + JSON agent set)
- **Trust**: `internal-only` pack from `config/trust-packs/internal-only.json`

## Deploy

From the repo root:

```bash
export ASHLAR_API_KEY="$(openssl rand -hex 32)"   # required; compose refuses to start without it
docker compose -f deploy/compose/docker-compose.game-director.yml up -d --build
docker compose -f deploy/compose/docker-compose.game-director.yml exec ollama ollama pull llama3.1:latest
```

The repo is bind-mounted at `/work` by default (`ASHLAR_REPO_ROOT` defaults to `../..` relative to `deploy/compose/`, i.e. the repo root, whatever your shell CWD); set `ASHLAR_REPO_ROOT` only to mount another tree. `ASHLAR_API_KEY` is **required** — the host runs `AuthorizationMode=ApiKey` and neither the compose file nor `appsettings.json` ships a default key any more (running the host directly with `dotnet run` needs `Ashlar__Security__ApiKey` in the environment for the same reason).

## Cursor integration

Add to Cursor MCP settings (use the same value you exported as `ASHLAR_API_KEY`):

```json
{
  "mcpServers": {
    "ashlar-game-director": {
      "url": "http://127.0.0.1:8080/mcp",
      "headers": { "X-Ashlar-Api-Key": "<your ASHLAR_API_KEY>" }
    }
  }
}
```

### MCP tools

| Tool | Description |
|------|-------------|
| `analyze_balance` | Stat sheet outliers + TTK spread |
| `validate_map` | Choke density, spawn equity, recommendations |
| `generate_content` | Flavor/item/macro text from Forge session |
| `get_audit_trail` | Query decision records (30-day window) |
| `query_patterns` | Pattern store insights |

## Dogfood backlog surfaced

1. `IAshlarClient.QueryKnowledgeAsync` added; Forge session typed methods still missing
2. Composition registry placeholders exposed by full `BrickInterface` on custom bricks
3. Background agent framework lacks native file-watcher trigger (wrapper in `MapValidatorHostedService`)
4. Multi-asset Forge session via `GameProjectContext` metadata keys
5. Portal audit timeline UI lags `get_audit_trail` API
6. Integrator matrix — this vertical fills the game-studio row

## Local development

```bash
dotnet run --project commercial/src/Ashlar.Commercial.GameDirector.Host
curl -s http://127.0.0.1:8080/health
```

Drop balance JSON under `data/balance/` and `.mapconfig` under `data/maps/` to trigger watchers.
