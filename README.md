# Nexo

Nexo is a .NET framework and CLI for building AI-enabled software with orchestration, trust controls, adaptive pipelines, and test automation.

Repository: <https://github.com/IanFrelinger/Nexo>

## Why Nexo

- Build AI-powered workflows as composable, testable runtime pipelines.
- Mix deterministic and agentic execution with fallback chains.
- Enforce trust boundaries (barriers, audit, routing policy).
- Run validation and multi-environment checks from one CLI.

## Quick Start (5 minutes)

### Choose your lane (recommended)

**Lane A: fastest path (container runtime)**

```bash
docker pull ghcr.io/ianfrelinger/nexo-cli:latest
docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help
```

**Lane B: full local dev path (native SDK)**

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
bash scripts/setup/setup.sh all
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project src/Nexo.CLI -- --help
```

### 1) Prerequisites

- .NET SDK 9.x (the repo is pinned by `global.json`)
- Git
- Optional: Docker (for containerized test workflows)
- Optional: Ollama/OpenAI/Azure credentials (for live model backends)

### Environment setup scripts (Windows/macOS/Linux)

Use the setup scripts to validate or install required tooling and restore baseline NuGet packages used by the core CI gates:

```bash
# Linux/macOS: check required dependencies
bash scripts/setup/setup.sh check

# Linux/macOS: restore NuGet packages/solutions
bash scripts/setup/setup.sh restore

# Linux/macOS: run check + restore
bash scripts/setup/setup.sh all
```

```powershell
# Windows PowerShell: check required dependencies
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode check

# Windows PowerShell: restore NuGet packages/solutions
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode restore

# Windows PowerShell: run check + restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode all
```

### 2) Clone and build (native)

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
bash scripts/setup/setup.sh all
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

### 3) Confirm CLI is working

```bash
dotnet run --project src/Nexo.CLI -- --help
```

### 4) Run a first high-signal command

```bash
dotnet run --project src/Nexo.CLI -- --help
tmp_dir="$(mktemp -d)"
template_path="$tmp_dir/nexo_pipeline_quickstart.json"
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
dotnet run --project src/Nexo.CLI -- validate
```

## First Successful Pipeline Run

```bash
tmp_dir="$(mktemp -d)"
template_path="$tmp_dir/nexo_pipeline_demo.json"

cat > "$template_path" <<'JSON'
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

dotnet run --project src/Nexo.CLI -- pipeline validate --template "$template_path"
dotnet run --project src/Nexo.CLI -- pipeline run --template "$template_path" --run-id demo-run --format-json
dotnet run --project src/Nexo.CLI -- pipeline diagnostics --format-json
```

Pipeline runtime option precedence:
1) defaults, 2) config under `Nexo:Pipelines:*`, 3) environment variables (`NEXO_PIPELINE_*`).

## Common CLI Workflows

| Goal | Command |
|------|---------|
| Show all commands | `dotnet run --project src/Nexo.CLI -- --help` |
| Validate architecture/contracts | `dotnet run --project src/Nexo.CLI -- validate` |
| Analyze source/assemblies | `dotnet run --project src/Nexo.CLI -- analyze --path .` |
| Run pipeline template | `dotnet run --project src/Nexo.CLI -- pipeline run --template <file>` |
| View trust boundary/audit | `dotnet run --project src/Nexo.CLI -- trust --help` |
| Run local test entrypoint | `dotnet run --project src/Nexo.CLI -- test local` |
| CI verification bundle | `dotnet run --project src/Nexo.CLI -- ci` |

## Providers

LLM/vision routing is provider-based:

| Provider | Notes |
|----------|-------|
| `offline`, `mock`, `mock-json` | deterministic/local-friendly paths |
| `ollama` | local model runtime (`OLLAMA_BASE_URL`, `OLLAMA_MODEL`) |
| `openai` | requires `OPENAI_API_KEY` |
| `azure` | requires `AZURE_OPENAI_*` settings |

## Project Layout

```text
Nexo/
├── src/
│   ├── Nexo.CLI/                 # CLI surface
│   ├── Nexo.Hosting/             # AddNexo() integration entrypoint
│   ├── Nexo.Infrastructure/      # execution, persistence, adapters
│   ├── Nexo.Orchestration/       # orchestrator, routing, coordination
│   ├── Nexo.Runtime/             # runtime services and barrier plumbing
│   ├── Nexo.Core.Application/    # use cases and ports
│   ├── Nexo.Core.Domain/         # domain model
│   └── Nexo.Tests.*/             # test suites
├── docs/                         # docs, specs, guides
├── .docker/                      # docker test/runtime definitions
├── global.json                   # SDK pin
└── Nexo.sln
```

## Testing

```bash
# full solution tests
dotnet test Nexo.sln

# focused pipeline tests
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~Pipelines"
```

## Documentation Map

Start here:
- `docs/GettingStarted.md` – guided first-hour setup and usage
- `docs/DocsIndex.md` – where to find docs by task

Core references:
- `docs/Configuration.md` – environment/config options
- `docs/Architecture.md` – architecture and subsystem overview
- `docs/Testing.md` – test strategy, guard rails, and commands
- `docs/OnboardingAutomation.md` – what setup is automated vs. still manual
- `docs/TrustAndInformationArchitecture.md` – trust model, barriers, audit
- `docs/ProductionReadinessGate-v1.md` – production gate procedure
- `docs/EnvironmentSetupGate-v1.md` – cross-platform dependency/bootstrap gate
- `docs/ReleaseCandidateChecklist-v1.md` – RC readiness checklist

## Trust Notes

- JWT barrier resolution reads pre-validated claims from host auth middleware.
- API keys are stored as SHA-256 hashes, not plaintext.
- Audit details never include full API key values.

## License

See repository for license information.
