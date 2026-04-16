# Runtime Studio (Application Layer)

`runtime-studio` is an application-level integration that composes Nexo runtime services into a planner + worker agent set.

This sits outside kernel internals and uses:

- `Nexo.CLI background-agent daemon --config ...`
- project-scoped sandboxing under `.nexo/`
- local-first model routing via Ollama

<a id="how-runtime-studio-fits-with-nexo-api"></a>

## How this fits (one config, flexible hosts)

There is **no second “Runtime Studio app”** in the repo — only this folder (config + scripts) plus the shared Nexo kernel.

| Piece | What it is |
|--------|------------|
| **`config/agent_set.local.json`** | **Single source of truth** for the planner / optimizer / tester **background** agent definitions (`BackgroundAgents:Agents`). |
| **CLI daemon** (`run_agent_set_local.sh`) | Runs that JSON in a **standalone** `nexo background-agent daemon` process (best for local dev matching CI-style scripts). |
| **`Nexo.API` + `NEXO_BACKGROUND_AGENTS_CONFIG`** | Runs the **same JSON** inside the API process (best with Docker / a mounted repo — see `docs/SelfHostedAgentServer.md`). |
| **Director portal** (`Nexo.API` `/`) | **Different workflow**: human-driven goals → orchestration → **dailies** JSON. It does not replace the background cluster; it often lives on the **same** API host. |

**Compose (pick the lane you need):**

- **`docker-compose.portal.yml`** — portal + API + Ollama; **no** mounted workspace / default agent-server cluster wiring.
- **`docker-compose.agent-server.yml`** — portal + API + Ollama + **mounted repo** + default `NEXO_BACKGROUND_AGENTS_CONFIG` → this folder’s JSON.

Cross-platform env tuning: `docs/config/agent-server.env.example`.

## Why this exists in `apps/`

This is a runtime application of Nexo, not a kernel primitive.
Kernel packages stay reusable; Runtime Studio defines product workflow, operator scripts, and agent-set policy.

## Local-first agent set

Config file:

- `apps/runtime-studio/config/agent_set.local.json`
- `apps/runtime-studio/config/agent_set.game_director.local.json` (director mode for game projects)

Agent roles included:

- `runtime-planner` (`extender`) - planning + safe code actions through policy-gated self-extend
- `runtime-worker-optimizer` (`optimizer`) - code analysis worker
- `runtime-worker-tester` (`tester`) - test verification worker

Game director mode roles included:

- `game-director-planner` (`extender`) - plans iterations and delegates technical execution
- `game-worker-asset-pipeline` (`extender`) - drives asset pipeline tasks and integration
- `game-worker-level-layout` (`extender`) - executes level layout / blockout iteration tasks
- `game-worker-systems-designer` (`extender`) - implements and tunes game systems
- `game-worker-code-optimizer` (`optimizer`) - static analysis / optimization sweeps
- `game-worker-test-automation` (`tester`) - recurring automated test execution

## Quick start

From repo root:

```bash
bash apps/runtime-studio/scripts/bootstrap_runtime_studio.sh
bash apps/runtime-studio/scripts/run_agent_set_local.sh --duration 5m --disable-observation
```

### Optimize for your hardware first (recommended)

A single script bundles the full setup → optimize → run workflow:

```bash
# Benchmark compositions on your hardware, emit a report, then start the daemon.
bash apps/runtime-studio/scripts/optimize_agent_cluster.sh \
  --objective "Plan and deliver iterative runtime improvements" \
  --report-output .nexo/workflow/optimize-report.md \
  --duration 5m \
  --verbose
```

The script executes three steps:

1. **Bootstrap** — initialises the Runtime Studio sandbox (idempotent).
2. **Optimize** — scaffolds a workflow lab spec if none exists, runs `nexo workflow optimize` to benchmark composition + model candidates against local hardware, selects and promotes a winner, and writes an optional recommendation report.
3. **Daemon** — starts the background agent daemon with the agent-set config (only when `--duration` is provided).

Run `--help` for the full option list:

```bash
bash apps/runtime-studio/scripts/optimize_agent_cluster.sh --help
```

Common flags:

| Flag | Purpose |
|------|---------|
| `--objective <text>` | High-level objective used to prioritise candidates. |
| `--spec <path>` | Custom workflow lab runtime spec JSON. |
| `--search-strategy <name>` | `successive-halving` (default), `objective-first`, or `exhaustive`. |
| `--max-candidates <n>` | Cap evaluated candidates (default 24). |
| `--report-output <path>` | Write recommendation report (`.md`, `.json`, `.txt`). |
| `--provider <name>` | Override LLM provider for all profiles. |
| `--ollama-model <model>` | Override `OLLAMA_MODEL`. |
| `--duration <dur>` | Daemon duration (e.g. `5m`, `1h`); omit to skip the daemon step. |
| `--skip-optimize` | Bootstrap + daemon only (skip benchmarking). |
| `--skip-daemon` | Bootstrap + optimize only (no daemon). |
| `--verbose` | Emit optimizer progress output. |

Game director quick start:

```bash
bash apps/runtime-studio/scripts/bootstrap_runtime_studio.sh
bash apps/runtime-studio/scripts/run_game_director_local.sh --duration 10m --disable-observation
```

Set a test filter for your project lane before starting:

```bash
export NEXO_GAME_TEST_FILTER="FullyQualifiedName~YourGameNamespace"
bash apps/runtime-studio/scripts/run_game_director_local.sh --duration 10m
```

The run script configures:

- `NEXO_SANDBOX_ROOT`
- `NEXO_PATH_ALLOWLIST_EXTRA`
- cache relocation (`TMPDIR`, `NUGET_PACKAGES`, `NPM_CONFIG_CACHE`)
- local model defaults (`OLLAMA_BASE_URL`, `OLLAMA_MODEL`)

## Customize the agent set

Edit:

- `apps/runtime-studio/config/agent_set.local.json`

Common changes:

- tune schedule intervals
- update `ModelName` for planner/worker split
- change tester filter
- adjust exfiltration policy boundaries
- adjust each game worker `Objective` to match your project pillars (art style, encounter pacing, systems depth)

