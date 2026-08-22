# Composition & mesh hardening plan v1

Validates **pipeline composition** (templates, fan-out/fan-in, agentic/hybrid stages, orchestration) and **async clustered mesh tasks** (fleet registry, placement, leases, worker execution) after [Application Readiness v1](ApplicationReadiness-v1.md).

**Automation:** `make composition-mesh-gate-full`

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Pipeline composition (in-process) | `make composition-mesh-gate-tier-a` |
| B | CLI `pipeline` + `mesh` surfaces | `make composition-mesh-gate-tier-b` |
| C | Mesh fleet control plane (in-process) | `make composition-mesh-gate-tier-c` |
| D | Docker mesh lab (workers, task schedule→placement) | `make composition-mesh-gate-tier-d` |

## Prerequisites

- `make application-gate-full` (or `make kernel-gate-full` minimum)
- Docker + `python3` for Tier D
- Optional: `make bootstrap-mesh-lab-env` for persistent `.env.mesh-lab`

## Flags

| Variable | Effect |
|----------|--------|
| `COMPOSITION_MESH_GATE_SKIP_TIER_D=1` | Skip Docker mesh E2E on full run |
| `COMPOSITION_MESH_GATE_DEEP=1` | After Tier D up, run `mesh-lab-verify-deep.sh` (needs `.env.mesh-lab`) |
| `COMPOSITION_MESH_GATE_STRESS=1` | Run `make mesh-lab-e2e-stress` instead of standard Tier D |

## What each tier proves

**Tier A** — `PipelineTemplateValidator`, `PipelineDecomposer`, `PipelineScheduler`, `PipelineOrchestrator`, fan-in/join strategies, agentic fallback, lifecycle/resume.

**Tier B** — `ashlar pipeline validate/run` command wiring; mesh director URI builder; mesh CLI trust paths; optimize-agent-cluster script layout.

**Tier C** — `MeshTaskRegistry`, placement (elastic, trust, idempotency), execution leases, checkpoint migration, director persistence.

**Tier D** — heterogeneous peers + **workers** profile; HTTP schedule/create mesh tasks; assigned placement across the lab (see `docs/MeshVirtualLab.md`).

## Related

- [Kernel phase 10 — pipeline composition](../architecture/KernelPhaseMatrix.md)
- [Mesh agent setup tear sheet](../MeshAgentSetupCapabilityBreakdown.md)
- `make mesh-lab-e2e-workers`, `make mesh-lab-verify-director-cli`
