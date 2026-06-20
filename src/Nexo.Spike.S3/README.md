# S3 — Skill registry + reuse loop

Offline skill registry that admits only promotion-contract-certified bricks, looks up
existing capabilities before regenerating, and reuses stored skills without
re-certification.

## Components

| Piece | Role |
| --- | --- |
| `SkillRegistry` | File-backed catalog under `artifacts/s3/registry/` |
| `IntentMatcher` | Deterministic lookup (see `docs/spike/s3-intent-matching.md`) |
| `ScriptedStandInSkillGenerator` | Stand-in generation (`scripted-standin`, not a model) |
| `SkillCertificationHarness` | Reuses S0→S2 gates + S1 density/escape envelope |
| `SkillReuseLoop` | `EnsureSkill` → lookup → generate → certify → admit |

## Run (headless, offline)

```bash
dotnet run --project src/Nexo.Spike.S3 -- --out artifacts/s3 --reset-registry
```

Writes `artifacts/s3/skill-loop-report.json` with four scripted outcomes:
generated+admitted, reused (gen skipped), reused-other-context, rejected.

## Tests

```bash
dotnet test src/Nexo.Tests.Spike.S3
```
