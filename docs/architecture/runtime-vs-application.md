# Runtime vs application boundary

This monorepo keeps **kernel/runtime libraries** under `src/` and **open application surfaces** under `application/src/` (CLI, HTTP API). Forge/game descriptors and the Game Director host live under **`commercial/`**. The same split matters when you consume Nexo as **NuGet packages** from another repository.

## Layout in this repository

| Location | Role |
|----------|------|
| `src/` | Execution kernel: abstractions, core, hosting library, infrastructure, orchestration, runtime, agents, ingress adapters, and tests for the kernel graph |
| `application/src/` | Open product hosts: `Nexo.API`, `Nexo.CLI`, plus `Nexo.Tests.CLI` |
| `commercial/` | Commercial vertical: `Nexo.Commercial.GameDomain`, Game Director host/MCP, and `Nexo.Commercial.Tests.GameDomain` |

## Runtime layer (NuGet / embeddable graph)

Use **`Nexo.Runtime.sln`** at the repository root to build and validate only the kernel graph (no `application/` projects).

Conceptually **runtime** includes:

| Concern | Projects (examples) |
|--------|---------------------|
| Contracts | `Nexo.Abstractions`, `Nexo.Brick.Contracts`, `Nexo.Contracts` |
| Domain rules | `Nexo.Core.Domain` |
| Application ports and use cases | `Nexo.Core.Application` |
| Policy primitives | `Nexo.Policies`, `Nexo.Policies.Dev` |
| Agent execution and transport | `Nexo.Runtime`, `Nexo.Transport.Grpc`, `Nexo.Transport.Grpc.Server` |
| Default adapters | `Nexo.Infrastructure` |
| Orchestration | `Nexo.Orchestration` |
| Background agents (library) | `Nexo.BackgroundAgents` |
| Tooling for agents | `Nexo.Tools.Assembly`, `Nexo.Tools.Dev` |
| SMS ingress (optional) | `Nexo.Ingress.AwsSns`, `Nexo.Ingress.DynamoDb` and their test projects |

**Composition helper (still a library, not your app):**

- **`Nexo.Hosting`** — `AddNexo` and DI wiring. Many hosts consume it from NuGet; strict splits may reimplement registration in the application repo.

## NuGet metapackages

| Package | Purpose |
|---------|---------|
| `Nexo.Runtime.Bundle` | Kernel libraries without `Nexo.Hosting` (compose DI yourself or add `Nexo.Hosting` separately). |
| `Nexo.Hosting.Bundle` | Kernel plus `Nexo.Hosting` for turnkey embedding. |

Version all packages from the same release (same `PackageVersion` when packing). Application-specific deployment (API container, CLI publish) can reference paths under `application/src/` or consume published `Nexo.API` / `Nexo.CLI` packages after pack.

## Application layer

Product-specific deployables and descriptors stay under **`application/src/`** in this repo. If you maintain a **separate** product repository, keep private integrations, bespoke DTOs, and composition overrides there and reference the runtime packages above.

## Solution files

| File | Purpose |
|------|---------|
| `Nexo.Runtime.sln` | Runtime kernel graph for CI and publishing libraries (and `Nexo.Runtime.Bundle`). |
| `application/Nexo.Application.sln` | Open `application/src/*` plus commercial GameDomain projects referenced for local dev. |
| `Nexo.sln` | Full monorepo: kernel, clients, infrastructure tests, `Nexo.Runtime.Bundle`, ingress projects, etc. Application code is built via **`dotnet build application/Nexo.Application.sln`** when you only need product surfaces. |

Build application layer:

```bash
dotnet build application/Nexo.Application.sln
```

`Nexo.Kernel.sln` (when present) is a mid-sized kernel-focused subset for workflows that do not need the entire `Nexo.sln` graph.

## See also

- **`docs/DistributionModels.md`** — how external hosts and operators consume Nexo (NuGet, HTTP, CLI, compose, mesh) and which **CI gates** prove each path.
