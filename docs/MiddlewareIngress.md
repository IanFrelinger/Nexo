# Middleware ingress (HTTP, MediatR, WebSocket lab, SMS lab, AWS follow-on)

This document closes the operational gaps around **multi-transport middleware**: how headers flow into Nexo, how MediatR sees them, what is implemented in-process versus what belongs in AWS, and how to gate spend.

## In-repo surfaces

| Surface | Route / artifact | Purpose |
|--------|-------------------|---------|
| Correlation | `X-Correlation-Id` (in + out), `Activity` tag `nexo.correlation_id` | End-to-end tracing. |
| Ingress envelope | `X-Nexo-Tenant`, `X-Nexo-App-Id`, `X-Idempotency-Key`, `X-Nexo-Payload-Version` | Adapter-agnostic metadata on HTTP requests. |
| Operator echo | `GET /api/middleware/correlation-echo` | Quick sanity check. |
| Operator snapshot | `GET /api/middleware/ingress-context` | JSON view of mapped ingress (tests + debugging). |
| Catalog | `GET /api/middleware/ingress-catalog` | Static list of ingress seams (HTTP, Forge, WS lab, SMS lab, Swagger). |
| OpenAPI | `/swagger/v1/swagger.json`, Swagger UI | Contract visibility for integrators. |
| WebSocket lab | `GET /ws/v1/echo` | Feature-flagged echo (JSON hello + text echo). |
| SMS lab | `POST /api/ingress/sms/simulate` | Parses `YES <token>`; in-memory idempotent store. **Not** signed AWS callbacks. |

Configuration: `Nexo:MiddlewareIngress` in `appsettings.json` (`EnableWebSocketIngress`, `EnableSmsSimulationIngress`, `SmsSimulationAllowedAppIds`, `DisabledCapabilities`, `TenantCapabilityAllowlists`).

## MediatR and `INexoIngressAccessor`

- **`INexoIngressAccessor`** exposes correlation, transport, tenant, app id, idempotency, and payload version. Nexo.API registers **`HttpNexoIngressAccessor`** before `AddNexo()` so it overrides the default no-op.
- **`IngressLoggingPipelineBehavior`** (registered in `AddNexo`) wraps every MediatR request in a logging scope when `CorrelationId` is present, so handlers and validators inherit structured logging fields without changing individual commands.
- CLI and non-web hosts keep **`NoOpNexoIngressAccessor`** via `TryAddSingleton` when no HTTP implementation is registered.

Handlers that need full detail can also call `HttpContext.GetIngressEnvelope()` in the API layer.

## AWS: production-style inbound SMS (not shipped in-process)

The lab endpoint exists to **parse and unit-test** approval keywords. A production path typically looks like:

1. **End User Messaging (SMS)** two-way number → inbound publishes to **SNS topic**.
2. **Lambda** (SNS subscription) validates sender, rate limits, parses `YES <run-id>`, writes idempotent state (**DynamoDB** or **SSM**), optionally calls **Step Functions** `SendTaskSuccess` or the **GitHub API** (fine-grained PAT or GitHub App) to satisfy an environment approval.
3. **Outbound** estimates use **SNS Publish** to the operator handset; enforce **spend limits** and region support.
4. **IAM**: OIDC from GitHub Actions to a **least-privilege role**; never long-lived keys in the repo.
5. **Cost**: **AWS Budgets** + billing alarms; fixed **EC2** or **Lambda** memory/timeouts for workers that apply approvals.

Wire contract: treat Lambda’s normalized payload as the same conceptual shape as `SmsInboundSimulationRequest` (from, body, stable external id for idempotency) and map into your approval store interface in application code **outside** Nexo.API if you want strict network boundaries.

## GitHub approval without SMS

Use **GitHub Environments** with required reviewers on the job that assumes AWS OIDC or starts expensive resources. SMS can mirror that approval for on-call, but the environment gate is the simplest hard stop.

## Smoke script

From repo root against a running API:

```bash
NEXO_BASE_URL=http://127.0.0.1:8080 ./scripts/middleware-ingress-smoke.sh
```

Set `RUN_SMS_SMOKE=1` only when `EnableSmsSimulationIngress` is true on the server.

## Tests

Integration coverage lives in `Nexo.Tests.Infrastructure` → `MiddlewareIngressIntegrationTests` (net8.0 `WebApplicationFactory` host).

From repo root, full solution tests (matches `make test`):

```bash
NEXO_ALLOW_MOCK=1 dotnet test Nexo.sln --blame-hang-timeout 120s --blame-hang-dump-type none
```

`NEXO_ALLOW_MOCK=1` is required for mock-provider integration tests on net9.0; `make test` sets this automatically.
