# Review Mode — Always Demo a Working Slice

**Goal:** Always have a presentable, working vertical slice for reviews — even while features are in flight — without weakening quality gates in CI.

## Concepts
- **PhaseGuard:** Wraps each phase (Plan→Validate). On failure (when `review.failSoft=true`) it falls back to **last_good** artifacts or tasteful placeholders, so the run remains playable.
- **Promotion:** Strictly green runs promote artifacts to `Artifacts/last_good/` for future fallback.
- **Canary Seeds:** Small set of deterministic seeds used for review to test variance.
- **Gates:** Functional/design/engagement checks produce JUnit and JSON; strict PR CI still fails on low quality.

## Commands

**Run for review (always-green lane):**
```bash
./scripts/run-for-review.sh

Outputs:
	•	Artifacts/<runId>/.../output.json
	•	playmode-smoke.junit.xml (plus design/functional/engagement cases)
	•	review_summary.json (per-seed verdicts, fallbacks used)
	•	Optional Presentation/boomer-slice/ via present-bundle.sh
```

**Strict CI gate (PR lane):**
```bash
./scripts/ci-verify.sh

	•	UTF PlayMode preferred → smoke fallback; exits non-zero on failing thresholds.
```

**Aggregate multi-seed JUnit (review lane):**
```bash
scripts/aggregate-junit.sh playmode-review-aggregate.junit.xml Artifacts/*/playmode-smoke.junit.xml
```

**Sanity check last_good promotion:**
```bash
scripts/check-promotion.sh
```

## Review Day Flow
1. Update `nexo.pipeline.json` prompt/seed (and review block).
2. `./scripts/run-for-review.sh`
3. `./scripts/aggregate-junit.sh`
4. `./scripts/present-bundle.sh`
5. Present: open `Presentation/boomer-slice/` (stills, artifacts, JUnit, summaries).

## Raising the Bar
- Edit `validation.policy.json` to bump `minEngagement` and `minGameplayQuality`.
- Tighten `AcceptanceSpec` (`MinInteractions`, `MustTriggerIds`) and design-lint gates.
- Keep canary seeds stable while tuning; expand later.