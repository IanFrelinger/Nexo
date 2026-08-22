# Kernel hardening plan v1

Pre-application validation for the Ashlar execution kernel (`src/`, `Ashlar.Hosting`). Goal: prove DI contracts, deployment profiles, and core execution paths before building product features under `application/src/`.

**Tracking:** update [Kernel Readiness v1](KernelReadiness-v1.md) as tiers complete.

**Automation:** `make kernel-gate` (see [Testing.md](../Testing.md#kernel-gate-pre-application)).

---

## Tier A — Kernel contracts (required before apps)

| # | Item | Deliverable | Verification |
|---|------|-------------|--------------|
| A1 | Phase coverage matrix | [KernelPhaseMatrix.md](../architecture/KernelPhaseMatrix.md) | Doc + `KernelPhaseResolutionTests` |
| A2 | Profile behavioral matrix | `HostingDeploymentProfileTests`, `KernelPhaseResolutionTests` | `make kernel-gate` |
| A3 | Strict-mode contract | `StrictModeE2ETests` (existing) | ProdStyle / prime-time |
| A4 | Single kernel gate | `Makefile` target `kernel-gate` | Local + CI `kernel-gate.yml` |

**Tier A status (2026-05-19):** implemented; see [Kernel Readiness v1](KernelReadiness-v1.md).

**Tier B status (2026-05-19):** `make kernel-gate-tier-b` / `scripts/kernel-gate-tier-b.sh` — build, pipeline lifecycle, CLI ops, cross-process LiteDB resume.

**Tier C status (2026-05-19):** `make kernel-gate-tier-c` / `scripts/kernel-gate-tier-c.sh` — ProdStyle Infrastructure, workflow executor, gRPC transport, air-gapped profile; mesh when `.env.mesh-lab` exists (`make bootstrap-mesh-lab-env`).

**Tier D status (2026-05-19):** `make kernel-gate-tier-d` / `scripts/kernel-gate-tier-d.sh` — `Ashlar.Runtime.sln`, pack graph alignment, `StableSdkHostSample` consumer from local feed.

## Tier B — Execution and state

| # | Item | Deliverable | Verification |
|---|------|-------------|--------------|
| B1 | Loop / workflow invariants | Extend `WorkflowExecutorIntegrationTests` | `dotnet test --filter WorkflowExecutor` |
| B2 | Cross-process pipeline resume | [ProductionReadinessGate-v1.md](../ProductionReadinessGate-v1.md) | `production-readiness-gate-v1.yml` |
| B3 | Pipeline certification | Pipeline tests net8 + net9 | `kernel-gate` pipeline filter |

## Tier C — Trust and network

| # | Item | Deliverable | Verification |
|---|------|-------------|--------------|
| C1 | Trust negatives | `test-trust-multi-env.yml` | Scheduled / manual |
| C2 | Mesh on fleet changes | `mesh-lab-gate.yml` path filters | CI |
| C3 | Air-gapped proof | `test-air-gapped-no-network.yml` | CI |

## Tier D — Consumption

| # | Item | Deliverable | Verification |
|---|------|-------------|--------------|
| D1 | NuGet consumer drill | `nuget-consumer-verify.yml` | Release + monthly |
| D2 | Runtime vs application boundary | `layer-boundary.yml`, `Ashlar.Runtime.sln` | `kernel-gate` build |

## Tier E — Operations (advisory until v2)

| # | Item | Deliverable | Verification |
|---|------|-------------|--------------|
| E1 | Observability contract | `OpenTelemetryTests`, prod-dry-run | `make kernel-gate-tier-e` |
| E2 | Perf budgets | `Ashlar.Tests.Orchestration.Performance` in tier-e | `make kernel-gate-tier-e` |
| E3 | Chaos / game day | [KernelChaosDrill-v1.md](KernelChaosDrill-v1.md) | Quarterly manual + optional `KERNEL_GATE_CHAOS_LITE=1` |

**Tier E status:** `make kernel-gate-tier-e` / `scripts/kernel-gate-tier-e.sh` — OpenTelemetry, orchestration performance tests, `prod-dry-run.sh --portal`.

---

## Suggested timeline

| Week | Focus | Command |
|------|--------|---------|
| 1 | Tiers A + D2 | `make kernel-gate` |
| 2 | Tier B | `make ci-verify`, production-readiness gate |
| 3 | Tier C | `make mesh-lab-e2e-workers` |
| 4 | Sign-off | Update Kernel Readiness v1 |

---

## Optional kernel-gate flags

| Variable | Effect |
|----------|--------|
| `KERNEL_GATE_MESH=1` | After core gate, run `make mesh-lab-verify` (Docker required) |
| `KERNEL_GATE_PRODSTYLE=1` | Run `make test-prod-style` (longer) |

---

## Related docs

- [Runtime vs application](../architecture/runtime-vs-application.md)
- [Testing model](../architecture/TestingModel.md)
