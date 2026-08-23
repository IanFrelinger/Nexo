# Kernel chaos drill v1 (quarterly)

Operator checklist for game-day validation beyond automated `make kernel-gate-tier-e`. Record results in [Kernel Readiness v1](KernelReadiness-v1.md) or your ticket system.

## Automated (Tier E)

```bash
make kernel-gate-tier-e
KERNEL_GATE_CHAOS_LITE=1 make kernel-gate-tier-e   # mesh network-negative (requires mesh lab up)
```

## Manual drill matrix

| Scenario | Command / action | Expected | Pass |
|----------|------------------|----------|------|
| Single API container loss | `docker compose -f deploy/compose/docker-compose.portal.yml restart ashlar-api` | `/health` recovers &lt; 2 min | [ ] |
| Ollama unavailable | Stop `ollama` service in compose; API degrades gracefully | `/health` or status shows dependency issue, no hang | [ ] |
| Disk full on pipeline store | Fill volume or bad `ASHLAR_PIPELINE_STORE_PATH` | Clear error, no silent corruption | [ ] |
| Mesh peer partition | `make mesh-lab-e2e` then `mesh-lab-verify-network-negative` | Director survives; placement blocks bad peers | [ ] |
| LiteDB resume after crash | Tier B script / production-readiness gate resume | Second process completes run | [ ] |

## RPO / RTO (fill in)

| Component | RPO | RTO | Last restore test |
|-----------|-----|-----|-------------------|
| Mesh director (LiteDB) | | | |
| Pipeline run store | | | |
| Portal / API config | | | |

## Sign-off

- [ ] Drill date: ___________
- [ ] Owner: ___________
- [ ] Gaps filed as issues: ___________
