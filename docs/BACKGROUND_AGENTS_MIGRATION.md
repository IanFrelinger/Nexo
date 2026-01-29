# Background Agents: Migration and Integration Guide

This guide explains how to add optional background agents to an existing Nexo host (CLI, API, or custom app).

## Overview

Background agents are **optional**. You can:

- Run the CLI or API without any background agents.
- Add the `Nexo.BackgroundAgents` package and configuration when you want embedded agents.

No changes are required to existing orchestration or agent code unless you choose to use background agents.

## Step 1: Add the package

Reference the background agents project (or NuGet package when published):

```xml
<ItemGroup>
  <ProjectReference Include="..\Nexo.BackgroundAgents\Nexo.BackgroundAgents.csproj" />
</ItemGroup>
```

## Step 2: Register services

In your host's service configuration (e.g. `Program.cs` or `Startup.cs`):

```csharp
using Nexo.BackgroundAgents;

// Register background agent infrastructure
services.AddBackgroundAgents();

// Optional: register RAG and web search (for agents that use them)
services.AddBackgroundAgentsRAG();
```

For **CLI-only** usage (no hosted service), skip the background service:

```csharp
services.AddBackgroundAgents(registerHostedService: false);
```

This registers the registry, config loader, scheduler, and related services without starting the `BackgroundAgentService` that auto-starts agents on app startup.

## Step 3: Provide configuration

Background agents are configured under `BackgroundAgents:Agents` (array of agent configs).

### Option A: appsettings.json

Add a section to your existing `appsettings.json`:

```json
{
  "BackgroundAgents": {
    "Agents": [
      {
        "Id": "my-agent",
        "Name": "My Agent",
        "Role": "monitor",
        "Commands": ["ping"],
        "Schedule": { "Type": "Interval", "Interval": "00:05:00" },
        "Enabled": true,
        "MaxDataSensitivity": "Public"
      }
    ]
  }
}
```

### Option B: Dedicated JSON file

Add a separate file (e.g. `background-agents.json`) and load it in your configuration builder:

```csharp
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("background-agents.json", optional: true)
    .AddEnvironmentVariables()
    .Build();
```

### Option C: Environment variables

Use the standard `IConfiguration` key format, e.g.:

- `BackgroundAgents__Agents__0__Id=my-agent`
- `BackgroundAgents__Agents__0__Name=My Agent`
- `BackgroundAgents__Agents__0__Schedule__Type=Interval`
- `BackgroundAgents__Agents__0__Schedule__Interval=00:05:00`

## Step 4: Start the host

- If you used `AddBackgroundAgents()` (default), the hosted `BackgroundAgentService` starts with the app and loads config, registers agents, and starts those with `Enabled: true`.
- If you used `AddBackgroundAgents(registerHostedService: false)` (e.g. CLI), you control when agents run via CLI commands: `nexo background-agent start <id>`, `nexo background-agent execute --id <id>`, etc.

## Step 5: Use the CLI (optional)

When the CLI is used with `registerHostedService: false`, you can:

```bash
nexo background-agent list
nexo background-agent show my-agent
nexo background-agent start my-agent
nexo background-agent stop my-agent
nexo background-agent execute --id my-agent
nexo background-agent logs --id my-agent
nexo background-agent metrics --id my-agent
```

Sensitivity, RAG, and web search have their own subcommands; see [BACKGROUND_AGENTS_CLI_SPEC.md](BACKGROUND_AGENTS_CLI_SPEC.md).

## Data sensitivity and exfiltration

- Each agent has a `MaxDataSensitivity` (e.g. `Public`, `Internal`, `Confidential`). The framework uses this to restrict which data the agent can see and which tools (e.g. web search, external LLM) are allowed.
- You can override behavior with `ExfiltrationPolicy` on the agent config.
- Custom sensitivity levels can be defined and referenced; see the data sensitivity docs in [BACKGROUND_AGENTS_ARCHITECTURE.md](BACKGROUND_AGENTS_ARCHITECTURE.md).

## RAG and web search

- To give agents RAG or web search, call `AddBackgroundAgentsRAG()` and configure `RAG` and/or `WebSearch` on the agent config. Provide a vector store (e.g. SQLite path) and/or search provider (e.g. Bing API key) as required.
- Without `AddBackgroundAgentsRAG()`, RAG and web search tools are not registered; agents that reference them in config will still run but those tools won’t be available.

## Troubleshooting

| Issue | Check |
|-------|--------|
| Agents not starting | Ensure `Enabled: true` and schedule is valid (e.g. `Interval` set for `Interval` type). |
| "Unknown sensitivity level" | Use a built-in level (Public, Internal, Confidential, Secret, TopSecret) or register a custom one. |
| Config not loaded | Ensure the config section `BackgroundAgents:Agents` is present and bound (e.g. correct JSON structure). |
| CLI "agent not found" | Ensure the agent is registered (config loaded) and, for start/stop, that you’re using the correct agent `Id`. |

For more detail on configuration and architecture, see [BACKGROUND_AGENTS_ARCHITECTURE.md](BACKGROUND_AGENTS_ARCHITECTURE.md) and [docs/background-agents/examples/](background-agents/examples/).
