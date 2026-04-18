# Runtime Studio — operator cheat sheet

Quick reference when triaging a background-agent daemon or sandboxed repo. Env overrides (optional) for isolated paths:

| Variable | Purpose |
|----------|---------|
| `NEXO_OBSERVATIONS_PATH` | Absolute path to `observations.jsonl` |
| `NEXO_OBJECTIVES_ROOT` | Objectives store root |
| `NEXO_FORGE_ROOT` | Forge proposal queue root |
| `NEXO_AGENT_MODE_PATH` | Aggressiveness mode JSON (`passive`, `semi-active`, `active`, `ambient`) |
| `NEXO_DASHBOARD_AUTH_TOKEN` | Optional shared secret for `background-agent dashboard` (same as `--auth-token`) |

From repo root, prefer:

```bash
dotnet run --project src/Nexo.CLI -- background-agent <subcommand>
```

**API metrics** (when Nexo.API is running, same shape as `runtime-studio metrics`):

```bash
curl -s http://localhost:5000/api/runtime-studio/metrics
```

**CLI metrics** (no API required; uses repo root + `NEXO_*`):

```bash
dotnet run --project src/Nexo.CLI -- runtime-studio metrics
dotnet run --project src/Nexo.CLI -- runtime-studio metrics --format-json
```

Combine with agent-set status (optional backlog block / JSON field `runtimeStudioMetrics`):

```bash
dotnet run --project src/Nexo.CLI -- runtime-studio status --with-metrics
dotnet run --project src/Nexo.CLI -- runtime-studio status --format-json --with-metrics
```

**Doctor** (CI / laptop sanity — exit 1 on hard failures):

```bash
dotnet run --project src/Nexo.CLI -- runtime-studio doctor
dotnet run --project src/Nexo.CLI -- runtime-studio doctor --format-json
dotnet run --project src/Nexo.CLI -- runtime-studio doctor --strict
```

`--strict` fails if `NEXO_OBJECTIVES_ROOT` / `NEXO_FORGE_ROOT` directories (after resolution) or the parent folder of the observations file does not exist yet.

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

## Operator dashboard (local HTTP)

Read-only snapshot (objective counts by folder, proposal counts, last lines of `observations.jsonl`, aggressiveness mode). **Listens on 127.0.0.1 only** — not for exposure to a network.

```bash
dotnet run --project src/Nexo.CLI -- background-agent dashboard --port 5055
dotnet run --project src/Nexo.CLI -- background-agent dashboard --port 5055 --open
dotnet run --project src/Nexo.CLI -- background-agent dashboard --auth-token "your-secret"
```

Set the same `NEXO_*` paths as the daemon before starting so the UI matches that sandbox.

### TLS reverse proxy (tailnet / LAN)

The dashboard binds **127.0.0.1** only. To reach it from another machine over HTTPS, terminate TLS on a reverse proxy and forward to localhost (do not expose the raw HttpListener to the internet).

**Caddy** (example):

```text
dashboard.example.ts.net {
  reverse_proxy 127.0.0.1:5055
}
```

**nginx** (example):

```nginx
location / {
    proxy_pass http://127.0.0.1:5055;
    proxy_set_header Host $host;
    proxy_set_header Authorization $http_authorization;
}
```

With `--auth-token` set, configure the browser bookmark as `https://…/?token=…` or inject `Authorization` at the proxy (narrow ACLs on who can reach the vhost).

## Android AAB / Play Console

See **[ANDROID_STORE.md](./ANDROID_STORE.md)** for `dotnet publish` AAB, CI artifact workflow, and release keystore secrets.

## Related docs

- [README](./README.md) — layout, compose, tuning
- [CONTRIBUTING](../../CONTRIBUTING.md) — parallel `dotnet build` / file locks
