# Nexo Game Director Studio

Self-hosted, MCP-exposed AI sidecar for game balance, map validation, and content generation.

## Quick start

```bash
# From repo root (the repo is mounted at /work by default: NEXO_REPO_ROOT defaults to ../..
# relative to deploy/compose/, i.e. the repo root; set it only to mount another tree)
export NEXO_API_KEY="$(openssl rand -hex 32)"   # required: no dev key is shipped
docker compose -f deploy/compose/docker-compose.game-director.yml up -d --build
docker compose -f deploy/compose/docker-compose.game-director.yml exec ollama ollama pull llama3.1:latest

# Or run locally (the host runs AuthorizationMode=ApiKey, so it needs a key too)
Nexo__Security__ApiKey="$NEXO_API_KEY" dotnet run --project commercial/src/Nexo.Commercial.GameDirector.Host
```

Portal: http://127.0.0.1:8080/  
MCP endpoint: http://127.0.0.1:8080/mcp  
API key: the value you set as `NEXO_API_KEY` (header `X-Nexo-Api-Key`)

## Watched paths

- `data/balance/*.json` — balance sheets (see `data/balance/halo_weapons.sample.json`)
- `data/maps/**/*.mapconfig` — map configs

## Cursor MCP config

```json
{
  "mcpServers": {
    "nexo-game-director": {
      "url": "http://127.0.0.1:8080/mcp",
      "headers": {
        "X-Nexo-Api-Key": "<your NEXO_API_KEY>"
      }
    }
  }
}
```

See [docs/GameDirectorStudio.md](../../docs/GameDirectorStudio.md) for full operator guide.
