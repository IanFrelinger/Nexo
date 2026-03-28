# Getting Started with Nexo

This guide is for first-time users who want to run Nexo quickly and understand the core workflows.

## What you will do

In ~10-15 minutes, you will:

1. Pick one startup lane (container-first or native).
2. Verify CLI is working.
3. Validate and run a minimal pipeline.
4. Move to deeper validation/testing only after first success.

## Prerequisites

- .NET SDK **9.x** (repo is pinned in `global.json`).
- Git.
- Optional:
  - Docker (multi-environment testing workflows)
  - Ollama/OpenAI/Azure credentials (model-backed commands)

## 1) Choose your startup lane

### Lane A (fastest): container-first

Use this when local SDK/workload setup is causing friction.

```bash
docker pull ghcr.io/ianfrelinger/nexo-cli:latest
docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help
```

With workspace mount:

```bash
docker run --rm -v "$PWD:/work" -w /work ghcr.io/ianfrelinger/nexo-cli:latest --help
```

### Lane B (full local dev): native setup scripts + CLI build

These scripts validate/install base dependencies and restore the setup-gate baseline NuGet graph for this repository:

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
bash scripts/setup/setup.sh all
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

```powershell
git clone https://github.com/IanFrelinger/Nexo.git
Set-Location Nexo
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode all
dotnet build src\Nexo.CLI\Nexo.CLI.csproj --no-restore
```

## 2) Verify CLI is available

```bash
dotnet run --project src/Nexo.CLI -- --help
```

You should see commands including `analyze`, `validate`, `pipeline`, `trust`, `test`, and `orchestrate`.

## 3) First success command (high confidence, low friction)

```bash
tmp_dir="$(mktemp -d)"
template_path="$tmp_dir/nexo_quickstart.json"
cat > "$template_path" <<'JSON'
{
  "templateId": "quickstart",
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
dotnet run --project src/Nexo.CLI -- pipeline validate --template "$template_path"
```

## 4) Run first high-signal commands

Run these from the repository root:

```bash
# architecture and contract checks
dotnet run --project src/Nexo.CLI -- validate

# code and assembly analysis
dotnet run --project src/Nexo.CLI -- analyze --path .
```

## 5) Run your first pipeline

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

## 6) Optional provider setup

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

## 7) Testing workflows

```bash
# targeted infrastructure tests
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~Pipelines"

# broader local test command
dotnet run --project src/Nexo.CLI -- test local
```

For timeout policy and anti-hang guidance, see `docs/Testing.md`.

## 8) Embed in your host application

At minimum:

```csharp
using Nexo.Hosting;

services.AddNexo();
```

Then resolve application ports from DI (analysis, validation, orchestration, etc.).

## Common pitfalls

- If commands fail due to SDK mismatch, ensure your local SDK honors `global.json` (`9.x`).
- Prefer running heavy validations sequentially (not in parallel terminals) to avoid resource pressure.
- For CI parity, use the documented gate workflows under `.github/workflows/`.
- If native setup is blocking, switch to container lane first and continue there.

## Next documents to read

- `README.md` — high-level orientation and command map.
- `docs/Architecture.md` — subsystem layout and flow.
- `docs/TrustAndInformationArchitecture.md` — barrier/trust model.
- `docs/ProductionReadinessGate-v1.md` — release gate and operator checks.
