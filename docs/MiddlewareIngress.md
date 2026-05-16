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
| AWS SNS webhook | `POST /api/ingress/sms/sns` | Optional signed SNS → same approval store (`Nexo.Ingress.AwsSns` helpers). |

Configuration: `Nexo:MiddlewareIngress` (`EnableWebSocketIngress`, `EnableSmsSimulationIngress`, `SmsSimulationAllowedAppIds`, `SmsIngressApprovalStore`, `EnableAwsSnsSmsWebhook`, `AwsSnsAllowedTopicArnPrefixes`, `AwsSnsAllowedAppIds`, `AwsSnsAutoConfirmSubscription`, `AwsSnsSkipSignatureVerification`, `AwsSnsSigningCertificateRevocationMode`, `IngressSmsPostRateLimitPermitLimit`, `IngressSmsPostRateLimitWindowSeconds`, `DisabledCapabilities`, `TenantCapabilityAllowlists`) and `Nexo:SmsIngressDynamoDb` (`TableName` when using DynamoDB).

## `Nexo.Ingress.AwsSns` (library)

Small helpers used by Nexo.API (not AWS SDK–heavy):

- **`SnsCanonicalStringBuilder`** — builds the SNS string-to-sign for `Notification` and subscription handshake types.
- **`SnsRsaSignatureVerifier`** — downloads the PEM from `SigningCertURL` (HTTPS + `*.amazonaws.com` host allowlist), validates an **Amazon-anchored X.509 chain** (system trust first, then **Amazon Root CA 1** embedded as a custom root fallback; revocation mode from `AwsSnsSigningCertificateRevocationMode`), and verifies `SignatureVersion` 1 (SHA-1) or 2 (SHA-256).
- **`SnsSmsMessageExtractor`** — unwraps plain text or JSON `Message` bodies (e.g. `originationNumber` + `messageBody`).

`AwsSnsSkipSignatureVerification` is honored **only** when `IHostEnvironment.EnvironmentName` is `Testing` (integration tests). Outside `Testing`, if `EnableAwsSnsSmsWebhook` is **true**, **`AwsSnsAllowedTopicArnPrefixes` must be non-empty** or the host fails `ValidateOnStart` (fail-closed against open-ended topic acceptance).

Optional **`AwsSnsAllowedAppIds`** enforces `X-Nexo-App-Id` on the SNS route (parity with SMS simulation app allowlists).

## Approval storage (`ISmsIngressApprovalStore`)

- **`Memory`** (default) — process-local `ConcurrentDictionary` for labs and CI.
- **`DynamoDb`** — set `SmsIngressApprovalStore` to `DynamoDb` and `Nexo:SmsIngressDynamoDb:TableName`. Table uses string keys **`pk`** (constant `NexoSmsIngress`) and **`sk`** (idempotency key from `SmsIngressExternalIds`). Grant the runtime identity `dynamodb:PutItem` and `dynamodb:GetItem`.
- **`UnsupportedSmsIngressApprovalStore`** — registered via `TryAddSingleton` in `AddNexo()` when no host-specific store is configured (e.g. CLI). Nexo.API replaces this with Memory or DynamoDB.

**Optional end-to-end check (DynamoDB Local in Docker):** `Nexo.Tests.Infrastructure` includes `DynamoDbSmsIngressDockerTests` (Testcontainers, trait `DockerOptional`). Set **`NEXO_RUN_DYNAMODB_CONTAINER=1`** and run tests with Docker available to exercise `DynamoDbSmsIngressApprovalStore` against a real DynamoDB Local process (no AWS account). Default CI runs skip this (no env set).

The interface lives in **`Nexo.Contracts`** so Lambda workers can share the contract without referencing `Nexo.API`.

## MediatR command

`RecordSmsYesApprovalCommand` / `RecordSmsYesApprovalHandler` (in `Nexo.API`) record approvals through **MediatR** so pipeline behaviors (ingress logging, validation) apply consistently. `POST /api/ingress/sms/simulate` and SNS `Notification` handling both dispatch this command.

## Rate limiting and revocation

- **`IngressSmsPostRateLimitPermitLimit`** / **`IngressSmsPostRateLimitWindowSeconds`** — per-IP fixed window on both SMS POST routes. When the permit limit is **0**, an effectively unlimited partition is used (no practical throttling).
- **`AwsSnsSigningCertificateRevocationMode`** — `NoCheck` (default), `Online`, or `Offline` for SNS signing certificate chain builds (`X509Chain.ChainPolicy.RevocationMode`). `Online` can add latency or fail closed if OCSP/CRL endpoints are unreachable.

## Terraform and WAF

`infra/terraform/nexo-sms-ingress/` provisions the DynamoDB table and an **optional** regional WAFv2 Web ACL with a rate-based rule (`create_waf` + `alb_arn`). Tune limits for your traffic.

## Step Functions sample

`samples/approval-workflow/` contains a minimal ASL file and README for a **callback-token** style gate that could invoke GitHub from Lambda (replace placeholders).

## Sample Lambda forwarder

See **`samples/aws-sns-nexo-lambda/`** — minimal Node.js Lambda that reshapes SNS subscription events into the HTTP JSON document Nexo verifies, then `POST`s to `NEXO_SMS_URL`.

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

For HTTP(S) subscriptions directly to Nexo.API, **`POST /api/ingress/sms/sns`** reuses **`ISmsIngressApprovalStore`** after signature verification. Prefer a dedicated Lambda (see `samples/aws-sns-nexo-lambda/`) or private networking in front of Nexo when the API is not meant to be Internet-exposed.

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

`NEXO_ALLOW_MOCK=1` is required for mock-provider integration tests; `make test` sets this automatically.
