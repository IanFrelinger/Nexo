# Dog-food: Self-extending background agent

Background agents with **Role: `extender`** run a self-extend cycle: an LLM-backed agent decides which tools to call (e.g. write or search/replace files), and the host executes only **policy-approved** tool calls. The framework can thus extend its own codebase within guardrails.

## How it works

1. **Config**: An agent with `Role: "extender"` and `Parameters.RepoRoot` (or `Parameters.Path`) set to the repository root.
2. **Registry**: When that agent executes, `BackgroundAgentRegistry.ExecuteAgentAsync` calls the optional **ISelfExtendRunner** with that path.
3. **Host**: The host (e.g. CLI) implements **ISelfExtendRunner** (e.g. **SelfExtendRunnerAdapter**), which:
   - Builds a toolbox with **repo.fs.write** and **repo.fs.search_replace** (from Nexo.Tools.Dev).
   - Applies **PathAllowlist** (writes only under `src/` or `tests/`) and **MaxWriteSize** (default 200KB).
   - Creates a **ToolCallingAgent** backed by **IModel** (the same model used by the CLI).
   - Runs one **ThinkAsync** cycle: the model receives tool schemas and world state, responds with JSON `tool_calls`, and the host executes each call that policy approves.
4. **Result**: Executed and denied counts are logged; success/failure reflected in metrics.

## Configuration

- **Role**: `extender`
- **Parameters**:
  - **RepoRoot** or **Path**: repository root directory (required).
- **ModelProvider**: Use an LLM provider (e.g. OpenAI, Azure) if you want the agent to actually propose edits; **deterministic** will return no tool calls.

## Example

See [examples/dogfood-extender.json](examples/dogfood-extender.json): one agent, role `extender`, `Parameters.RepoRoot = "."`, interval 2 hours. For real edits, set **ModelProvider** to your LLM and ensure **MaxDataSensitivity** allows external LLM if needed.

## Safety

- **PathAllowlist**: Only `src/` and `tests/` are writable; other paths are rejected by policy.
- **MaxWriteSize**: Single-file writes are capped (default 200KB).
- **Exfiltration**: Use **DataExfiltrationPolicy** (e.g. via BackgroundAgentPolicyEngineFactory) in the host if you want to restrict what data can leave the process; the default adapter uses PathAllowlist + MaxWriteSize only.

## Relation to self-evolution

The **extender** role is the **self-extending** piece: the framework can schedule an agent that uses the LLM to propose file writes/edits, and only approved calls are executed. Together with **optimizer** (analysis) and **tester** (tests), this supports continuous evolution after deployment. See [SELF_EVOLUTION.md](SELF_EVOLUTION.md).
