# Product fleet — implementation roadmap

This is the **engineering, operations, and go-to-market sequence** for turning the single Nexo runtime into the product lines described in [`ProductsAndBusinessPlan.md`](./ProductsAndBusinessPlan.md). Phases overlap; order is **risk-reduction first** (sellable Private path before multi-tenant Cloud unless you explicitly bet on PLG first).

---

## Principles

1. **One runtime, many SKUs** — differentiate by **deployment profile**, **entitlements**, **support/SLA**, and **billing**, not by forking core execution logic.  
2. **Ship revenue before perfect PLG** — a **Private** customer paying annual is often easier than anonymous Cloud scale on day one.  
3. **Tenant isolation and metering are product** — for Cloud they are as important as features.  
4. **Every phase has a “done means”** — demo, contract, or metric—not just merged code.

---

## Phase 0 — Foundation (all products depend on this)

| Step | What to implement | Done means | Status |
|------|-------------------|------------|--------|
| 0.1 | **Tenant model** end-to-end: stable `tenant_id` (or org id) on every job, audit row, artifact, and API key | Cross-tenant access tests fail in CI | **Done:** `X-Nexo-Tenant`; `ProductFleetTenantIsolationTests` + `NexoHttpTenantTests` |
| 0.2 | **Entitlements configuration** (file or DB) keyed by plan: seats, `included_jobs`, `max_concurrency`, `retention_days`, `sso_enabled`, `audit_export`, `deployment_mode` | Same binary runs with different config profiles (dev/staging/prod) | **Done:** `NexoEntitlementsOptions`; copilot hourly quota enforced |
| 0.3 | **Usage counters** (jobs submitted, jobs succeeded, tokens optional if BYOK proxy) emitted to logs or metrics table | You can answer “how many jobs per tenant last 24h?” | **Done:** `ITenantUsageStore`, `GET /api/usage/summary`, `NexoUsage` logs |
| 0.4 | **Reference deployment** documented: compose (or Helm) for “single-tenant production shape” matching what you sell as Private | New hire reproduces deploy from docs in one session | **Done:** `docker-compose.private-single-tenant.yml` + [`private-reference-deployment.md`](./product-fleet/private-reference-deployment.md) |
| 0.5 | **Observability baseline**: structured logs, health checks, redacted config export for support | On-call playbook v0.1 exists | **Done:** `GET /api/support/diagnostics`, copilot audit `TenantId`, [`on-call-playbook-v0.1.md`](./product-fleet/on-call-playbook-v0.1.md) |
| 0.6 | **Legal/commercial shell**: entity, basic ToS/Privacy for a website, DPA template if Cloud will hold customer data | Counsel-reviewed drafts (timing varies) |

**Exit:** you can run **one production-shaped tenant** with measurable usage and no ambiguous identity.

---

## Phase 1 — **Nexo Private** (Product B) — ship first if enterprise is near

Private is usually **less COGS risk** and forces **install, upgrade, and air-gap** clarity early.

| Step | What to implement | Done means | Status |
|------|-------------------|------------|--------|
| 1.1 | **Install path**: pinned images, versioned release artifacts, migration notes | Customer upgrades one minor version without data loss | **Started:** install/upgrade table in [`private-reference-deployment.md`](./product-fleet/private-reference-deployment.md) |
| 1.2 | **License or subscription check** (even v0: signed JWT license file or online activation with **air-gap fallback**) | Expired license degrades gracefully (read-only or block execution—your policy, documented) | **Started:** `NexoPrivateLicenseOptions`, `PrivateLicenseMiddleware`, sample [`sample-private-license.json`](./product-fleet/sample-private-license.json) |
| 1.3 | **Secrets**: BYOK storage for provider keys; document what never leaves host | Security one-pager accurate | **Started:** [`private-byok-security.md`](./product-fleet/private-byok-security.md) |
| 1.4 | **Backup/restore** runbook + tested restore for DB and object stores you use | RPO/RTO stated on support page | **Started:** [`private-backup-restore.md`](./product-fleet/private-backup-restore.md) (RPO 24h / RTO 4h pilot defaults) |
| 1.5 | **Private pricing + invoice flow** (Stripe Invoicing, Paddle, or manual) + **order form template** | First paying Private customer can be billed without heroics | **Started:** [`private-order-form-template.md`](./product-fleet/private-order-form-template.md) |
| 1.6 | **Support boundaries**: severity levels, response-time targets for paid Private | Published on website or contract appendix | **Started:** [`private-support-boundaries.md`](./product-fleet/private-support-boundaries.md) |

**Exit:** **first annual Private customer** or equivalent pilot revenue with a repeatable deploy story.

---

## Phase 2 — **Nexo Cloud** (Product A) — multi-tenant hosted

Only start when Phase 0 is solid; overlap Phase 1 if you have capacity.

| Step | What to implement | Done means | Status |
|------|-------------------|------------|--------|
| 2.1 | **AWS account structure**: prod/stage, IAM boundaries, secrets manager, VPC | No secrets in git; least-privilege roles | **Started:** [`cloud-aws-account-structure.md`](./product-fleet/cloud-aws-account-structure.md) |
| 2.2 | **Isolation**: per-tenant DB schema or DB, or strict row-level security + proven tests; network policies between services | Third-party or internal pen test of **tenant escape** path | **Started:** `RequireOrgMembership` + `ProductFleetOrgControlPlaneTests`; [`docker-compose.cloud-multi-tenant.yml`](../docker-compose.cloud-multi-tenant.yml) |
| 2.3 | **Control plane**: signup, org creation, invite flow, role model (admin vs member) | Non-admin cannot read other org’s jobs | **Started:** `IOrganizationStore`, `/api/orgs/*`, admin-only invites |
| 2.4 | **Stripe Billing**: products/prices for seats; optional metered usage for jobs/storage | Test subscription + upgrade + cancel in staging |
| 2.5 | **Metering pipeline**: usage events → aggregation job → Stripe usage records (or internal ledger if invoiced) | Overage bill matches product-defined job definition |
| 2.6 | **Abuse controls**: rate limits, CAPTCHA or email domain rules, plan caps | Cost cannot spike unbounded from one free account |
| 2.7 | **BYOK path in Cloud** (recommended default): key vault per tenant, rotation doc | LLM spend is not on your card by default |
| 2.8 | **Status page + incident comms template** | First incident handled without improvising customer email |
| 2.9 | **Subprocessors page** if any third party touches customer content | Matches reality |

**Exit:** **first paying Cloud customer** (even one team) with automated renewal and measured COGS per tenant.

---

## Phase 3 — **Nexo Enterprise** (Product C) — productize what Private pilots asked for

| Step | What to implement | Done means |
|------|-------------------|------------|
| 3.1 | **SSO (SAML/OIDC)** with IdP-initiated and SP-initiated flows tested against two common IdPs | Checklist item for enterprise security reviews |
| 3.2 | **Audit export**: scheduled export, SIEM-friendly format, retention policy enforcement | Customer success runs export without engineering |
| 3.3 | **Dedicated VPC / single-tenant AWS** *or* **hard isolation profile** on shared cloud—pick one strategy and document cost | Pricing model updated in [`MonetizationProductDesign.md`](./MonetizationProductDesign.md) |
| 3.4 | **SLA document** + error budgets + on-call rotation (even if founder-led) | Contractual language matches actual practice |
| 3.5 | **Security pack**: answers to common questionnaire, architecture PDF, pen test summary | Shorter enterprise sales cycle |
| 3.6 | **Professional services SOW templates** (deploy, integration, policy workshop) with hour caps | PS revenue does not consume all eng time |
| 3.7 | **True-up / annual renewal** process for seats or environments | Finance can reconcile without custom spreadsheets |

**Exit:** **second enterprise** (or expansion of first) with **ARR** and **referenceable** security story.

---

## Phase 4 — **Nexo Automation** (Product D) — API-first / headless

| Step | What to implement | Done means |
|------|-------------------|------------|
| 4.1 | **Public API surface** versioned (`/v1/...`), deprecation policy, OpenAPI spec | External team integrates without reading .NET source |
| 4.2 | **API keys** scoped by tenant + optional scoped capabilities; rotation | Compromised key revocation is instant |
| 4.3 | **Webhooks** for job completion / failure with signing secret | Downstream automation is reliable |
| 4.4 | **Rate limits and quotas** aligned with entitlements | Noisy neighbor cannot starve others on Cloud |
| 4.5 | **SDK or code samples** (curl → one language SDK is enough v1) | “Time to first API job” under an hour |
| 4.6 | **Pricing attachment**: API bundles or metered tier on same Stripe products | Sales can quote without custom code |

**Exit:** **one production integration** by a customer or partner that is not using the portal as primary UI.

---

## Phase 5 — **Nexo Mesh** (Product E) — federation as premium

| Step | What to implement | Done means |
|------|-------------------|------------|
| 5.1 | **Trust tier and routing policy** UX and enforcement tests across two nodes | Mis-routed request is impossible or fails closed |
| 5.2 | **Peer onboarding**: credential exchange, rotation, revocation | Two orgs can connect without sharing one master key |
| 5.3 | **Operational runbooks**: failure modes, split-brain, upgrade order | You can sleep during a partial outage |
| 5.4 | **Commercial**: mesh as **add-on SKU** on Enterprise (or separate line item) | SKU appears in order form and entitlements |

**Exit:** **two paying nodes** (orgs or environments) in production with mesh traffic and a support playbook.

---

## Cross-cutting workstreams (run continuously)

| Workstream | Examples |
|------------|----------|
| **Security** | Dependency scanning, secret scanning, annual pen test budget, vulnerability disclosure |
| **Compliance readiness** | SOC2 roadmap if Cloud holds sensitive data; data map for GDPR/CCPA |
| **Finance** | Revenue recognition rules for annual vs monthly; sales tax if applicable |
| **Docs** | One “choose your path” page: Cloud vs Private vs Enterprise |
| **Analytics** | Activation funnel, COGS per tenant, support tickets per $ |

---

## Sequencing options (pick based on your first buyer)

| If your first serious buyer is… | Favor this order |
|---------------------------------|------------------|
| **Self-serve teams** | Phase 0 → **2 Cloud** in parallel with thin **1 Private** (install doc only) |
| **Security / regulated / VPC-only** | Phase 0 → **1 Private** → **3 Enterprise** pieces (SSO, export) → **2 Cloud** later |
| **Platform team embedding APIs** | Phase 0 → **4 Automation** on top of **1 Private** or **2 Cloud** |

The default recommendation in [`ProductsAndBusinessPlan.md`](./ProductsAndBusinessPlan.md) remains **A + B** in market messaging; implementation can still **lead with B** if that is where revenue is.

---

## Checklist: “Are we ready to market a SKU?”

- [ ] **Deploy path** documented and tested  
- [ ] **Tenant isolation** proven for that SKU  
- [ ] **Billing or contract** path exists  
- [ ] **Support and limits** published  
- [ ] **Security story** matches reality (subprocessors, data flow)  
- [ ] **Rollback/upgrade** for the data the SKU stores  

---

## Related documents

| Document | Purpose |
|----------|---------|
| [`ProductsAndBusinessPlan.md`](./ProductsAndBusinessPlan.md) | What the fleet is and why |
| [`MonetizationProductDesign.md`](./MonetizationProductDesign.md) | Tiers, prices, entitlements |
| [`commercial-workbook/`](./commercial-workbook/) | Financial stress templates |
