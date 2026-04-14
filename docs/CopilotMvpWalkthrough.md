# Copilot MVP: First-Success Walkthrough

This guide walks you through submitting your first coding task through the Nexo copilot, seeing the result with an auditable execution context, and verifying trust controls.

## Prerequisites

- .NET 8+ SDK installed
- Nexo repo cloned and built (`dotnet build src/Nexo.API/Nexo.API.csproj`)
- An LLM provider configured (or `NEXO_ALLOW_MOCK=1` for local testing)

## 1. Start the API

```bash
# With mock provider (no API keys needed):
NEXO_ALLOW_MOCK=1 dotnet run --project src/Nexo.API

# With Ollama:
OLLAMA_BASE_URL=http://localhost:11434 dotnet run --project src/Nexo.API

# With OpenAI:
OPENAI_API_KEY=sk-... dotnet run --project src/Nexo.API
```

The portal is available at `http://localhost:5000`.

## 2. Submit a Copilot Task (Web Portal)

1. Open `http://localhost:5000` in your browser.
2. In the **Quick chat** panel, type a coding task: `Analyze the security posture of this codebase`.
3. Click **Run** (or press Enter).
4. The response includes:
   - The orchestrator's output (analysis, suggestions, code).
   - An **audit count** showing how many trust events were recorded.
   - A **trust pill** indicating the current trust boundary status.

## 3. Submit a Copilot Task (API)

```bash
curl -s http://localhost:5000/api/copilot/task \
  -H "Content-Type: application/json" \
  -d '{"prompt": "List the top 3 security findings in src/Nexo.Infrastructure"}' \
  | jq .
```

Response includes:
```json
{
  "taskId": "...",
  "output": "...",
  "status": "Completed",
  "recentAudit": [ ... ],
  "trustBoundaryActive": true
}
```

## 4. Review Audit Trail

```bash
# List recent copilot tasks:
curl -s http://localhost:5000/api/copilot/tasks | jq .

# View trust dashboard:
curl -s http://localhost:5000/api/trust/dashboard | jq .

# Or via CLI:
dotnet run --project src/Nexo.CLI -- trust audit --json
dotnet run --project src/Nexo.CLI -- trust dashboard
```

## 5. Control Trust Boundary

```bash
# Apply strict enterprise policy pack:
dotnet run --project src/Nexo.CLI -- trust pack apply --id strict-enterprise

# Pause observation (halt data collection):
dotnet run --project src/Nexo.CLI -- trust pause

# Resume:
dotnet run --project src/Nexo.CLI -- trust resume

# View boundary status:
dotnet run --project src/Nexo.CLI -- trust boundary
```

## 6. Docker Compose Launch

```bash
docker compose -f docker-compose.agent-server.yml up -d
# Portal at http://localhost:8080
```

## Verification Checklist

- [ ] Task submitted and output received
- [ ] Audit entries visible in response and/or trust dashboard
- [ ] Trust boundary status accessible
- [ ] Policy pack can be applied and takes effect
- [ ] Works with mock provider (no external API keys)
