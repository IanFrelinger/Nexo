# Agent Sandbox Architecture (Host + Project-Scoped Tools)

This guide describes how to run Nexo agents in a constrained sandbox while still
allowing host-bound tools (for example Unity Editor) and dependency downloads.

## Goals

1. Prevent agents from writing outside approved paths.
2. Keep third-party SDKs/tools and caches in project-scoped directories.
3. Support host-installed apps that cannot be containerized by default.

## Policy enforcement in this repo

`PathAllowlist` now supports configurable sandbox prefixes:

- Defaults: `src/`, `tests/`, `docs/`
- Optional env var: `NEXO_AGENT_SANDBOX_PATHS`

When set, `NEXO_AGENT_SANDBOX_PATHS` extends allowed write prefixes (comma-separated):

```bash
export NEXO_AGENT_SANDBOX_PATHS=".nexo/sandbox/projects/,.nexo/sandbox/tools/,.nexo/sandbox/cache/"
```

The policy still blocks:

- absolute paths
- path traversal (`..`)
- writes outside allowlisted prefixes

## Recommended project layout

Create a per-project sandbox tree under repo root:

```text
.nexo/
  sandbox/
    projects/      # agent-created project artifacts
    tools/         # third-party tools/SDKs installed for this project
    cache/         # npm/nuget/pip/unity package caches
    logs/          # run logs/audit traces
```

Bootstrap helper:

```bash
bash scripts/sandbox/init-agent-sandbox.sh
```

## Host-bound applications (Unity, etc.)

Some tools cannot be containerized economically or by license.
For these, use a split model:

1. Keep editor/runtime host-installed by a human operator.
2. Keep project-specific dependencies/caches under `.nexo/sandbox`.
3. Restrict agent-generated files to sandbox + approved code folders.
4. Trigger host apps through wrapper scripts that accept only sandboxed paths.

### Unity pattern

- Unity Editor remains host-installed and user-licensed.
- Agent output goes to:
  - `.nexo/sandbox/projects/<project>/...`
  - or directly to controlled Unity project subfolders under repo.
- Unity package/download caches use `.nexo/sandbox/cache`.
- Any script invoking Unity should validate paths before execution.

## Operating modes

### Mode A: Fully containerized workers

- Use container execution for generic build/test tasks.
- Mount only sandbox directories into container.

### Mode B: Hybrid host workers (Unity-capable)

- Use host workers for Unity-specific operations.
- Keep strict path allowlist + sandbox-rooted caches.
- Prefer dedicated OS user account for agent processes.

## Minimal hardening checklist

1. Run agent daemons as non-admin user.
2. Set `NEXO_AGENT_SANDBOX_PATHS` in daemon environment.
3. Route all temp/cache dirs (`TMPDIR`, package caches) to `.nexo/sandbox/cache`.
4. Keep network egress rules narrow for worker nodes where possible.
5. Audit tool calls and denied writes.

## Example daemon env

```bash
export NEXO_AGENT_SANDBOX_PATHS=".nexo/sandbox/projects/,.nexo/sandbox/tools/,.nexo/sandbox/cache/"
export TMPDIR="$PWD/.nexo/sandbox/cache/tmp"
export NUGET_PACKAGES="$PWD/.nexo/sandbox/cache/nuget"
export npm_config_cache="$PWD/.nexo/sandbox/cache/npm"
```

Then run:

```bash
dotnet run --project src/Nexo.CLI -- background-agent daemon --duration 2h
```

