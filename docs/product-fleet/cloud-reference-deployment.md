# Cloud multi-tenant reference deployment (Phase 2)

Local **staging shape** for Ashlar Cloud: multiple orgs, membership enforcement, shared API process.

## Prerequisites

- Docker and Docker Compose
- Same base requirements as Private reference stack

## Quick start

```bash
export OLLAMA_MODEL=llama3.1:latest
export ASHLAR_API_KEY="$(openssl rand -hex 32)"   # required: the stack refuses to start without it
docker compose -f deploy/compose/docker-compose.cloud-multi-tenant.yml up --build -d
curl -sS http://127.0.0.1:8080/health
```

Every mutating call below also needs `-H "X-Ashlar-Api-Key: $ASHLAR_API_KEY"` (built-in `AuthorizationMode=ApiKey`, `MutatingApi` scope).

## Control plane flow (Phase 2.3)

1. **Create org** (creator becomes admin when `X-Ashlar-User` is set):

```bash
curl -sS -X POST http://127.0.0.1:8080/api/orgs \
  -H 'Content-Type: application/json' \
  -H 'X-Ashlar-User: alice@acme.example' \
  -d '{"name":"Acme Cloud"}'
```

2. **Add a member** (admin only):

```bash
curl -sS -X POST "http://127.0.0.1:8080/api/orgs/<orgId>/members" \
  -H 'Content-Type: application/json' \
  -H 'X-Ashlar-User: alice@acme.example' \
  -H 'X-Ashlar-Org: <orgId>' \
  -d '{"userId":"bob@acme.example","role":"Member"}'
```

3. **Copilot job** (requires org membership; tenant must match org scope):

```bash
curl -sS -X POST http://127.0.0.1:8080/api/copilot/task \
  -H 'Content-Type: application/json' \
  -H 'X-Ashlar-User: bob@acme.example' \
  -H 'X-Ashlar-Org: <orgId>' \
  -H 'X-Ashlar-Tenant: <tenantId from org response>' \
  -d '{"task":"Summarize usage"}'
```

## Cloud vs Private headers

| Header | Private (default) | Cloud (`RequireOrgMembership=true`) |
|--------|-------------------|-------------------------------------|
| `X-Ashlar-Tenant` | Required for isolation | Must match org `tenantId` |
| `X-Ashlar-User` | Optional | Required on copilot/usage |
| `X-Ashlar-Org` | N/A | Required on copilot/usage |

Staging headers are **not** production auth — replace with SSO (Phase 3.1) before GA. `X-Ashlar-Tenant` (and `X-Ashlar-User` / `X-Ashlar-Org`) is client-asserted: trust it only behind built-in auth or an authenticating proxy that sets it.

## Configuration

| Setting | Cloud reference value |
|---------|----------------------|
| `Ashlar__Product__RequireOrgMembership` | `true` |
| `Ashlar__Entitlements__DeploymentMode` | `Cloud` |
| `Ashlar__Entitlements__MaxCopilotSubmissionsPerHour` | per tier |

## Related

- [`cloud-aws-account-structure.md`](./cloud-aws-account-structure.md)
- [`private-reference-deployment.md`](./private-reference-deployment.md)
