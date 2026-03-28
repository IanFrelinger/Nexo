# Getting Started with Nexo

This guide is for first-time users who want to run Nexo quickly and understand the core workflows.

## What you will do

In ~15 minutes, you will:

1. Build and run the CLI.
2. Run validation and analysis.
3. Execute a pipeline template.
4. Inspect runtime diagnostics.

## Prerequisites

- .NET SDK **9.x** (repo is pinned in `global.json`).
- Git.
- Optional:
  - Docker (multi-environment testing workflows)
  - Ollama/OpenAI/Azure credentials (model-backed commands)

## 1) Clone

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
```

## 2) Run platform setup script (recommended first run)

These scripts validate/install base dependencies and restore the setup-gate baseline NuGet graph for this repository:

```bash
# Linux/macOS
bash scripts/setup/setup.sh check
bash scripts/setup/setup.sh all
```

```powershell
# Windows PowerShell
powershell -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode check
powershell -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode all
```

## 2b) One-click installer wrappers (Option 1)

If you want a single-command bootstrap that clones/updates repo + setup + restore + CLI build:

```bash
# Linux/macOS unified entrypoint
bash scripts/install/install.sh --yes
```

```powershell
# Windows PowerShell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install.ps1 -Yes
```

## 3) Build only the CLI project (workload-safe baseline)

`Nexo.sln` includes mobile projects that may require platform workloads (`maui-android`, etc.). For first run, build the CLI path first:

```bash
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

## 4) Verify CLI is available

```bash
dotnet run --project src/Nexo.CLI -- --help
```

You should see commands including `analyze`, `validate`, `pipeline`, `trust`, `test`, and `orchestrate`.

## 4b) Run background-agent daemon mode (optional)

Use this when you want Nexo to run as a long-lived local process with hosted background agents.
If your config does not define `Nexo:Barriers:Levels`, the daemon defaults to `["public","internal"]` for local bootstrap.

```bash
# run for 30 seconds (smoke test)
dotnet run --project src/Nexo.CLI -- background-agent daemon --duration 30s

# run until Ctrl+C
dotnet run --project src/Nexo.CLI -- background-agent daemon

# use an explicit background-agent config file
dotnet run --project src/Nexo.CLI -- background-agent daemon --config docs/background-agents/examples/minimal-agent.json
```

## 5) Run first high-signal commands

Run these from the repository root:

```bash
# code and assembly analysis
dotnet run --project src/Nexo.CLI -- analyze --path .

# optional (heavier) architecture/test validation
dotnet run --project src/Nexo.CLI -- validate
```

## 6) Run your first pipeline

Create a minimal template:

```bash
cat > /tmp/nexo_pipeline_demo.json <<'JSON'
{
  "templateId": "demo",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "hybrid", "name": "Hybrid", "mode": "Hybrid", "fallbackChain": ["Deterministic", "Agentic"] }
  ],
  "edges": [
    { "fromStageId": "ingest", "toStageId": "hybrid" }
  ]
}
JSON
```

Validate + run:

```bash
dotnet run --project src/Nexo.CLI -- pipeline validate --template /tmp/nexo_pipeline_demo.json
dotnet run --project src/Nexo.CLI -- pipeline run --template /tmp/nexo_pipeline_demo.json --run-id demo-run --format-json
dotnet run --project src/Nexo.CLI -- pipeline diagnostics --format-json
```

## 7) Optional provider setup

If you plan to run model-backed workflows:

### OpenAI

```bash
export OPENAI_API_KEY="sk-..."
export OPENAI_MODEL="gpt-4o-mini"
```

### Ollama

```bash
export OLLAMA_BASE_URL="http://localhost:11434"
export OLLAMA_MODEL="llama3.1"
```

Provider behavior and full configuration are documented in `docs/Configuration.md`.

## 8) Testing workflows

```bash
# targeted infrastructure tests
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~Pipelines"

# broader local test command
dotnet run --project src/Nexo.CLI -- test local
```

For timeout policy and anti-hang guidance, see `docs/Testing.md`.

## 9) Embed in your host application

At minimum:

```csharp
using Nexo.Hosting;

services.AddNexo();
```

Then resolve application ports from DI (analysis, validation, orchestration, etc.).

## Common pitfalls

- If commands fail due to SDK mismatch, ensure your local SDK honors `global.json` (`9.x`).
- `dotnet build Nexo.sln` can require mobile workloads depending on host; use setup scripts + CLI project build first.
- Prefer running heavy validations sequentially (not in parallel terminals) to avoid resource pressure.
- For CI parity, use the documented gate workflows under `.github/workflows/`.

## Next documents to read

- `README.md` — high-level orientation and command map.
- `docs/Architecture.md` — subsystem layout and flow.
- `docs/TrustAndInformationArchitecture.md` — barrier/trust model.
- `docs/ProductionReadinessGate-v1.md` — release gate and operator checks.
