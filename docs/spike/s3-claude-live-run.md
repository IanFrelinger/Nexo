# S3.2 live Claude generation (local only)

## Prerequisites

- `ANTHROPIC_API_KEY` set in the environment (never committed)
- `NEXO_S3_GENERATOR=claude` (opt-in gate)
- Optional: `NEXO_S3_MODEL` (default `claude-sonnet-4-20250514`), `NEXO_S3_TEMPERATURE` (default `0`)

## Run

```bash
make s3-generate-live
```

Produces:

- `artifacts/s3/skill-loop-report.json` with version `s3.2-claude-v1`
- Transcripts under `artifacts/s3/generate-live/<handoff-id>/` (no API key)
- Registry entry with `isolationEnforced: true` when admitted

## Isolation

The Anthropic prompt is assembled solely from the sealed `GenerationRequest` (intent spec without acceptance criteria). `RequestIsolationTests` assert the assembled prompt contains no oracle, test, property, or acceptance content.

## CI / cloud

Never set `NEXO_S3_GENERATOR=claude` in CI. Default `recorded` backend runs via `make s3-loop-recorded`.
