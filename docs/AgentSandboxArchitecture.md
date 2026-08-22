# Agent Sandbox Architecture (Host + Project-Scoped Tools)

This guide describes how to run Ashlar agents in a constrained sandbox while still
allowing host-bound tools and dependency downloads.

## Goals

1. Prevent agents from writing outside approved paths.
2. Keep third-party SDKs/tools and caches in project-scoped directories.
3. Support host-installed apps that cannot be containerized by default.

## Policy enforcement in this repo

`PathAllowlist` supports sandboxed writes in two ways:

- Relative allowlisted prefixes (defaults): `src/`, `tests/`, `docs/`, `.ashlar/`
- Optional extra prefixes via env: `ASHLAR_PATH_ALLOWLIST_EXTRA`
- Absolute paths only when inside sandbox root (`WorldSnapshot["SandboxRoot"]` or `ASHLAR_SANDBOX_ROOT`)

When set, `ASHLAR_PATH_ALLOWLIST_EXTRA` extends relative write prefixes (comma-separated):

```bash
export ASHLAR_PATH_ALLOWLIST_EXTRA=".ashlar/host_apps/,.ashlar/agents/workspaces/"
```

The policy still blocks:

- absolute paths
- path traversal (`..`)
- writes outside allowlisted prefixes

## Recommended project layout

Create a per-project sandbox tree under repo root:

```text
.ashlar/
  agents/
    workspaces/    # agent-created artifacts and generated work trees
  tools/
    bin/           # third-party tools/SDKs installed for this project
    cache/         # npm/nuget/pip package caches
  host_apps/
    projects/      # host-app project data staged for agent workflows
    cache/         # host-app package/import caches
    runtimes/      # optional host-app runtime payloads
  logs/
  tmp/
```

Bootstrap helper:

```bash
bash scripts/sandbox/init-agent-sandbox.sh
```

## Host-bound applications (generic)

Some tools cannot be containerized economically or by license.
For these, use a split model:

1. Keep editor/runtime host-installed by a human operator.
2. Keep project-specific dependencies/caches under `.ashlar/`.
3. Restrict agent-generated files to sandbox + approved code folders.
4. Trigger host apps through wrapper scripts that accept only sandboxed paths.

## Operating modes

### Mode A: Fully containerized workers

- Use container execution for generic build/test tasks.
- Mount only sandbox directories into container.

### Mode B: Hybrid host workers (host-app capable)

- Use host workers for tool/app operations that cannot be containerized by default.
- Keep strict path allowlist + sandbox-rooted caches.
- Prefer dedicated OS user account for agent processes.

## Minimal hardening checklist

1. Run agent daemons as non-admin user.
2. Set `ASHLAR_SANDBOX_ROOT` in daemon environment.
3. Optionally set `ASHLAR_PATH_ALLOWLIST_EXTRA` for additional project-local prefixes.
4. Route all temp/cache dirs (`TMPDIR`, package caches) to `.ashlar/tools/cache` and `.ashlar/host_apps/cache`.
5. Keep network egress rules narrow for worker nodes where possible.
6. Audit tool calls and denied writes.

## Example daemon env

```bash
export ASHLAR_SANDBOX_ROOT="$PWD/.ashlar"
export ASHLAR_PATH_ALLOWLIST_EXTRA=".ashlar/host_apps/,.ashlar/agents/workspaces/"
export TMPDIR="$PWD/.ashlar/tools/cache/tmp"
export NUGET_PACKAGES="$PWD/.ashlar/tools/cache/nuget"
export npm_config_cache="$PWD/.ashlar/tools/cache/npm"
```

Then run:

```bash
dotnet run --project application/src/Ashlar.CLI -- background-agent daemon --duration 2h
```

