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

## Operator CLI quick reference

See **[OPERATOR.md](./OPERATOR.md)** for copy-paste `background-agent` commands and env vars.

## Quick start

From repo root:

```bash
bash apps/runtime-studio/scripts/bootstrap_runtime_studio.sh
bash apps/runtime-studio/scripts/run_agent_set_local.sh --duration 5m --disable-observation
```

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

## Hardware-tuned workflow optimize (optional)

After bootstrap, benchmark workflow compositions on this machine and emit a recommendation report:

```bash
bash apps/runtime-studio/scripts/optimize_agent_cluster.sh --objective 'your tuning goal' --verbose
```

The script forwards **`--budget-runs`** to `nexo workflow optimize` (default **48** measured runs cap for laptop-friendly runs; use **`--budget-runs 0`** for no cap), then runs **`nexo runtime-studio apply-tune`** so the winning local model profile is written into **`apps/runtime-studio/config/agent_set.local.json`** (skip with **`--skip-apply-agent-set`**). Run **`bash apps/runtime-studio/scripts/optimize_agent_cluster.sh --help`** for all flags.

Windows **`scripts/setup/setup.ps1 -Mode all`** runs the same optimize + apply flow after dependencies restore (skip with **`-SkipRuntimeStudioTune`** or **`NEXO_SKIP_RUNTIME_STUDIO_TUNE=1`**).

Inspect current state anytime: **`dotnet run --project src/Nexo.CLI -- runtime-studio status`** (or **`--format-json`**).

## Customize the agent set

Edit:

- `apps/runtime-studio/config/agent_set.local.json`

Common changes:

- tune schedule intervals
- update `ModelName` for planner/worker split
- change tester filter
- adjust exfiltration policy boundaries
- adjust each game worker `Objective` to match your project pillars (art style, encounter pacing, systems depth)

## Phase 2 (shipped in tree)

| Track | Status |
|--------|--------|
| **Daemon in CI** | Black-box: timed daemon with/without `--disable-observation` (`RuntimeStudioBlackBoxSmokeTests`). |
| **Passive + forge** | `ForgeToolsTests` (propose/check/**forge.build**) + `ForgeMediatedWritesPolicy` tests + `ProposalsBackgroundAgentCommandTests` (`build`, `apply --verify-build`). |
| **Operator UX** | **[OPERATOR.md](./OPERATOR.md)** — env vars, CLI one-liners for observations / objectives / proposals / mode / daemon. |
| **Mobile / MAUI** | `.github/workflows/maui-client-build-gate.yml` — Windows, Mac Catalyst, **Android** compile jobs. |
| **Performance** | `CliRunner` cross-process mutex + `CONTRIBUTING.md` guidance; smoke blame-hang 180s on Cross-Platform Tests. |

Contributing note: avoid parallel full `dotnet build` on one clone (see `CONTRIBUTING.md` — `*.deps.json` locks).

## Phase 3 (shipped)

| Track | Status |
|--------|--------|
| **Objective claim in daemon (E2E)** | Black-box: `RuntimeStudioBlackBoxSmokeTests.Daemon_extender_claims_objective_from_store_increments_attempts` — timed extender with no pinned `Objective`, deterministic model, asserts backlog `Attempts` after release-on-no-action. |
| **Android signing / store** | **[ANDROID_STORE.md](./ANDROID_STORE.md)** — Play-style AAB pipeline (`.github/workflows/maui-android-publish.yml`) + keystore secrets checklist. |
| **Operator dashboard** | `nexo background-agent dashboard [--port 5055] [--open]` — read-only JSON + auto-refresh UI on **127.0.0.1** only (same `NEXO_*` paths as the CLI). |

## Phase 4 (shipped)

| Track | Status |
|--------|--------|
| **Objective SLA-style metrics in API** | `GET /api/runtime-studio/metrics` — counts by objective/proposal status, `OldestPendingAgeHours` / `OldestInProgressAgeHours`, observation log path + file size. |
| **Dashboard auth + TLS notes** | `nexo background-agent dashboard --auth-token …` or `NEXO_DASHBOARD_AUTH_TOKEN`; `?token=` / `Authorization: Bearer`; reverse-proxy snippets in **[OPERATOR.md](./OPERATOR.md)**. |
| **Play Internal testing** | **[PLAY_INTERNAL.md](./PLAY_INTERNAL.md)** — internal track checklist, CI artifact handoff, service-account automation pointers. |

## Phase 5 (shipped)

| Track | Status |
|--------|--------|
| **Shared metrics core** | `Nexo.BackgroundAgents.RuntimeStudio` — `RuntimeStudioPathResolver`, `RuntimeStudioMetricsCollector` (+ unit tests). API `GET /api/runtime-studio/metrics` and CLI/dashboard consume the same logic. |
| **CLI parity** | `nexo runtime-studio metrics [--format-json]` — backlog counts, SLA ages, observation file size (paths from `NEXO_*` + repo root). |
| **Dashboard JSON** | `background-agent dashboard` `/api/summary.json` includes a `metrics` object (`RuntimeStudioDiskMetrics`) alongside `observationsTail`. |

## Phase 6 (shipped)

| Track | Status |
|--------|--------|
| **Operator dashboard UI** | HTML **at-a-glance** cards + paths summary + collapsible raw JSON (reads `metrics` / PascalCase-safe). |
| **Status + metrics** | `nexo runtime-studio status --with-metrics` (text block or JSON `runtimeStudioMetrics` when combined with `--format-json`). |
| **Smoke** | `RuntimeStudioBlackBoxSmokeTests.Runtime_studio_metrics_format_json_exits_zero` — CLI metrics with isolated `NEXO_*` env. |

## Phase 7 (shipped)

| Track | Status |
|--------|--------|
| **Observation tail metrics** | `RuntimeStudioDiskMetrics` adds `ObservationsTailLineCount` (tail window) + `ObservationsLastTimestamp`; `ObservationLogTailReader` (+ tests). API, CLI, dashboard cards updated. |
| **Runtime Studio doctor** | `nexo runtime-studio doctor [--format-json] [--strict]` — validates agent-set JSON (`BackgroundAgents.Agents`) and path layout; strict mode errors on missing objectives/forge dirs. |
| **Smoke** | `Runtime_studio_doctor_format_json_exits_zero` — doctor against the real repo agent set. |

