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

Agent roles included:

- `runtime-planner` (`extender`) - planning + safe code actions through policy-gated self-extend
- `runtime-worker-optimizer` (`optimizer`) - code analysis worker
- `runtime-worker-tester` (`tester`) - test verification worker

## Quick start

From repo root:

```bash
bash apps/runtime-studio/scripts/bootstrap_runtime_studio.sh
bash apps/runtime-studio/scripts/run_agent_set_local.sh --duration 5m --disable-observation
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

