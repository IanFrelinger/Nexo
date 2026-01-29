# Background Agent Example Configurations

These JSON examples can be merged into your `appsettings.json` or loaded via a dedicated config file.

## Files

| File | Description |
|------|-------------|
| **minimal-agent.json** | Single agent with interval schedule; no RAG or web search. |
| **full-agent-with-rag-and-websearch.json** | Agent with RAG (SQLite vector store), web search (Bing), and exfiltration policy. |
| **air-gapped-deterministic.json** | Strict local-only agent for air-gapped or high-sensitivity environments. |
| **dogfood-optimizer.json** | **Dog-fooded** optimizer agent: runs the app's own code analysis on a path (e.g. `.`) on a schedule. See [DOGFOOD_OPTIMIZER.md](../DOGFOOD_OPTIMIZER.md). |
| **dogfood-tester.json** | **Dog-fooded** tester agent: runs the app's own test pipeline on a schedule. Optional `Parameters.Filter` for test filter. See [DOGFOOD_TESTER.md](../DOGFOOD_TESTER.md). |
| **dogfood-extender.json** | **Dog-fooded** extender agent: runs one self-extend cycle (LLM + repo.fs.write / search_replace) with path and size policy. See [DOGFOOD_EXTENDER.md](../DOGFOOD_EXTENDER.md). |

## Usage

### Option 1: Merge into appsettings.json

Copy the `BackgroundAgents` section from an example into your app's `appsettings.json`.

### Option 2: Dedicated config file

```bash
# Load from file (implementation-dependent; see your host configuration)
dotnet run --project src/Nexo.CLI -- background-agent list
```

Configure your host to add the JSON file:

```csharp
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("background-agents.json", optional: true)
    .Build();
```

### Option 3: Environment variables

Keys follow the pattern `BackgroundAgents:Agents:0:Id`, `BackgroundAgents:Agents:0:Name`, etc. For multiple agents, use index 1, 2, …

## Schedule types

- **Interval**: `"Schedule": { "Type": "Interval", "Interval": "00:05:00" }` — run every 5 minutes.
- **Cron**: `"Schedule": { "Type": "Cron", "CronExpression": "0 */6 * * *" }` — every 6 hours.
- **Continuous**: `"Schedule": { "Type": "Continuous" }` — run in a tight loop (use with care).

## Sensitivity levels

Built-in levels (least to most restrictive): **Public**, **Internal**, **Confidential**, **Secret**, **TopSecret**. Set `MaxDataSensitivity` per agent; RAG and exfiltration policies respect it.
