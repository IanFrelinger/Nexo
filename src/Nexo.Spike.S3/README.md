# S3 — Skill registry + reuse loop

Offline skill registry with deterministic lookup and sealed generation seam.

## Backends

| Backend | Env gate | Isolation enforced | Where |
| --- | --- | --- | --- |
| `recorded` (default) | _(unset)_ | `false` | CI / cloud |
| `claude` | `NEXO_S3_GENERATOR=claude` | `true` | **Local only** |

## Recorded loop (CI/cloud)

```bash
make s3-loop-recorded
# or: dotnet test src/Nexo.Tests.Spike.S3
```

## Live Claude generation (local, keyed)

```bash
export ANTHROPIC_API_KEY=...
export NEXO_S3_MODEL=claude-sonnet-4-20250514   # optional
make s3-generate-live
```

Transcripts: `artifacts/s3/generate-live/`. API key is read at call time only — never persisted.

## Sealed generation seam

`ISkillGenerator`: `Describe()` → sealed `request.json` (intent only) → `Ingest()` → candidate.

Prompt isolation: `docs/spike/s3-intent-matching.md` + `RequestIsolationGuard`.
