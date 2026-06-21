# Merge readiness: certification tower (PRs #186–#191)

**Status:** Ready for human merge to `master` — **do not auto-merge**.

## Stack topology (STEP 0)

| Item | Value |
|------|-------|
| Merge-base with `origin/master` | `5bd1a103f53030bb43fd037b3a287c1377bbff78` |
| Tower tip (pre-integration) | `9baf34a9` on `cursor/merge-readiness-921c` |
| Landing strategy | **ONE merge** — single stacked tower (30 feature commits + 6 merge-readiness commits), not N independent branches |
| Ordered span | `4f8223a4` (portability spike) … `9baf34a9` (merge-readiness cleanup) |

The tower is linear: each PR branch was stacked, not independent forks off `master`.

## Merge plan

1. **Human** merges `cursor/integration-cert-tower-921c` → `master` (or merges `cursor/merge-readiness-921c` — equivalent content).
2. Expect **36 commits** fast-forward (or one squash at maintainer discretion).
3. Post-merge: confirm **`cert-gate`** workflow is green on `master`.
4. Ignore unrelated reds documented in [`docs/ci-pre-existing-failures.md`](docs/ci-pre-existing-failures.md).

## Conflicts

**None.** Integration was a clean fast-forward:

```
git checkout -b cursor/integration-cert-tower-921c origin/master
git merge cursor/merge-readiness-921c
# Updating 5bd1a103..9baf34a9 — Fast-forward
```

No certification logic was modified to resolve conflicts.

## cert-gate confirmation (integration branch)

Run on `cursor/integration-cert-tower-921c` @ `9baf34a9` (2026-06-21):

```bash
bash scripts/run-cert-gate.sh
```

| Metric | Result |
|--------|--------|
| Exit code | **0** |
| Tests executed | **19** |
| Tests passed | **19** |
| Zero-test guard | **PASS** (`expected>=19`, derived from `--list-tests`) |
| `HonestCursorGeneration_Admits_WithZeroEscapeRate` | **PASS** |
| `BuggyCursorGeneration_Rejects` | **PASS** |

**Dogfood verdict on integration:** `honest=ADMIT`, `buggy=REJECT`.

## Merge-readiness commits (docs/labeling only)

| Commit | Message |
|--------|---------|
| `d5a4244b` | `docs(cert): promote evidence ledger out of spikes/` |
| `4087c825` | `docs(cert): label generator test doubles explicitly` |
| `dabf91b5` | `docs(cert): record v0 seam limitations` |
| `d31c5064` | `ci(cert): single source of truth for MIN_EXPECTED` |
| `9baf34a9` | `docs(ci): document pre-existing red dry-run jobs` |

## What was NOT done (by design)

- No merge to `master`
- No changes to gate, mutation engine, witness, test assertions, or generated brick logic
- No weakening of certification tests

## Branches

| Branch | Purpose |
|--------|---------|
| `cursor/merge-readiness-921c` | Cleanup commits on tower tip |
| `cursor/integration-cert-tower-921c` | Verified `master` + tower fast-forward |

Human pulls the merge trigger.
