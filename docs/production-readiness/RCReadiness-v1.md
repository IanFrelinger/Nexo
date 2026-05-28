# Release candidate readiness v1

Track after [Security readiness v1](SecurityReadiness-v1.md). **Plan:** [RC hardening plan v1](RCHardeningPlan-v1.md)

**Testing strategy:** RC proof is **workflow evidence + gates**, not repo-wide line coverage. See [Testing strategy pivot v1](../architecture/TestingStrategyPivot-v1.md) and the RC → workflow map in [Testing strategy tracking v1](../architecture/TestingStrategyTracking-v1.md#release-candidate-checklist--automation).

## Command

```bash
make rc-gate-full
# Faster local stack (skips Docker tiers):
NEXO_READY_SKIP_DOCKER=1 make nexo-ready-gate
# Kernel coverage evidence before RC:
make kernel-coverage-gate
```

## Sign-off

- [x] `make waterproofing-gate-full` green (local, SHA `bec2a6ed`)
- [x] [Release sign-off v1](ReleaseSignOff-v1.md) and [Rollback drill v1](RollbackDrill-v1.md) recorded
- [ ] `rc-gate-tier-d` strict on `master` (`RC_GATE_TRIGGER_GH=1 make rc-gate-tier-d`)
- [ ] GitHub RC workflows green on `master` (see `.nexo/rc-gate/github-workflows.txt`)

## Next: post-RC waterproofing

```bash
make waterproofing-gate-full
```

See [Perf readiness](PerfReadiness-v1.md), [Compat readiness](CompatReadiness-v1.md), [DR readiness](DRReadiness-v1.md).
