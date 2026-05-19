# Phase 10 — Lab stress hardening

Phase 10 aligns the **weekly stress gate** with the **PR mesh-lab gate** and adds **post-stress** director checks so worker scale/health bursts do not mask director regressions.

**Depends on:** Phase 9 persistence (LiteDB on peer-a), Phase 5 stress ramp, full verify scripts (governance, trust, entitlements, CLI).

## CI workflows

| Workflow | When | What runs |
|----------|------|-----------|
| [`mesh-lab-gate.yml`](../.github/workflows/mesh-lab-gate.yml) | PR / push (path filters) | `mesh-lab-verify.sh` + `mesh-lab-verify-deep.sh` |
| [`mesh-lab-stress-gate.yml`](../.github/workflows/mesh-lab-stress-gate.yml) | Weekly + `workflow_dispatch` | Full verify + deep + stress ramp + **post-stress** |

`mesh-lab-verify.sh` already invokes governance, trust, entitlements (workers), director CLI, and persistence sub-scripts.

## Post-stress verify

[`scripts/mesh-lab-verify-post-stress.sh`](../scripts/mesh-lab-verify-post-stress.sh):

1. Registers an isolated fleet peer and asserts **schedule → Assigned** after the stress ramp.
2. Re-runs [`mesh-lab-verify-persistence.sh`](../scripts/mesh-lab-verify-persistence.sh) (peer-a restart) when LiteDB is enabled.
3. Restores `mesh-lab-verify-peer` for any follow-on manual checks.

## Local commands

```bash
make mesh-lab-e2e-stress          # verify + deep + stress + post-stress (one shot)
make mesh-lab-verify-post-stress  # lab already up after stress
```

Environment:

- `MESH_LAB_SKIP_POST_STRESS_VERIFY=1` — skip in `run-mesh-lab-e2e.sh` when `MESH_LAB_RUN_STRESS=1`
- `MESH_LAB_POST_STRESS_PEER_ID` — placement probe peer id (default `mesh-lab-post-stress-peer`)

## Revision history

| Date | Change |
|------|--------|
| 2026-05-19 | Stress gate parity; post-stress placement + persistence re-check. |
