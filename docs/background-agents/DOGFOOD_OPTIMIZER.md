# Dog-Fooding: Optimizer Background Agents

The framework **dog-foods** its own infrastructure: optimizer background agents run the application's own code analysis pipeline to analyze the codebase after deployment (or on a schedule).

## How It Works

1. **Config**: An agent with `Role: "optimizer"` and `Parameters.Path` (or `Parameters.AnalysisPath`) set to a directory path (e.g. `"."` for repo root).
2. **Registry**: When that agent executes, `BackgroundAgentRegistry.ExecuteAgentAsync` calls the optional **ICodeAnalysisRunner** with that path.
3. **Host**: The host (e.g. CLI) registers an implementation of **ICodeAnalysisRunner** that runs the app's analysis pipeline (e.g. **IAnalysisService** / **AnalyzeCodeCommand**).
4. **Result**: Analysis runs; violation count and summary are logged to the agent log store and success/failure are reflected in metrics.

No separate service or external tool is required—the same pipeline used by `nexo analyze` is invoked by the background agent.

## CLI (Dog-Fooded)

The Nexo CLI registers **CodeAnalysisRunnerAdapter** as **ICodeAnalysisRunner**. It uses **IAnalysisService** (the same port used by `nexo analyze`) inside a scope, so optimizer agents run the same analysis as the CLI command.

- **Config**: Load an optimizer agent (e.g. from `docs/background-agents/examples/dogfood-optimizer.json` or merge into appsettings).
- **Run**: Use `nexo background-agent start codebase-optimizer` or run the host with `registerHostedService: true` and the agent in config; the agent will run on its schedule and execute analysis on the configured path.
- **Logs**: `nexo background-agent logs codebase-optimizer` shows analysis results (violation count, summary).

## Example Config

See **docs/background-agents/examples/dogfood-optimizer.json**: one agent, role `optimizer`, `Parameters.Path = "."`, interval 1 hour. Merge that section into your app config or point your host at that file.

## Sensitivity and Safety

- Optimizer agents respect **MaxDataSensitivity** and **ExfiltrationPolicy** like any other background agent.
- Analysis runs in-process using the same rules and policies as `nexo analyze`; no data is sent to an external LLM unless you configure one for the agent (and policy allows it).
- For read-only, local analysis, use **ModelProvider: "deterministic"** and **MaxDataSensitivity: "Public"** (or appropriate level).

## Extending

- **RAG**: Add RAG config to the optimizer agent and wire a toolbox so the agent can search docs/codebase knowledge before or after analysis (when ThinkAsync is wired).
- **Suggestions**: Today the agent only runs analysis and logs; a future step is to have the agent consume analysis results and produce optimization suggestions (e.g. via tools or report output).
