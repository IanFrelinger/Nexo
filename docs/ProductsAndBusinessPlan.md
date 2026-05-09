# Products and business plan (runtime-first)

This document assumes **one underlying Nexo runtime** (composable AI workflows, trust boundaries, audit trail, optional federation, self-extending capabilities). **Products** are how that runtime is **packaged, deployed, sold, and supported**—not separate engines. Tiered pricing detail lives in [`MonetizationProductDesign.md`](./MonetizationProductDesign.md); here we define **what you sell**, **who buys it**, and **how the business operates around it**.

---

## 1. Thesis

**Problem:** Teams want AI-assisted work (agents, copilots, automation) without losing **control, auditability, and predictable standards**—especially when data or policy cannot follow generic SaaS defaults.

**Solution:** A **runtime** that enforces structure and provenance wherever it runs (your cloud, their cloud, or air-gapped), with a path from **individual speed** to **enterprise governance**.

**Business implication:** Revenue comes from **software + operational responsibility + scale**, not from reselling commodity tokens unless you deliberately add that as a metered add-on.

---

## 2. The core asset (what you build once)

| Layer | What it is | Branching lever |
|-------|------------|-----------------|
| **Runtime** | Pipeline execution, capability contracts, policy, adaptation loop, audit sinks | Same binaries; config and enabled modules differ |
| **Control plane** | Portal, APIs, identity hooks, billing entitlements (where applicable) | Cloud-hosted vs customer-hosted vs minimal |
| **Distribution** | Containers, GHCR images, CLI, compose stacks | Per-SKU compose profiles and docs |
| **Trust surface** | SSO, export formats, retention, subprocessors list | Enterprise and regulated SKUs |

**Rule:** New “products” should be **deployment + packaging + GTM**, not forks of core execution semantics unless a segment truly needs a incompatible codebase (avoid until proven).

---

## 3. Product lines (branches from the runtime)

Below are **five** coherent SKUs. You can launch with **two** (Cloud + Self-host) and add the rest as the same runtime matures.

### Product A — **Nexo Cloud** (hosted multi-tenant)

| Dimension | Choice |
|-----------|--------|
| **Buyer** | Individual devs, startups, small product teams |
| **Job to be done** | “Get auditable agent/copilot workflows running **today** without running my own infra.” |
| **Delivery** | Your AWS (or equivalent); shared tenancy with strong isolation; Stripe self-serve |
| **Revenue** | Seat subscription + optional usage (jobs, storage) + add-ons; **BYOK default** for LLM to limit COGS |
| **You invest in** | Onboarding UX, abuse prevention, metering, status page, lightweight support |
| **Runtime branch** | Full portal + cloud control plane; federation optional/off |

### Product B — **Nexo Private** (customer VPC / self-hosted license)

| Dimension | Choice |
|-----------|--------|
| **Buyer** | Same as Cloud but security- or policy-sensitive; mid-market engineering orgs |
| **Job to be done** | “Same capabilities as Cloud, but **data and keys never live on your balance sheet**.” |
| **Delivery** | Their Kubernetes/VMs; license key or private registry; they pay cloud + LLM |
| **Revenue** | Per-seat or per-environment **annual** subscription; optional support tiers |
| **You invest in** | Installers, upgrade path, air-gap docs, license telemetry (counts only, configurable off) |
| **Runtime branch** | Full runtime + portal on their infra; billing may be external (invoice) |

### Product C — **Nexo Enterprise** (contract + SLA + PS)

| Dimension | Choice |
|-----------|--------|
| **Buyer** | Regulated industries, large eng orgs, anyone with procurement and InfoSec gates |
| **Job to be done** | “Production-grade **governance**, integrations, SLAs, and someone accountable when things break.” |
| **Delivery** | Dedicated VPC, their VPC, or air-gapped; named support; SOWs for custom boundaries |
| **Revenue** | **ARR** ($35k–$400k+ bands depending on scope), professional services, true-ups |
| **You invest in** | Security questionnaire pack, architecture reviews, CSM/part-time SRE on largest deals |
| **Runtime branch** | Same as B with **hard isolation** profiles, SSO, audit export guarantees, optional federation for mesh |

### Product D — **Nexo Automation** (API-first / headless for builders)

| Dimension | Choice |
|-----------|--------|
| **Buyer** | Platform teams, internal tooling groups, vendors embedding workflow in their product |
| **Job to be done** | “**HTTP APIs and webhooks** for audited tasks—portal optional or white-labeled.” |
| **Delivery** | Container image + API keys; Cloud or Private pricing applies |
| **Revenue** | Bundled into A/B/C or **usage-based API** tier (environments + monthly job bundles) |
| **You invest in** | API versioning, SDKs, rate limits, partner docs |
| **Runtime branch** | Thin UI; same execution and audit; marketing as “headless Nexo” |

### Product E — **Nexo Mesh** (federated capability network) *[optional second act]*

| Dimension | Choice |
|-----------|--------|
| **Buyer** | Distributed orgs, partners sharing **sanitized** capability across trust tiers |
| **Job to be done** | “Route work to the right host/model under **explicit trust policy**.” |
| **Delivery** | Licensed feature on top of B/C; heavier sales cycle |
| **Revenue** | **Premium** on Enterprise or separate “mesh connector” line item |
| **You invest in** | Federation hardening, peer onboarding, operational playbooks |
| **Runtime branch** | Federation enabled; same core; different docs and SRE expectations |

**Launch order (recommended):** **A + B** first (dual motion), **C** as soon as you have one referenceable enterprise deploy, **D** when API demand is clear, **E** when two or more customers ask for cross-site routing with money attached.

---

## 4. Business model (how money flows)

| Stream | Products | Notes |
|--------|----------|--------|
| **Recurring software** | A, B, C, (D) | Primary engine of value |
| **Usage overage** | A, (D) | Jobs, storage, concurrency—only where you host or meter |
| **Professional services** | C (and large B) | Deploy, policy tuning, integrations—**explicit SOW**, caps hours |
| **Training / enablement** | B, C | Fixed workshops; protects support margins |
| **Marketplace / rev-share** | Later | Optional; do not anchor the plan on it in year one |

**Pricing anchors** are specified in [`MonetizationProductDesign.md`](./MonetizationProductDesign.md) (Cloud tiers, Team self-host, Enterprise ARR bands).

---

## 5. Business plan (concise)

### 5.1 Vision (3-year direction)

Become the **default runtime** for teams that need **structured, auditable AI workflows** on **infrastructure they choose**—with a **hosted on-ramp** so evaluation and small teams do not require a six-month security project.

### 5.2 Positioning (one sentence)

**“Nexo is the trust-first AI workflow runtime: run it in our cloud for speed, or in yours for control—the same engine, policies, and audit trail.”**

### 5.3 Year-one objectives (outcomes, not dates)

1. **Reference customers:** 3–5 paying organizations across **at least two** of {Cloud, Private, Enterprise}.  
2. **Repeatable deploy:** documented path from zero to **first audited production job** in each supported mode.  
3. **Unit economics clarity:** measured **COGS per hosted tenant** and **support hours per $1k ARR** so pricing can be tuned.  
4. **Security narrative:** architecture one-pager, data flow diagram, subprocessors (if Cloud), incident response outline.  
5. **Single billing story:** Stripe for A; invoices for B/C; entitlements table shared across products.

### 5.4 Go-to-market

| Motion | Product | Channel |
|--------|---------|---------|
| **PLG / self-serve** | A | Product-led signup, docs, templates, community |
| **Bottom-up + land** | A → C | Team starts on Cloud; procurement upgrades to Private/Enterprise |
| **Top-down** | C, B | Outbound to security-conscious eng leaders; design partner pilots |
| **Partner** | B, C | MSPs and consultancies implement Nexo Private for clients (rev-share or referral later) |

**ICP (primary):** treat this as a **hypothesis** until validated; see [`ICPResearchMemo.md`](./ICPResearchMemo.md) for ranked segments (regulated internal AI platform, security-first SaaS vendors, mid-market platform engineering) and a 30-day discovery plan. The previous one-line summary was: software engineering organizations (roughly 50–2000 engineers) in regulated or security-sensitive industries **or** vendors building **customer-facing** AI features who need an internal “compliance-ready” execution layer.

### 5.5 Operating model

- **Engineering:** one core team owns runtime + portal; “product” differences are **config, entitlements, and release channels**.  
- **Support:** tiered (docs → email → named) mapped to Cloud vs Enterprise in [`MonetizationProductDesign.md`](./MonetizationProductDesign.md).  
- **Success:** activation = **time-to-first audited job**; expansion = seats, environments, jobs, mesh features.

### 5.6 Costs (planning buckets)

| Bucket | Drivers |
|--------|---------|
| **Hosted COGS** | Compute, DB, S3, NAT, logs, egress (Product A) |
| **LLM COGS** | Only if you fund tokens; **minimize** via BYOK in v1 |
| **People** | Engineering first; fractional GTM/legal/finance until ARR supports hires |
| **GTM** | Content, events, pilot travel—keep lean until repeatability |

Use [`commercial-workbook/`](./commercial-workbook/) and [`scripts/saas_cost_model.py`](../scripts/saas_cost_model.py) to stress-test assumptions as tenant count and jobs scale.

### 5.7 Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Positioning split (cloud vs sovereignty) | Single narrative: **opt-in hosted**; serious default remains **customer control** |
| Margin collapse on usage | BYOK, caps, overage pricing, defer “unlimited” |
| Enterprise sales without artifacts | Security pack + pilots before scaling outbound |
| Scope creep on services | Fixed SOWs, packaged “production readiness” |

### 5.8 What you explicitly defer

- Owning foundation-model training or heavy GPU hosting until hosted path is metered and profitable.  
- Large marketplace ecosystems until you have density of third-party capability authors.  
- Geographic expansion before one region is referenceable and supportable.

---

## 6. How this connects to the repo today

Existing capabilities (portal, copilot task API, changelog assistant, activity feed, strict mode, compose/GHCR) map cleanly to **Products A–D**. Federation features align with **Product E** when you choose to monetize them as a premium.

---

## 7. Document map

| Doc | Role |
|-----|------|
| **This file** | Product lines + business plan + sequencing |
| [`ProductFleetImplementationRoadmap.md`](./ProductFleetImplementationRoadmap.md) | Phased engineering and GTM steps to ship each SKU |
| [`MarketingAndDeploymentPlaybook.md`](./MarketingAndDeploymentPlaybook.md) | How to market each SKU and how to deploy Cloud / Private / Enterprise / Automation / Mesh |
| [`ICPResearchMemo.md`](./ICPResearchMemo.md) | Evidence-based ICP hypotheses, anti-ICPs, and a 30-day validation plan |
| [`MonetizationProductDesign.md`](./MonetizationProductDesign.md) | Tiers, prices, entitlements, billing mechanics |
| [`commercial-workbook/`](./commercial-workbook/) | Financial templates and KPI skeleton |

Treat all numbers as **hypotheses** until validated with paying customers and real COGS measurements.
