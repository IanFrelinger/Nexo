# Agent Development Guide

Nexo agents encapsulate higher-level automation workflows. Agents implement `Nexo.Abstractions.IAgent` and are registered via dependency injection.

## Creating a New Agent

1. **Implement the interface**
   ```csharp
   public class MyAgent : IAgent
   {
       public string Name => "my-agent";

       public Task<AgentResult> ExecuteAsync(WorldSnapshot snapshot, CancellationToken ct)
       {
           // register tool calls, orchestrate logic...
       }
   }
   ```

2. **Register the agent**
   ```csharp
   services.AddTransient<IAgent, MyAgent>();
   ```

3. **Run the agent**
   ```bash
   nexo agent --name my-agent --input ./payload.json
   ```

## Agent Input Files

- Optional `--input` parameter passed to `AgentExecutorAdapter`.
- Provide JSON blobs or domain-specific formats as required by the agent.
- Validate input paths inside the agent to provide clear error codes.

## Capabilities & Tool Registration

`AgentExecutorAdapter` automatically registers:
- `DotnetTestTool`
- `AssemblyAnalyzeTool`
- `AssemblySecurityScanTool`

Agents can request additional tools via constructor injection or by extending the adapter.

## Policies

The adapter enforces:
- Output sandboxing
- Perf headroom (default: 5 minutes)
- Optional allowlist/denylist policies

Update `PolicyEngine` configuration to add stricter rules.

## Agent Metadata

`IAgentRegistry` exposes metadata for CLI discovery:
- `Name`
- `Description`
- `Capabilities`
- `Parameters`

Populate attributes or override `GetAgentDescription`/`GetAgentCapabilities` in `AgentRegistryAdapter` to describe your agent accurately.

