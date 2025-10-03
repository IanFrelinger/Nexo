# Review Mode

**Goal:** Always demo a working, feature-complete slice—even when features are in flight.

## How it works
- **PhaseGuard** wraps each phase (Plan→Validate). On error, if `review.failSoft=true`:
  - use **last_good** artifact for that phase, or
  - create a tasteful **fallback** (tokens-compliant).
- **Promotion:** strictly green runs promote artifacts to `Artifacts/last_good/`.

## Commands
- Run for review (always-green): `./scripts/run-for-review.sh`
- Strict CI gate: `./scripts/ci-verify.sh`
- Aggregate multi-seed JUnit: `./scripts/aggregate-junit.sh`

## Outputs
- `review_summary.json` (per-seed verdicts, fallbacks used)
- `playmode-smoke.junit.xml` (assertions)
- `Presentation/boomer-slice/` (bundle)

## Raising the bar
- Edit `validation.policy.json` (min scores)
- Tighten `AcceptanceSpec` and design-lint gates
