# Production readiness

This folder helps you **support every major production concern** in one place: release discipline, security, operations, data/compliance, reliability, testing, and operator-facing deployment. Use it as a **roadmap and checklist**, then fill in org-specific details (names, URLs, RPO/RTO, tooling).

**Who this is for:** operators, security reviewers, release managers, and engineers hardening self-hosted or air-gapped deployments.

| Area | Guide | One-line goal |
| ---- | ----- | --------------- |
| Release & promotion | [Release and promotion](ReleaseAndPromotion.md) | Reproducible versions, artifacts, rollback, one promotion path |
| Security & trust | [Security and trust](SecurityAndTrust.md) | Secrets, threat model, supply chain, dependency risk |
| Operations | [Operations and observability](OperationsAndObservability.md) | SLOs, alerting, health, capacity, runbooks |
| Data & compliance | [Data privacy and compliance](DataPrivacyAndCompliance.md) | Classification, retention, encryption, control mapping |
| Reliability | [Reliability and chaos](ReliabilityAndChaos.md) | Failure modes, limits, idempotency, drills |
| Testing & CI | [Testing and quality gates](TestingAndQualityGates.md) | Required checks, coverage, perf budgets |
| Operators | [Operator deployment](OperatorDeployment.md) | Production install, upgrades, backups pointer |
| Audience fit | [Catalog by deployment type](CatalogByDeploymentType.md) | SMB vs enterprise vs SaaS: which items matter most |

## How to use this

1. Read [Catalog by deployment type](CatalogByDeploymentType.md) and pick your lane.
2. Open each linked guide for your lane and work through the **checklists** (checkboxes are Markdown `- [ ]` so you can track in a fork or internal doc).
3. Link completed runbooks and decisions from your internal wiki or ticket system; keep this repo as the **canonical structure**, not necessarily every secret or customer name.

## Existing repo docs

- Configuration: `docs/Configuration.md`
- Architecture / trust overview: `docs/architecture/README.md`
- Self-hosted agent server: `docs/SelfHostedAgentServer.md`
- Contributing / local CI: `CONTRIBUTING.md`
