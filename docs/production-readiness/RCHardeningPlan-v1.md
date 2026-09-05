# Release candidate hardening plan v1

Moves from **security gate green** to **release-ready with evidence** — mirrors [Release candidate checklist v1](../ReleaseCandidateChecklist-v1.md).

**Automation:** `make rc-gate-full`. PRs that touch RC docs, `docs/exceptions.yaml`, or `scripts/rc-gate*.sh` produce a `ci release-bundle` and Security D supply-chain evidence, then run Tier C and Tier E. A/B/D stay dispatch-only.

## Prerequisites

```bash
make security-gate-full
# Tier D requires `gh` and authentication (`gh auth login` or GH_TOKEN).
# Red workflows fail the gate. The old advisory skip is refused.
```

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Full `ashlar-ready-gate` | `make rc-gate-tier-a` |
| B | Ship gate + `ci release-bundle` | `make rc-gate-tier-b` |
| C | Evidence audit (bundle, security, rollback docs) | `make rc-gate-tier-c` |
| D | GitHub Actions RC workflows | `make rc-gate-tier-d` |
| E | Exceptions policy, rollback drill record, sign-off | `make rc-gate-tier-e` |

## Flags

| Variable | Effect |
|----------|--------|
| `RC_GATE_SKIP_PRIOR=1` | Skip tier A (default in `rc-gate-full`) |
| `RC_GATE_SKIP_DOCKER=1` | Tier A: `ASHLAR_READY_SKIP_DOCKER=1` |
| `RC_GATE_RELEASE_BUNDLE_FULL=1` | Tier B: also `ci release-bundle --profile full` |
| `RC_GATE_BUNDLE_JSON` | Tier C: override path to `release-bundle-report.json` |
| `RC_GATE_VULN_REPORT` | Tier C: override path to `vulnerable-packages.txt` (missing or High/Critical fails) |
| `RC_GATE_TRIGGER_GH=1` | Tier D: dispatch + watch on workflow miss |
| `RC_GATE_GH_BRANCH=master` | Branch for `gh run list` |
| `RC_GATE_RUN_PUBLISH=1` | Tier D: require `container-image-publish` green |

## Related

- [Release candidate checklist v1](../ReleaseCandidateChecklist-v1.md)
- [Release and promotion](ReleaseAndPromotion.md)
