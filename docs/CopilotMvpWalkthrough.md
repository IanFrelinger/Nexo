# Copilot MVP: First-Success Walkthrough

This guide walks you through submitting your first coding task through the Nexo copilot, seeing the result with an auditable execution context, and verifying trust controls.

## Prerequisites

- .NET SDK 8.x (repo is pinned by `global.json`; the API project targets `net8.0`)
- Nexo repo cloned and built (`dotnet build application/src/Nexo.API/Nexo.API.csproj`)
- An LLM provider configured (or `NEXO_ALLOW_MOCK=1` for local testing without API keys)

## 1. Start the API

```bash
# With mock provider (no API keys needed):
NEXO_ALLOW_MOCK=1 dotnet run --project application/src/Nexo.API

# With Ollama:
OLLAMA_BASE_URL=http://localhost:11434 dotnet run --project application/src/Nexo.API

# With OpenAI:
OPENAI_API_KEY=sk-... dotnet run --project application/src/Nexo.API
```

The portal is available at `http://localhost:5000` (default Kestrel HTTP port). Docker compose stacks use port `8080` instead — see section 6.

## 2. Submit a Copilot Task (Web Portal)

1. Open `http://localhost:5000` in your browser.
2. If this is your first visit, the **setup wizard** will guide you through provider configuration.
3. In the **Quick chat** panel, type a coding task: `Analyze the security posture of this codebase`.
4. Click **Send prompt**.
5. The response includes:
   - The orchestrator's output (analysis, suggestions, code).
   - An **audit count** showing how many trust events were recorded.
   - A **trust pill** in the header showing the current trust boundary status.

## 3. Submit a Copilot Task (API)

```bash
curl -s http://localhost:5000/api/copilot/task \
  -H "Content-Type: application/json" \
  -d '{"task": "List the top 3 security findings in src/Nexo.Infrastructure"}' \
  | jq .
```

Response shape:
```json
{
  "taskId": "abc123...",
  "success": true,
  "summary": "...",
  "output": { ... },
  "isTrustPaused": false,
  "recentAudit": [ ... ]
}
```

The request body field is `task` (not `prompt`). Optional field: `auditCount` (default 25).

## 4. Review Audit Trail

```bash
# List recent copilot tasks:
curl -s http://localhost:5000/api/copilot/tasks | jq .

# View trust dashboard:
curl -s http://localhost:5000/api/trust/dashboard | jq .

# Or via CLI:
dotnet run --project application/src/Nexo.CLI -- trust audit --json
dotnet run --project application/src/Nexo.CLI -- trust dashboard
```

Note: CLI trust commands require `NEXO_TRUST_ENABLED=1` in the environment for full trust service registration.

## 5. Control Trust Boundary

```bash
# Apply strict enterprise policy pack:
dotnet run --project application/src/Nexo.CLI -- trust pack apply --id strict-enterprise

# Pause observation (halt data collection):
dotnet run --project application/src/Nexo.CLI -- trust pause

# Resume:
dotnet run --project application/src/Nexo.CLI -- trust resume

# View boundary status:
dotnet run --project application/src/Nexo.CLI -- trust boundary
```

## 6. Docker Compose Launch

```bash
docker compose -f docker-compose.agent-server.yml up -d
# Portal at http://localhost:8080 (compose maps to port 8080)
```

## 7. Activity Feed and Changelog

The portal includes two additional product surfaces:

- **Activity feed**: Shows recent background agent actions and audit events (auto-refreshes every 60s).
- **Changelog assistant**: Generate a summary of recent project changes from adaptation, pattern, and audit stores.

API endpoints (use port `5000` for native `dotnet run`, or `8080` for Docker/compose):
```bash
# Activity feed (last 24h):
curl -s http://localhost:5000/api/activity/feed | jq .

# Generate changelog (last 7 days):
curl -s http://localhost:5000/api/changelog/generate \
  -H "Content-Type: application/json" \
  -d '{"since": "2026-04-06"}' | jq .

# Health check:
curl -s http://localhost:5000/health | jq .
```

## Verification Checklist

- [ ] Task submitted via portal or API and output received
- [ ] Audit entries visible in response and/or trust dashboard
- [ ] Trust boundary status accessible
- [ ] Policy pack can be applied and takes effect
- [ ] Works with mock provider (no external API keys)
- [ ] Setup wizard appears on first visit (no prior copilot tasks)
