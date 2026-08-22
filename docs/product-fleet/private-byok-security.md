# Private BYOK security one-pager (Phase 1.3)

**Bring your own key (BYOK)** means LLM provider credentials stay on the customer-controlled host. Ashlar orchestrates calls; it does not need to custody provider secrets in Ashlar-operated cloud infrastructure for Private deployments.

## What stays on the customer host

| Data | Typical storage | Leaves host? |
|------|-------------------|--------------|
| OpenAI / Anthropic / Ollama API keys | Env vars, Docker secrets, or host keychain | Only to the configured provider endpoint |
| Copilot task text and outputs | LiteDB / volumes per deployment profile | No Ashlar cloud copy by default |
| Audit and usage counters | Local LiteDB or in-memory | Export only when customer runs compliance export |
| Private license file | `ASHLAR_LICENSE_FILE` mount | Never transmitted automatically |

## What Ashlar operators may see (support)

With customer consent, support may receive:

- Redacted diagnostics from `GET /api/support/diagnostics` (secrets stripped)
- Aggregated usage counts (jobs per 24h)
- Log excerpts with PII redacted per customer policy

Support must **not** request raw provider API keys.

## Hardening checklist

1. Set `Ashlar:Security:AuthorizationMode` to `ApiKey` (or stronger) for production.
2. Bind API to localhost or private network; use reverse proxy TLS for remote access.
3. Restrict `Ashlar:Product:AllowedTenantIds` to the licensed tenant.
4. Enable `Ashlar:PrivateLicense:EnforceLicense` with HMAC-signed license files for production pilots.
5. Rotate API keys on the same schedule as other production secrets.
6. Keep Ollama / model endpoints on the internal Docker network (reference compose does this by default).

## Related

- [`private-reference-deployment.md`](./private-reference-deployment.md)
- [`sample-private-license.json`](./sample-private-license.json)
