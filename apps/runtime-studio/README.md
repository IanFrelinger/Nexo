# Runtime Studio (Application Layer)

`runtime-studio` is an application-level integration that composes Nexo runtime services into a planner + worker agent set.

This sits outside kernel internals and uses:

- `Nexo.CLI background-agent daemon --config ...`
- project-scoped sandboxing under `.nexo/`
- local-first model routing via Ollama

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

To run the **same agent set** behind the **Nexo.API** web portal (Docker, mounted repo), see `docs/SelfHostedAgentServer.md` and `docker-compose.agent-server.yml`.

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

