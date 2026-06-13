# Private support boundaries (Phase 1.6)

Published severity levels and response targets for **paid Nexo Private** pilots. Adjust in contract exhibits as needed.

## Severity definitions

| Severity | Definition | Examples |
|----------|------------|----------|
| **S1 — Critical** | Production API unavailable or all copilot jobs failing | `/health` down, license gate mis-config blocks all work |
| **S2 — Major** | Degraded service; workaround exists | Elevated 429s, single integration failure, Ollama unreachable |
| **S3 — Minor** | Question, doc gap, non-blocking defect | UI copy, log noise, feature request |

## Response targets (business hours)

| Severity | First response | Update cadence | Resolution target |
|----------|----------------|----------------|-------------------|
| S1 | 4 hours | Every 4 hours | 1 business day (mitigation) |
| S2 | 1 business day | Daily | 5 business days |
| S3 | 2 business days | Weekly | Best effort / next release |

Business hours: **09:00–17:00 customer local time**, Mon–Fri, excluding published holidays.

## In scope

- Nexo API, CLI, and reference compose stack from order form version
- License validation and tenant-scoped copilot/usage APIs
- Diagnostics bundle review (`GET /api/support/diagnostics`)
- Upgrade guidance for **one minor** version jump

## Out of scope (unless separate SOW)

- Customer cloud account administration
- LLM provider outages or quota on customer keys
- Custom agent/brick development
- 24×7 coverage (Enterprise SKU)

## How to open a ticket

Include: severity, diagnostics JSON, license `expiresAt`, compose/image digest, steps to reproduce, and approximate job volume from `/api/usage/summary`.

See also [`on-call-playbook-v0.1.md`](./on-call-playbook-v0.1.md).
