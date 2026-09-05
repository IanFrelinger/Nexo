# Release candidate hardening plan v1

Moves from **security gate green** to **release-ready with evidence** — mirrors [Release candidate checklist v1](../ReleaseCandidateChecklist-v1.md).

**Automation:** `make rc-gate-full`

## Prerequisites

```bash
make security-gate-full
# Tier D requires `gh` and authentication (`gh auth login` or GH_TOKEN).
```

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Full `ashlar-ready-gate` | `make rc-gate-tier-a` |
| B | Ship gate + `ci release-bundle` | `make rc-gate-tier-b` |
| C | Evidence audit (bundle, security, rollback docs) | `make rc-gate-tier-c` |
| D | GitHub Actions RC workflows | `make rc-gate-tier-d` |

## Flags

| Variable | Effect |
|----------|--------|
| `RC_GATE_SKIP_PRIOR=1` | Skip tier A (default in `rc-gate-full`) |
| `RC_GATE_SKIP_DOCKER=1` | Tier A: `ASHLAR_READY_SKIP_DOCKER=1` |
| `RC_GATE_RELEASE_BUNDLE_FULL=1` | Tier B: also `ci release-bundle --profile full` |
| `RC_GATE_STRICT_EVIDENCE=1` | Tier C fails on bundle FAIL |
| `RC_GATE_STRICT_SECURITY=1` | Tier C fails on High/Critical CVEs |
| `RC_GATE_TRIGGER_GH=1` | Tier D: dispatch + watch on workflow miss |
| `RC_GATE_GH_ADVISORY_ONLY=1` | Tier D: warn instead of fail |
| `RC_GATE_GH_BRANCH=master` | Branch for `gh run list` |
| `RC_GATE_RUN_PUBLISH=1` | Tier D: require `container-image-publish` green |

## Related

- [Release candidate checklist v1](../ReleaseCandidateChecklist-v1.md)
- [Release and promotion](ReleaseAndPromotion.md)
