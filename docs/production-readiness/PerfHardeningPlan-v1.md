# Performance hardening plan v1

Regression backstop after **RC gate** — latency/throughput baselines before tag.

**Automation:** `make perf-gate-full`

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Orchestration + background-agent perf tests | `make perf-gate-tier-a` |
| B | Pipeline throughput (mocked) | `make perf-gate-tier-b` |
| C | CLI cold-start (`--help`) | `make perf-gate-tier-c` |
| D | Mini-soak (default) or long soak | `make perf-gate-tier-d` |

## Flags

| Variable | Effect |
|----------|--------|
| `PERF_GATE_SKIP_PRIOR=1` | Skip RC prerequisite (default in `perf-gate-full`) |
| `PERF_GATE_SKIP_TIER_D=1` | Skip soak tier |
| `PERF_GATE_SOAK_MINUTES=30` | Tier D: run 30-minute soak |
| `PERF_GATE_STRICT_BASELINE=1` | Fail on regression vs `.ashlar/perf/baseline.json` |
| `PERF_GATE_UPDATE_BASELINE=1` | Refresh baseline after run |

## Related

- `.github/workflows/perf-certification.yml`
- [Perf readiness v1](PerfReadiness-v1.md)
