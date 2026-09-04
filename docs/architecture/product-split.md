# Framework vs product split

Ashlar is the **framework**. Guard, Forge, Mesh Exchange, the air-gapped
workstation, the cluster engine, the hosted control plane, and the native host
are **products**. This document is the placement rule for new code.

## Rule

If a type defines how an **arbitrary** Ashlar workload executes, certifies,
routes, or is verified, it belongs in the framework (`src/`). If it is
user-facing product UX, a tenant, billing, an installer, or a deployment of one
of those products, it belongs in a product tree (today: `products/`; later: its
own repository).

```text
ashlar-cloud  →  ashlar-cluster  →  ashlar (this repo's src/)
ashlar-workstation  →  ashlar
ashlar-native  →  ashlar
```

Ashlar must never reference a product project. Products consume
`Ashlar.Hosting`, `Ashlar.Contracts`, and `Ashlar.Client` only. The
dependency-boundary gate rejects `src/` → `products/` and non-test `src/` →
`application/` `ProjectReference`s. The existing exception is
`Ashlar.Tests.Infrastructure` hosting `Ashlar.API` in-process.

## Stay in this repository (framework)

- Agent, brick, tool, workflow, and pipeline contracts
- Execution-envelope and result-evidence schemas (`Ashlar.Contracts.Distributed`)
- Local / peer / cluster routing interfaces
- Certification, signatures, provenance
- Artifact manifests and verification
- Sandbox abstractions
- Durable task lifecycle **ports** (`ITaskScheduler`) — not a Kubernetes scheduler
- Native execution **ports** (`INativeExecutionHost`) — not a `dlopen` plugin loader
- Deployment profiles, including `SecureWorkstation`
- MCP / A2A / gRPC adapters
- Local model and RAG abstractions

`AirGapped` is a slim offline profile. It is **not** the workstation profile:
it excludes trust, background agents, RAG, and observation.
`SecureWorkstation` keeps those local capabilities and still excludes runtime
transport / cloud egress.

## Extractable product trees (`products/`)

| Tree | Future repo | Consumes | Ships |
|------|-------------|----------|-------|
| `products/ashlar-workstation` | `ashlar-workstation` | `SecureWorkstation`, IDE contracts | Daemon UX, VS Code extension, installers |
| `products/ashlar-cluster` | `ashlar-cluster` | `ITaskScheduler`, envelopes | Scheduler, GPU workers, k8s |
| `products/ashlar-cloud` | `ashlar-cloud` | Cluster protocol + org/billing stubs | Hosted control plane, OIDC, quotas |
| `products/ashlar-native` | `ashlar-native` | `INativeExecutionHost` | WASM / out-of-process workers |

Existing in-repo surfaces that will move with those products later (not in this
increment):

- `extensions/ashlar-vscode/` → workstation
- `application/src/Ashlar.API` IDE endpoints → workstation host or stay as the
  open single-node API
- `commercial/` Fleet / MeshDirector → cluster overlay or a commercial repo
  (not moved here)

`gh` cannot create the GitHub repositories from this agent. Extraction follows
the release-manager pattern: grow the tree here, then split when the consumer
shape is stable.

## Native code

The kernel hot-loads **managed** assemblies in an `AssemblyLoadContext` and
rejects P/Invoke in `IlImportFence`. Generated native code must not be
`dlopen`ed into the IDE or API process. Use WebAssembly or an out-of-process
worker via `INativeExecutionHost`.

## See also

- [`runtime-vs-application.md`](runtime-vs-application.md)
- [`../ProjectTiers.md`](../ProjectTiers.md)
- [`../OpenCoreBoundary.md`](../OpenCoreBoundary.md)
- [`KernelPhaseMatrix.md`](KernelPhaseMatrix.md)
