# Release candidate readiness v1

Track after [Security readiness v1](SecurityReadiness-v1.md). **Plan:** [RC hardening plan v1](RCHardeningPlan-v1.md)

## Command

```bash
make rc-gate-full
```

## Sign-off

- [ ] `rc-gate-full` green
- [ ] GitHub tier D workflows green (or `RC_GATE_GH_ADVISORY_ONLY=1` with owner)
- [ ] `make waterproofing-gate-full` (perf → compat → DR → RC policy)
- [ ] Manual checklist sections 3–5 in [Release candidate checklist v1](../ReleaseCandidateChecklist-v1.md)

## Next: post-RC waterproofing

```bash
make waterproofing-gate-full
```

See [Perf readiness](PerfReadiness-v1.md), [Compat readiness](CompatReadiness-v1.md), [DR readiness](DRReadiness-v1.md).
