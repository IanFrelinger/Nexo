# Nexo Game Director Studio

Self-hosted, MCP-exposed AI sidecar for game balance, map validation, and content generation.

## Quick start

```bash
# From repo root
docker compose -f docker-compose.game-director.yml up -d --build
docker compose -f docker-compose.game-director.yml exec ollama ollama pull llama3.1:latest

# Or run locally
dotnet run --project commercial/src/Nexo.Commercial.GameDirector.Host
```

Portal: http://127.0.0.1:8080/  
MCP endpoint: http://127.0.0.1:8080/mcp  
API key (dev): `game-director-dev-key` (header `X-Nexo-Api-Key`)

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
        "X-Nexo-Api-Key": "game-director-dev-key"
      }
    }
  }
}
```

See [docs/GameDirectorStudio.md](../../docs/GameDirectorStudio.md) for full operator guide.
