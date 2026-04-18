# Runtime Studio — operator cheat sheet

Quick reference when triaging a background-agent daemon or sandboxed repo. Env overrides (optional) for isolated paths:

| Variable | Purpose |
|----------|---------|
| `NEXO_OBSERVATIONS_PATH` | Absolute path to `observations.jsonl` |
| `NEXO_OBJECTIVES_ROOT` | Objectives store root |
| `NEXO_FORGE_ROOT` | Forge proposal queue root |
| `NEXO_AGENT_MODE_PATH` | Aggressiveness mode JSON (`passive`, `semi-active`, `active`, `ambient`) |

From repo root, prefer:

```bash
dotnet run --project src/Nexo.CLI -- background-agent <subcommand>
```

## Observations (structured log)

```bash
dotnet run --project src/Nexo.CLI -- background-agent observations --tail 20
dotnet run --project src/Nexo.CLI -- background-agent observations --kind Build --since-hours 24
dotnet run --project src/Nexo.CLI -- background-agent observations --summary
dotnet run --project src/Nexo.CLI -- background-agent observations --format-json
```

## Objectives (backlog)

```bash
dotnet run --project src/Nexo.CLI -- background-agent objectives list
dotnet run --project src/Nexo.CLI -- background-agent objectives list --status Pending
dotnet run --project src/Nexo.CLI -- background-agent objectives show <id>
dotnet run --project src/Nexo.CLI -- background-agent objectives add --id my-id --title "Title" --body "Markdown" --priority 10
dotnet run --project src/Nexo.CLI -- background-agent objectives block <id> --reason "waiting on upstream"
dotnet run --project src/Nexo.CLI -- background-agent objectives unblock <id>
dotnet run --project src/Nexo.CLI -- background-agent objectives stats
dotnet run --project src/Nexo.CLI -- background-agent objectives report --format-json
dotnet run --project src/Nexo.CLI -- background-agent objectives report --id my-id --format-json
```

**Backlog-driven extender:** In `agent_set` JSON, an `extender` agent with **`RepoRoot`** but **no `Objective` / `Goal` parameter** (or omit those keys) will call `ClaimNext` each self-extend cycle and pull the highest-priority pending item into the run. If you leave a non-empty `Objective` string, that static goal is used instead and the store is not consulted for that field.

## Proposals (forge queue)

```bash
dotnet run --project src/Nexo.CLI -- background-agent proposals list
dotnet run --project src/Nexo.CLI -- background-agent proposals show <id>
dotnet run --project src/Nexo.CLI -- background-agent proposals approve <id> --approver me --note "lgtm"
dotnet run --project src/Nexo.CLI -- background-agent proposals reject <id> --note "not now"
dotnet run --project src/Nexo.CLI -- background-agent proposals stats
dotnet run --project src/Nexo.CLI -- background-agent proposals janitor --format-json
```

Proposals live on disk under `{forge}/proposed|approved|rejected|applied|stale/*.json` — useful for `ls` and emergency edits.

## Aggressiveness mode

```bash
dotnet run --project src/Nexo.CLI -- background-agent mode get
dotnet run --project src/Nexo.CLI -- background-agent mode set --value passive
dotnet run --project src/Nexo.CLI -- background-agent mode set --value active
```

In **passive** / **semi-active**, direct `repo.fs.write` / `search_replace` under `src/` or `tests/` is rejected; agents should use **`forge.propose_change`** and operators **`proposals approve`** / **`apply`**.

## Daemon (long-running)

```bash
dotnet run --project src/Nexo.CLI -- background-agent daemon --config apps/runtime-studio/config/agent_set.local.json --duration 5m
```

Scripts: `apps/runtime-studio/scripts/run_agent_set_local.sh` (see [README](./README.md)).

## Related docs

- [README](./README.md) — layout, compose, tuning
- [CONTRIBUTING](../../CONTRIBUTING.md) — parallel `dotnet build` / file locks
