# Catalog by deployment type

Use this to prioritize production-readiness work. Every deployment benefits from **release discipline** and **backups**; the rest scales with exposure and regulation.

## Self-hosted / internal (small team, low external exposure)

**Must have**

- [Release and promotion](ReleaseAndPromotion.md) — tagged images, rollback
- [Operator deployment](OperatorDeployment.md) — compose or equivalent, health checks
- [Testing and quality gates](TestingAndQualityGates.md) — required CI on default branch

**Should have**

- [Operations and observability](OperationsAndObservability.md) — logs, disk alerts, basic runbooks
- [Security and trust](SecurityAndTrust.md) — secrets handling, TLS for any external HTTP

**Nice to have**

- [Reliability and chaos](ReliabilityAndChaos.md) — lightweight failure drills
- [Data privacy and compliance](DataPrivacyAndCompliance.md) — retention for logs and audit stores

## Enterprise / regulated (customer data, procurement, audits)

**Must have**

Everything in “self-hosted,” plus:

- [Security and trust](SecurityAndTrust.md) — written threat model, supply chain, access control
- [Data privacy and compliance](DataPrivacyAndCompliance.md) — classification, retention, encryption at rest, control mapping (SOC2-style or ISO-style)
- [Operations and observability](OperationsAndObservability.md) — SLOs, on-call, incident runbooks, backup/restore drills

**Should have**

- [Reliability and chaos](ReliabilityAndChaos.md) — periodic chaos or game days
- SBOM / provenance for shipped artifacts (see Security guide)

## SaaS or internet-exposed control plane

**Must have**

Everything in “enterprise,” plus:

- [Security and trust](SecurityAndTrust.md) — abuse controls (rate limits, auth hardening), WAF or edge policy as appropriate
- [Reliability and chaos](ReliabilityAndChaos.md) — multi-AZ or equivalent, clear RPO/RTO for stateful components
- [Operations and observability](OperationsAndObservability.md) — 24/7 paging policy and escalation

## Air-gapped or no-egress

**Must have**

- [Operator deployment](OperatorDeployment.md) — image transfer, offline registry, config without phone-home
- [Security and trust](SecurityAndTrust.md) — document allowed egress (ideally none) and update channels
- [Testing and quality gates](TestingAndQualityGates.md) — CI lanes that match air-gapped constraints (you may already mirror these in `.github/workflows/`)

Cross-check with existing air-gapped workflow intent in the repo and your operator runbooks.
