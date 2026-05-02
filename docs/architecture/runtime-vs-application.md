# Runtime vs application repository boundary

This document defines how to split **Nexo runtime libraries** (shipped from this repository as NuGet packages) from **application code** (your product repo that references those packages).

## Runtime layer (this repository)

Use **`Nexo.Runtime.sln`** at the repository root to build and validate only the kernel graph.

Conceptually **runtime** includes:

| Concern | Projects (examples) |
|--------|---------------------|
| Contracts | `Nexo.Abstractions`, `Nexo.Brick.Contracts` |
| Shared primitives | `Nexo.Core` |
| Domain rules | `Nexo.Core.Domain` |
| Application ports & use cases | `Nexo.Core.Application` |
| Policy primitives | `Nexo.Policies`, `Nexo.Policies.Dev` |
| Agent execution & transport | `Nexo.Runtime`, `Nexo.Transport.Grpc`, `Nexo.Transport.Grpc.Server` |
| Default adapters | `Nexo.Infrastructure` |
| Orchestration | `Nexo.Orchestration` |
| Background agents (library) | `Nexo.BackgroundAgents` |
| Tooling for agents | `Nexo.Tools.Assembly`, `Nexo.Tools.Dev` |

**Composition helper (still a library, not your app):**

- **`Nexo.Hosting`** — `AddNexo` and DI wiring. Many hosts use it from NuGet; strict splits may reimplement registration in the application repo.

## Application layer (separate repository)

Keep **product-specific** code out of the runtime repo:

| Concern | Examples |
|--------|----------|
| Deployable hosts | ASP.NET APIs, worker services, Unity glue |
| Product descriptors | Game manifests, bespoke DTOs for your title |
| Private integrations | Internal GIS, billing, tenancy |
| Composition overrides | Custom `IServiceCollection` extensions replacing kernel defaults |

Reference runtime packages from the application repo, e.g.:

- **`Nexo.Runtime.Bundle`** — kernel libraries **without** `Nexo.Hosting` (you compose DI yourself or add `Nexo.Hosting` as an extra package).
- **`Nexo.Hosting.Bundle`** — kernel **plus** `Nexo.Hosting` when you want the stock `AddNexo` graph.

## NuGet metapackages

| Package | Purpose |
|---------|---------|
| `Nexo.Runtime.Bundle` | Execution kernel only (no `Nexo.Hosting`). |
| `Nexo.Hosting.Bundle` | Kernel + `Nexo.Hosting` for turnkey embedding. |

Version all packages from the same release (same `PackageVersion` when packing).

## Solution files

| File | Purpose |
|------|---------|
| `Nexo.sln` | Full monorepo (tests, API, CLI, clients, etc.). |
| `Nexo.Kernel.sln` | Mid-sized kernel-focused subset (existing). |
| `Nexo.Runtime.sln` | **Runtime kernel graph** for CI/publish of libraries consumed by application repos. |
