# Marketing and deployment playbook

How to **market** and **deploy** the product fleet (Nexo Cloud, Private, Enterprise, Automation, Mesh) described in [`ProductsAndBusinessPlan.md`](./ProductsAndBusinessPlan.md). This is operational guidance—adjust for your actual cloud provider, compliance posture, and ICP.

---

## Part A — Marketing

### A.1 Core message (use everywhere)

**Headline direction:** *Auditable AI workflows on infrastructure you trust—not a black box in someone else’s account.*

**Supporting pillars (rotate by audience):**

| Pillar | One line | Best for |
|--------|----------|----------|
| **Sovereignty** | Data and keys stay where you put them; cloud is optional. | Security, regulated, enterprise |
| **Traceability** | Decisions, routing, and outputs are logged for review. | Platform, compliance, eng leadership |
| **Standards** | Pipelines and promoted capabilities meet enforced quality bars. | Architects, internal platform teams |
| **Speed** | Hosted tier gets a team to first audited job without owning infra. | Startups, small teams, evaluators |

Avoid leading with “we have agents.” Lead with **control + proof**.

---

### A.2 Positioning by product (what you say on the site)

| Product | Primary promise | Proof you show |
|---------|-----------------|----------------|
| **Nexo Cloud** | “Production-shaped workflows in minutes.” | Short video: signup → BYOK → first task → audit line |
| **Nexo Private** | “Same engine in your VPC or data center.” | Reference architecture PDF, air-gap note, upgrade path |
| **Nexo Enterprise** | “Procurement-ready governance and support.” | SSO, audit export, SLA, security questionnaire excerpt |
| **Nexo Automation** | “Headless runtime for your platform.” | OpenAPI snippet, webhook signing, rate-limit table |
| **Nexo Mesh** | “Route work across trust boundaries on policy.” | Two-node diagram, failure-mode summary (when live) |

---

### A.3 Audiences and channels

| Audience | Where they look | What you ship first |
|----------|------------------|---------------------|
| **Security / InfoSec** | Architecture reviews, peer intros | Data-flow one-pager, subprocessors (Cloud), pen test plan |
| **Platform / internal tooling** | GitHub, HN, eng blogs | Compose/Helm quickstart, API docs, “run beside CI” story |
| **Engineering leadership** | Case studies, conference talks | Before/after: incident audit, change control, cost of DIY |
| **Regulated vertical** | Vertical events, consultants | Language: retention, export, residency; avoid hype |

**Channels (practical stack):**

1. **Website:** three paths above the fold—*Try Cloud* · *Deploy Private* · *Talk to sales*.  
2. **Docs as marketing:** “Time to first audited job” is your hero metric; every tutorial ends in an audit trail screenshot or JSON export.  
3. **Outbound (early):** short list of teams that already air-gap or self-host AI glue; offer **scoped pilot** (see monetization doc).  
4. **Community:** OSS users and issue triage feed **Private** upgrades; Cloud is for people who will not run Docker on day one.  
5. **Partners:** consultancies and MSPs implement **Private**; you certify **reference deployments** and share margin later if useful.

---

### A.4 Funnel and assets (minimum viable)

| Stage | Asset |
|-------|--------|
| **Aware** | Landing pages per motion (Cloud vs Private vs Enterprise) |
| **Consider** | Comparison: “Nexo vs DIY agents + Lang* glue” (honest scope) |
| **Try** | Cloud free/trial; Private “eval license” with clear limits |
| **Buy** | Stripe checkout (Cloud); order form + invoice (Private/Enterprise) |
| **Expand** | In-product upgrade nudges: SSO, export, environments, API |

**Content that converts infra buyers:** architecture diagrams, threat-model summary, “what leaves the box” list, upgrade matrix, support SLAs.

---

### A.5 What not to do early

- Competing on **cheapest tokens** or “unlimited AI.”  
- One generic homepage that tries to sound like ChatGPT **and** like Splunk.  
- Big conference spend before **three reference stories** exist.

---

## Part B — Deployment

### B.1 Principles

1. **Same artifacts everywhere:** versioned container images (e.g. GHCR), pinned digests in customer-facing docs.  
2. **Profiles, not forks:** `cloud`, `private`, `enterprise-dedicated` are **config + topology**, not different codebases.  
3. **Upgrade is part of the product:** migration notes, backup, rollback, health checks.  
4. **Secrets never in git:** cloud secrets manager or customer vault; BYOK keys in tenant-scoped secret stores.

---

### B.2 Nexo Cloud (hosted multi-tenant)

| Layer | Typical pattern |
|-------|-----------------|
| **Network** | Dedicated VPC; public ingress only to API/portal; private subnets for app and DB |
| **Compute** | ECS Fargate or EKS; autoscaling on queue depth or CPU; separate worker pool for jobs |
| **Data** | RDS Postgres (Multi-AZ for prod); per-tenant schema or DB depending on isolation bar |
| **Objects** | S3 per environment; lifecycle rules for retention tier |
| **Ingress** | ALB + TLS; WAF optional; CloudFront for static if split |
| **Identity** | Managed auth + future SSO; session storage hardened |
| **Observability** | Central logs/metrics; **cost and retention caps** on log volume |
| **Release** | Blue/green or rolling per service; feature flags for entitlements |

**Deployment flow (your pipeline):** build → scan → push image → migrate DB → deploy control plane → deploy workers → smoke test tenant fixture → flip traffic.

**Customer “deployment”:** none—they sign up; you provision tenant and optional BYOK screen.

---

### B.3 Nexo Private (customer VPC / self-hosted)

| Layer | Typical pattern |
|-------|-----------------|
| **Package** | Helm chart **or** compose bundle with pinned images + `.env.example` |
| **Customer network** | Their VPC; no inbound from internet if air-gapped; optional bastion |
| **Data** | Customer-managed Postgres (RDS or self-run); backup is their runbook + your checklist |
| **Secrets** | Their KMS/vault; BYOK for LLM; license key or activation endpoint |
| **Upgrades** | Documented minor/major path; `nexo` CLI or image bump + `helm upgrade` |
| **Telemetry** | Optional usage counts for license compliance; **off** for strict air-gap |

**Deployment flow (customer):** prerequisites check → pull images / helm repo → configure values (URL, DB, retention) → install → run **post-install verification** job → register license.

**You ship:** signed charts or image pull secrets, release notes, CVE advisory channel.

---

### B.4 Nexo Enterprise (dedicated or hardened shared)

Two acceptable patterns—pick one and price for it:

| Pattern | When | Deploy notes |
|---------|------|--------------|
| **Dedicated VPC** (yours or theirs) | Strong isolation or regulatory ask | Same as Cloud but **one tenant per cluster** or per namespace with hard boundaries |
| **Hardened multi-tenant** | Cost-sensitive | Stronger isolation testing + contractual limits; higher engineering risk |

Add: **SSO** via their IdP, **audit export** to their bucket (S3/GCS) on schedule, **SLA** monitoring with external health checks.

**Deployment flow:** sales handoff → architecture worksheet → Terraform/Helm **parameterized** from worksheet → joint go-live window → hypercare week.

---

### B.5 Nexo Automation (API-first)

| Layer | Pattern |
|-------|---------|
| **Runtime** | Same services as Cloud/Private; optionally **disable portal** or run minimal UI |
| **Edge** | API gateway (Kong/AWS API GW) for rate limits, API keys, request validation |
| **Docs** | OpenAPI published; webhook signing secret per tenant |
| **Deploy** | Same topology as parent SKU; extra **WAF rules** if public internet |

**Customer deploy:** they call your Cloud URL or install Private and use **only** API base URL from their network.

---

### B.6 Nexo Mesh (federation)

| Layer | Pattern |
|-------|---------|
| **Topology** | At least two **peers** with mutual auth; no shared DB across trust zones unless explicitly designed |
| **Network** | mTLS between peers; egress allowlists; certificate rotation automated |
| **Rollout** | **Never** enable mesh without runbooks; upgrade order peer-by-peer |

Treat mesh as a **feature flag + license** on top of B or C.

---

### B.7 Cross-product checklist (every release)

- [ ] Image digests + CVE notes  
- [ ] Migration scripts and rollback  
- [ ] Config reference for each profile (`cloud` / `private` / `dedicated`)  
- [ ] Smoke test: signup or install → one job → audit row present  
- [ ] Breaking changes in API/version bump  

---

## Part C — How marketing and deployment connect

| Customer sees… | You must have deployed… |
|----------------|---------------------------|
| “Try Cloud” | Multi-tenant stack + billing + abuse limits + status page |
| “Deploy Private” | Helm/compose, license flow, security PDF, upgrade doc |
| “Enterprise” | SSO path, export to customer bucket, SLA monitors, PS SOW |
| “Automation” | Public API + keys + webhooks behind gateway |
| “Mesh” | Two-peer lab + production checklist |

Keep **one docs site** with **tabs by deployment mode** so marketing URLs do not multiply into conflicting stories.

---

## Related documents

| Document | Role |
|----------|------|
| [`ProductsAndBusinessPlan.md`](./ProductsAndBusinessPlan.md) | SKUs and business model |
| [`MonetizationProductDesign.md`](./MonetizationProductDesign.md) | Tiers and pricing |
| [`ProductFleetImplementationRoadmap.md`](./ProductFleetImplementationRoadmap.md) | Build order and exit criteria |
| Repo **README** / **Deploy** sections | Concrete Nexo container paths today |
