# Coverage gates v1

Automated line-coverage floors for kernel assemblies. These gates complement functional workflows (`kernel-gate`, `rc-gate`, `production-readiness-gate-v1`) and block coverage regressions on merge.

**Strategy context:** floors are one layer in the [testing strategy pivot v1](../architecture/TestingStrategyPivot-v1.md) — not a substitute for ProdStyle, mesh-lab, or RC evidence.

## CI workflows

| Workflow | Job id | Assemblies | Line threshold |
|----------|--------|------------|----------------|
| [Core domain coverage](../../.github/workflows/core-domain-coverage.yml) | `domain-coverage` | `Nexo.Core.Domain` | **100%** |
| [Kernel coverage gate](../../.github/workflows/kernel-coverage-gate.yml) | `kernel-coverage` | Domain + Infrastructure + Core.Application | **100% / 84% / 67%** |

Enable **`domain-coverage`** and **`kernel-coverage`** as required status checks on the default branch (see [TestingModel.md](../architecture/TestingModel.md)).

## Local verification

```bash
bash scripts/ci/kernel-coverage-gate.sh
# or
make kernel-coverage-gate
```

Override thresholds for experiments:

```bash
INFRA_COVERAGE_THRESHOLD=90 APP_COVERAGE_THRESHOLD=70 bash scripts/ci/kernel-coverage-gate.sh
```

## Scope and exclusions

- **In scope:** deterministic unit-testable kernel code under `src/Nexo.Core.Domain`, `src/Nexo.Core.Application`, and `src/Nexo.Infrastructure`.
- **Out of scope for 100% line coverage (integration / environment):** Docker engine adapters, Postgres ephemeral provisioners, Ollama/RunPod live routing, Playwright hosts. These paths are exercised by tiered gates (`kernel-gate-tier-e`, mesh-lab, prod-style) rather than line-coverage alone.
- **Ratchet policy:** Raise `INFRA_COVERAGE_THRESHOLD` and `APP_COVERAGE_THRESHOLD` in `scripts/ci/kernel-coverage-gate.sh` when gap tests land; do not lower floors without release sign-off.

## Release readiness link

For the full RC checklist (workflows, evidence, rollback), see [Release candidate checklist v1](../ReleaseCandidateChecklist-v1.md) and [Testing and quality gates](TestingAndQualityGates.md).
