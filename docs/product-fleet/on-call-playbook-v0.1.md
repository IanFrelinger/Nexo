# On-call playbook v0.1 (Product Fleet Phase 0.5)

Minimal first-response guide for a single-tenant **Ashlar Private** deployment.

## Severity guide

| Severity | Examples | First response |
|----------|----------|----------------|
| S1 | API down, all copilot jobs failing, data loss risk | Page on-call; capture diagnostics within 15 minutes |
| S2 | Degraded latency, quota mis-fires, one tenant blocked | Triage in business hours unless revenue-critical |
| S3 | Doc drift, non-blocking warnings in logs | Ticket; fix in next maintenance window |

## First five minutes

1. **Health** — `GET /health` should return `healthy`.
2. **Status** — `GET /api/status` shows background-agent mode and counts.
3. **Diagnostics** — `GET /api/support/diagnostics` returns a redacted config bundle (no secrets). Attach this JSON to the incident ticket.
4. **Usage** — `GET /api/usage/summary?hours=24` with `X-Ashlar-Tenant` confirms whether jobs are being recorded.
5. **Logs** — search for `AshlarUsage`, `Ashlar.Security`, and `Private license` warnings.

## Common scenarios

### Copilot returns 429

Hourly quota (`Ashlar:Entitlements:MaxCopilotSubmissionsPerHour`) is enforced per tenant. Raise the limit in config or wait for the rolling window.

### Copilot returns 402 / license errors

Private license enforcement is on (`Ashlar:PrivateLicense:EnforceLicense`). Check `license.state` in diagnostics. Renew the license file and restart the API container. Read-only GET routes remain available when `AllowReadOnlyWhenExpired` is true.

### Cross-tenant data concern

Copilot task history and usage are scoped by `X-Ashlar-Tenant`. Verify `Ashlar:Product:AllowedTenantIds` matches the customer contract. Run `ProductFleetTenantIsolationTests` in CI after config changes.

### Ollama / model unavailable

Private reference stack depends on `OLLAMA_BASE_URL`. Confirm the Ollama container is healthy and the model tag in `OLLAMA_MODEL` is pulled.

## Escalation data to collect

- Diagnostics JSON (`/api/support/diagnostics`)
- Last 200 lines of API container logs
- Compose file version and image digests
- License expiry (`expiresAt` only — do not paste HMAC secrets)
- Approximate job volume from usage summary

## Related

- [`private-reference-deployment.md`](./private-reference-deployment.md)
- [`private-backup-restore.md`](./private-backup-restore.md)
- [`private-byok-security.md`](./private-byok-security.md)
