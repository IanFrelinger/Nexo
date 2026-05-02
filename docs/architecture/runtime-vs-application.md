# Runtime vs application repository boundary

This repository keeps **kernel/runtime libraries** under `src/` and **application surfaces** under `application/src/` (CLI, HTTP API, Forge/game descriptors).

## Layout

| Location | Role |
|----------|------|
| `src/` | Execution kernel: Abstractions, Core, Hosting library, Infrastructure, Orchestration, Runtime, agents, tests for kernel |
| `application/src/` | Product hosts: `Nexo.API`, `Nexo.CLI`, `Nexo.GameDomain`, plus `Nexo.Tests.CLI` / `Nexo.Tests.GameDomain` |

## Solution files

| File | Contents |
|------|----------|
| `Nexo.Runtime.sln` | Kernel packages only (no application folder) |
| `Nexo.Application.sln` | `application/src/*` projects |
| `Nexo.sln` | Full monorepo (kernel + clients + tests); **does not** list application projects as top-level entries — reference via project refs when needed |

Build application layer:

```bash
dotnet build application/Nexo.Application.sln
```

## NuGet consumers

- **`Nexo.Runtime.Bundle`** — kernel libraries without composing your product.
- **`Nexo.Hosting.Bundle`** — kernel + `AddNexo` composition helper.

Application-specific deployment (API container, CLI tool publish) should reference paths under `application/src/` or consume published `Nexo.API` / `Nexo.CLI` packages after pack.
