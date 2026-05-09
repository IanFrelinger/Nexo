# ICP research memo — who should Nexo sell to first?

**Purpose:** Turn “we don’t know our ICP” into **testable hypotheses** grounded in **how buyers are pressured** (governance, sovereignty, platform ownership) and **how Nexo is positioned** (private/traceable workflows, air-gap capable, optional cloud targets).

**Method:** review of **public regulatory framing** and **common enterprise buying patterns** for internal AI and platform tooling—not proprietary market sizing. Validate with **10–15 customer discovery calls** before locking marketing spend.

---

## 1. What the market is optimizing for (2024–2027 public signals)

### 1.1 Regulation and “prove it” pressure

The **EU AI Act** is an explicit, risk-based framework: higher-risk uses face **stronger obligations**, including **traceability/logging**, **documentation**, **human oversight**, and **cybersecurity/robustness** expectations for systems placed on the EU market or affecting people in the EU. Official overview: [European Commission — AI Act / regulatory framework for AI](https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai).

**Implication for ICP:** organizations that **deploy AI inside regulated workflows** (or EU-facing products) need **runtime evidence**, not a policy PDF. That aligns with Nexo’s **audit trail + policy constraints** story.

### 1.2 “Internal AI platform” buyers already exist

Enterprises increasingly centralize developer experience and internal services under **platform engineering** / **internal developer platforms**. Buyer guides and vendor ecosystems treat **security/compliance** as a first-class evaluation dimension alongside integrations—see for example vendor-neutral buyer education such as [OpsLevel — Internal Developer Portal buyer’s guide](https://www.opslevel.com/resources/internal-developer-portal-the-buyers-guide) and aggregator commentary on **AI-native / agentic** internal portals (e.g. [CodeBrewTools — AI-native internal developer portals roundup](https://codebrewtools.com/blogs/ai-native-internal-developer-portals-2026)).

**Implication for ICP:** a strong wedge is **platform teams** standardizing **agent/copilot workflows** with **guardrails and auditability**, especially when “every team wires ChatGPT differently” becomes unacceptable.

### 1.3 Sovereignty and deployment location remain differentiators

Nexo’s README emphasizes **customer-controlled infrastructure**, **optional** cloud model providers, and **air-gapped** deployment. That is not the default posture of consumer-style AI SaaS.

**Implication for ICP:** prioritize buyers where **data residency, vendor egress, or offline operation** is non-negotiable (regulated, national-critical, defense-adjacent, certain EU enterprises)—*if* you can support the sales cycle and deployment expectations.

---

## 2. Recommended ICP candidates (ranked as hypotheses)

### Hypothesis A — **“Regulated internal AI platform” (primary if you want ARR and will do enterprise hygiene)**

| Field | Profile |
|-------|---------|
| **Company** | 500–15,000 employees; EU and/or US; financial services, insurance, health systems, pharma, public sector suppliers |
| **Trigger** | AI governance program, EU AI Act readiness, vendor risk review, internal ban on “shadow AI” |
| **Champion** | **Head of platform engineering**, **Director of IT / infrastructure**, sometimes **CISO office** (risk), **Legal/Privacy** (DPA) |
| **Economic buyer** | CIO/CTO/CISO + procurement |
| **Why Nexo fits** | Auditability, policy constraints, deployment in **their** environment, air-gap story |
| **Risk** | Long sales cycles, security questionnaires, proof of isolation |

**Validation questions:** “What evidence do you need for an internal AI workflow in production?” “Who blocks a new AI vendor today?” “What is your EU AI Act / risk-tier stance for this use case?”

---

### Hypothesis B — **“Security-first SaaS / software vendors” (primary if you want faster sales and can sell VPC/BYOC)**

| Field | Profile |
|-------|---------|
| **Company** | B2B SaaS vendors (50–1,000 eng) shipping features assisted by agents; customer data in tenant isolation matters |
| **Trigger** | Customer security review asks how AI features are governed; engineering wants internal agent standardization |
| **Champion** | **VP Eng**, **Security architect**, **Principal engineer** for platform |
| **Economic buyer** | VP Eng / CTO |
| **Why Nexo fits** | “Same engine in our VPC” narrative; API-first path for productized AI workflows |
| **Risk** | They may prefer hyperscaler marketplace models unless you are clearly cheaper/faster to **prove control** |

**Validation questions:** “Do you ship AI features to customers today?” “What does your pentest / customer questionnaire ask about generative AI?”

---

### Hypothesis C — **“Mid-market platform engineering” (good for Cloud + Private dual motion)**

| Field | Profile |
|-------|---------|
| **Company** | 150–2,000 employees; modern cloud estate; Kubernetes culture |
| **Trigger** | Too many one-off agent demos; need internal “golden path” for copilots/automation |
| **Champion** | **Staff/Principal platform engineer**, **DevEx lead** |
| **Economic buyer** | VP Eng |
| **Why Nexo fits** | Container-first deploy, composable workflows, internal standard |
| **Risk** | Competes with “we’ll glue LangGraph + logs” unless you win on **governance + time-to-standard** |

**Validation questions:** “What is your internal ‘approved AI stack’ today?” “What would ‘done’ look like for an internal copilot platform?”

---

### Hypothesis D — **MSP / systems integrator channel (secondary ICP: sells *to* your ICP)**

| Field | Profile |
|-------|---------|
| **Company** | Regional SIs, cloud MSPs, boutique DevSecOps consultancies |
| **Trigger** | Customers asking for private AI platforms; they need repeatable deploy packages |
| **Champion** | Practice lead |
| **Economic buyer** | Partner leadership + joint customer |
| **Why Nexo fits** | Private/Enterprise deployment story maps to **services-led** GTM |
| **Risk** | Requires enablement, margin rules, and partner pipeline management |

Treat this as a **route-to-market** to A/B/C, not the first “end user” ICP unless you are explicitly building a channel-first company.

---

## 3. Anti-ICPs (who to say “no” to early)

- **Purely individual hobbyists** unless you are explicitly building prosumer PLG with abuse controls.  
- **Teams wanting cheapest raw tokens** with no governance story—you will not win on price.  
- **Buyers needing SOC2 Type II on day one** if you cannot deliver attestation yet (either narrow scope or defer).  
- **Organizations without an internal owner** for “AI platform” (sales will stall).

---

## 4. Suggested “first ICP” choice (default recommendation)

If you must pick **one** to sequence GTM:

1. **Start with Hypothesis B (B2B SaaS vendors)** if you need **shorter cycles** and can show **VPC + audit** quickly.  
2. **Start with Hypothesis A (regulated enterprise)** if you can tolerate **6–18 month** sales and want **large ARR** and defensible differentiation.  
3. **Start with Hypothesis C** if your near-term product is strongest on **developer ergonomics** and **Cloud** onboarding.

Nexo’s README strengths suggest **A or B** will feel the most “native” story; **C** is viable if messaging emphasizes **golden-path internal automation** rather than compliance depth.

---

## 5. 30-day validation plan (cheap research)

| Week | Activity | Output |
|------|----------|--------|
| 1 | Write **one-page** problem hypothesis for A, B, C | Sharable memo |
| 2 | **15 discovery calls** (5 each), same script | Ranked ICP + objections |
| 3 | Publish **one landing page** for the winner + one for runner-up | CTR + waitlist quality |
| 4 | Run **2 paid pilots** scoped to one workflow | Signed SOW + reference path |

Discovery script (minimum):

1. What internal AI workflows exist today, and where do they fail (cost, trust, audit)?  
2. Who must approve a new system (security, legal, infra)?  
3. What evidence would satisfy them (logs, export, residency)?  
4. What budget line pays (R&D tooling, security, platform)?

---

## 6. Sources (non-exhaustive)

- European Commission — **AI Act** overview and timelines: [Regulatory framework for AI](https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai)  
- Nexo positioning (sovereignty, air-gap, audit): repository [`README.md`](../README.md)  
- Internal portal / platform buyer dynamics (examples): [OpsLevel buyer guide](https://www.opslevel.com/resources/internal-developer-portal-the-buyers-guide), [CodeBrewTools — AI-native IDP roundup](https://codebrewtools.com/blogs/ai-native-internal-developer-portals-2026)

---

## 7. Next document to update once ICP is chosen

- [`ProductsAndBusinessPlan.md`](./ProductsAndBusinessPlan.md) — replace generic ICP paragraph with the **selected hypothesis** and “who is out of scope.”  
- [`MarketingAndDeploymentPlaybook.md`](./MarketingAndDeploymentPlaybook.md) — narrow **advertising** and **proof assets** to the chosen buyer.
