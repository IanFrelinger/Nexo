# Operator deployment (production)

This document is the **anchor** for operators. Link out to detailed guides you already maintain; keep procedures out of code comments.

## Checklist

### Before first production deploy

- [ ] Read `docs/Configuration.md` and set environment variables for prod (strict mode, providers, trust).
- [ ] Choose stack: `deploy/compose/docker-compose.portal.yml` (or agent-server / ephemeral / cloud) or your orchestrator manifests. See `docs/DEPLOYMENT.md`.
- [ ] TLS termination decided (reverse proxy vs platform ingress).
- [ ] Secrets injected via your secret store, not `.env` in source control.

### Day-2 operations

- [ ] Upgrade procedure: pull new images → migrate state if needed → rolling restart → smoke test.
- [ ] Backup scope: volumes, databases, object stores holding audit or user data.
- [ ] Monitoring dashboards linked from [Operations and observability](OperationsAndObservability.md).

### Air-gapped or restricted network

- [ ] Image transfer and internal registry documented.
- [ ] Update path without public internet (mirror, USB, etc.).

## Fill in (org-specific)

| Item | Your value |
| ---- | ---------- |
| Production compose file or chart name | |
| Primary URL for portal | |
| Escalation contact | |

## Existing references

- `docs/SelfHostedAgentServer.md`
- `README.md` — Deploy (operators) section
- `docs/Configuration.md`
