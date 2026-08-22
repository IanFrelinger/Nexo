# Runtime vs application boundary

This monorepo keeps **kernel/runtime libraries** under `src/`, **open application surfaces** under `application/src/` (CLI, HTTP API), and **open products built on the core** under `applications/`. Forge/game descriptors and the Game Director host live under **`commercial/`**. The same split matters when you consume Ashlar as **NuGet packages** from another repository.

## Layout in this repository

| Location | Role |
|----------|------|
| `src/` | Execution kernel: abstractions, core, hosting library, infrastructure, orchestration, runtime, agents, transport/protocol adapters (gRPC, MCP, A2A), ingress adapters, and tests for the kernel graph |
| `application/src/` | Open product hosts: `Ashlar.API`, `Ashlar.CLI`, plus `Ashlar.Tests.CLI` |
| `applications/` | Open (Apache-2.0) products on top of the kernel — physical-atom certification, provenance graph, spatial contracts/runtime/platform providers. They reference `src/`; `src/` never references them (`dependency-boundary` check 4). Layout carries the autonomy tier (`TrustKernel.KernelPathPrefixes` lists `src/` prefixes), which is why these live outside `src/`. See [`applications/README.md`](../../applications/README.md). |
| `commercial/` | Commercial vertical: `Ashlar.Commercial.GameDomain`, Game Director host/MCP, Fleet, MeshDirector, and their tests |

## Runtime layer (NuGet / embeddable graph)

Use **`Ashlar.Runtime.sln`** at the repository root to build and validate only the kernel graph (no `application/` projects).

Conceptually **runtime** includes:

| Concern | Projects (examples) |
|--------|---------------------|
| Contracts | `Ashlar.Abstractions`, `Ashlar.Brick.Contracts`, `Ashlar.Contracts` |
| Domain rules | `Ashlar.Core.Domain` |
| Application ports and use cases | `Ashlar.Core.Application` |
| Policy primitives | `Ashlar.Policies`, `Ashlar.Policies.Dev` |
| Agent execution and transport | `Ashlar.Runtime`, `Ashlar.Transport.Grpc`, `Ashlar.Transport.Grpc.Server` |
| Default adapters | `Ashlar.Infrastructure` |
| Orchestration | `Ashlar.Orchestration` |
| Background agents (library) | `Ashlar.BackgroundAgents` |
| Tooling for agents | `Ashlar.Tools.Assembly`, `Ashlar.Tools.Dev` |
| SMS ingress (optional) | `Ashlar.Ingress.AwsSns`, `Ashlar.Ingress.DynamoDb` and their test projects |

**Composition helper (still a library, not your app):**

- **`Ashlar.Hosting`** — `AddAshlar` and DI wiring. Many hosts consume it from NuGet; strict splits may reimplement registration in the application repo.

## NuGet metapackages

| Package | Purpose |
|---------|---------|
| `Ashlar.Runtime.Bundle` | Kernel libraries without `Ashlar.Hosting` (compose DI yourself or add `Ashlar.Hosting` separately). |
| `Ashlar.Hosting.Bundle` | Kernel plus `Ashlar.Hosting` for turnkey embedding. |

Version all packages from the same release (same `PackageVersion` when packing). Application-specific deployment (API container, CLI publish) can reference paths under `application/src/` or consume published `Ashlar.API` / `Ashlar.CLI` packages after pack.

## Application layer

Product-specific deployables and descriptors stay under **`application/src/`** in this repo. If you maintain a **separate** product repository, keep private integrations, bespoke DTOs, and composition overrides there and reference the runtime packages above.

## Solution files

| File | Purpose |
|------|---------|
| `Ashlar.Runtime.sln` | Runtime kernel graph for CI and publishing libraries (and `Ashlar.Runtime.Bundle`). |
| `application/Ashlar.Application.sln` | Open `application/src/*` only (`Ashlar.API`, `Ashlar.CLI`, `Ashlar.Tests.CLI`); it no longer pulls the commercial GameDomain projects. |
| `Ashlar.sln` | Full monorepo: kernel, clients, infrastructure tests, `Ashlar.Runtime.Bundle`, ingress projects, `applications/`, plus the Game Director / GameDomain commercial projects. Application code is built via **`dotnet build application/Ashlar.Application.sln`** when you only need product surfaces. |

Build application layer:

```bash
dotnet build application/Ashlar.Application.sln
```

`Ashlar.Kernel.sln` (when present) is a mid-sized kernel-focused subset for workflows that do not need the entire `Ashlar.sln` graph.

## See also

- **`docs/DistributionModels.md`** — how external hosts and operators consume Ashlar (NuGet, HTTP, CLI, compose, mesh) and which **CI gates** prove each path.
