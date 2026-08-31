# Ashlar Game Director Studio

Self-hosted, MCP-exposed AI sidecar for game balance, map validation, and content generation.

## Quick start

```bash
# From repo root (the repo is mounted at /work by default: ASHLAR_REPO_ROOT defaults to ../..
# relative to deploy/compose/, i.e. the repo root; set it only to mount another tree)
export ASHLAR_API_KEY="$(openssl rand -hex 32)"   # required: no dev key is shipped
docker compose -f deploy/compose/docker-compose.game-director.yml up -d --build
docker compose -f deploy/compose/docker-compose.game-director.yml exec ollama ollama pull llama3.1:latest

# Or run locally (the host runs AuthorizationMode=ApiKey, so it needs a key too)
Ashlar__Security__ApiKey="$ASHLAR_API_KEY" dotnet run --project commercial/src/Ashlar.Commercial.GameDirector.Host
```

Portal: http://127.0.0.1:8080/  
MCP endpoint: http://127.0.0.1:8080/mcp  
API key: the value you set as `ASHLAR_API_KEY` (header `X-Ashlar-Api-Key`)

## Watched paths

- `data/balance/*.json` — balance sheets (see `data/balance/halo_weapons.sample.json`)
- `data/maps/**/*.mapconfig` — map configs

## Cursor MCP config

```json
{
  "mcpServers": {
    "ashlar-game-director": {
      "url": "http://127.0.0.1:8080/mcp",
      "headers": {
        "X-Ashlar-Api-Key": "<your ASHLAR_API_KEY>"
      }
    }
  }
}
```

See [docs/GameDirectorStudio.md](../../docs/GameDirectorStudio.md) for full operator guide.

## Local run mode (agent set)

A local, compose-free run mode drives the game-director agent set directly through the
open `background-agent` daemon:

- config: [`config/agent_set.game_director.local.json`](config/agent_set.game_director.local.json)
- launcher: [`scripts/run_game_director_local.sh`](scripts/run_game_director_local.sh)

```bash
bash apps/runtime-studio/scripts/bootstrap_runtime_studio.sh   # shared open bootstrap
bash apps/game-director/scripts/run_game_director_local.sh --duration 10m --disable-observation
```

Both files moved here from `apps/runtime-studio/` on 2026-08-31, when `runtime-studio`
graduated to the open tier: Game Director material is commercial (see `/LICENSING.md`),
so it lives with the vertical, not in an open app.
