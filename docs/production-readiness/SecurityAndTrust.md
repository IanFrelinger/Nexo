# Security and trust

Aligns with high-level trust boundaries in `docs/architecture/TrustAndExecutionBoundaries.md`. Expand with **your** deployment topology.

## Checklist

### Secrets and identity

- [ ] No secrets in git; use secret manager or sealed secrets appropriate to your platform.
- [ ] CI/CD uses least privilege; rotate credentials; prefer OIDC over static PATs where supported.
- [ ] Service-to-service auth documented (mesh, agent server, portal, API).

### Threat model (lightweight)

- [ ] Assets listed (data, keys, audit logs, model outputs).
- [ ] Trust boundaries drawn (browser → portal → API → agents → mesh peers).
- [ ] Top abuse cases noted (credential stuffing, SSRF to internal services, prompt injection as a policy concern).

### Supply chain

- [ ] Dependency updates automated (e.g. Dependabot) with review policy.
- [ ] Critical base images pinned by digest where practical.
- [ ] Optional: SBOM for shipped images; optional: build provenance (SLSA-style).

### Application security

- [ ] TLS for external HTTP surfaces; modern TLS policy at load balancer.
- [ ] Security headers and CORS policy reviewed for portal/API.
- [ ] Rate limiting / abuse controls if the control plane is internet-exposed.

### Dependency and vulnerability process

- [ ] Owner for triage of `dotnet list package --vulnerable` (or equivalent) on a schedule.
- [ ] SLA for critical CVEs (patch within N days).

## Fill in (org-specific)

| Item | Your value |
| ---- | ---------- |
| Secret store (e.g. Vault, cloud SM) | |
| WAF / edge (if any) | |
| Security contact / escalation | |
