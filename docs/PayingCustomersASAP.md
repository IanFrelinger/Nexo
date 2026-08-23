# Paying customers as fast as possible (playbook)

**Goal:** cash and learning in **weeks**, not a perfect product fleet. This favors **Hypothesis B** from [`ICPResearchMemo.md`](./ICPResearchMemo.md) when that file exists on your branch: **security-minded B2B SaaS / software vendors** (150–2,000 engineers) who already feel pain from **shadow AI**, **customer security questionnaires**, or **“every team wired ChatGPT differently.”**

If that segment is too narrow for your network, the same playbook works for **mid-market platform teams** (Hypothesis C)—but the **contract shape** stays identical.

---

## 1. What you sell in the next 14 days (pick one)

### Offer A — **Fixed paid pilot** (fastest to signature)

| Element | Recommendation |
|---------|------------------|
| **Price** | **$5,000–$15,000** flat (card or wire), not “usage later” |
| **Duration** | **2–4 weeks**, fixed calendar end |
| **Scope** | One workflow only (e.g. internal **audited copilot task** or **changelog assistant** in **their** VPC or a dedicated single-tenant env **they** pay for) |
| **Deliverables** | Deployed instance, **written success criteria** met, **30-day hypercare** optional add-on |
| **Out of scope** | Custom model training, unlimited integrations, mesh federation |

**Why this wins fast:** procurement can buy a **SOW/pilot** from a small vendor quickly; you are not asking them to bet a year on a roadmap.

### Offer B — **Annual prepay, narrow SKU** (if they refuse “pilot”)

| Element | Recommendation |
|---------|------------------|
| **SKU** | **Ashlar Private — Team** annual prepay at a **founder discount** (example band: **$3k–$12k ARR** for first 3 logos if you need logos more than margin) |
| **Includes** | Email support, upgrade path, license for N seats, documented limits |
| **They bring** | Their AWS/GCP, their Postgres, **BYOK** for models |

**Why this wins:** one invoice, no metering debate on day one.

**Do not lead with** multi-tenant Cloud at scale until isolation + billing are boringly reliable—too easy to stall on security review.

---

## 2. Who to call first (highest close rate)

Order your outreach list:

1. **People who already trust you** (ex-colleagues, GitHub followers, Discord/Slack communities you are in).  
2. **Heads of platform / principal engineers** at **B2B SaaS** companies (even 80–300 eng) where you can name **one internal pain** (“security review for AI features,” “no standard for internal agents”).  
3. **Consultancies** who need a **repeatable private AI stack** for clients—sell **Partner Pilot** same price as Offer A but scoped to **one client** they bring.

**Titles to DM:** Director/VP Engineering, Principal Platform Engineer, Security Architect (pair with an eng champion).

---

## 3. The 14-day execution checklist

| Day | Action |
|-----|--------|
| 1 | Write **one-page pilot SOW**: scope, success metric, price, payment terms, IP, confidentiality, limitation of liability (get a template; fill blanks—lawyer review if you can afford 1 hour). |
| 2 | Build **one landing section** or PDF: problem → pilot offer → “what you get in 2 weeks” → FAQ (data residency, BYOK, what you log). |
| 3–5 | **20 DMs or emails** to warm targets; ask for **15-min call** only. |
| 6–10 | Run **8–12 calls**; on call #2 with same account, send **pilot PDF + Stripe Payment Link** or invoice. |
| 11–14 | **Close 1 pilot** or iterate objection (usually security—address with architecture one-pager, not more features). |

**Success metric for this sprint:** **signed SOW + money in bank** for one pilot, not “great conversations.”

---

## 4. What you must have minimally (do not overshoot)

- **Repeatable install** for the pilot topology (compose or Helm + pinned images).  
- **One “golden path” demo** you can run live: submit task → output + **audit trail** visible.  
- **BYOK** path documented (they do not want your OpenAI bill).  
- **Single calendar booking link** + **template invoice** or Stripe link.

Everything else (SOC2, full Cloud multi-tenancy, marketplace) is **explicitly later**.

---

## 5. Pricing psychology for “ASAP”

- **Prefer flat pilot** over complex seat math for the first checks.  
- If they want a discount, trade for **case study rights**, **logo**, or **reference calls**—not endless scope.  
- After pilot success, **convert to annual** at a defined list price (write the conversion number in the pilot SOW).

---

## 6. If nothing closes in 14 days

| Likely blocker | Next move |
|----------------|-----------|
| “We need SOC2 / pen test” | Narrow pilot to **non-production data** + **VPC-only** + written data handling; or sell **to a smaller company** first. |
| “Legal is slow” | Lower ticket pilot **$2.5k** with **prepaid card** path; shorten legal surface. |
| “We can build this” | Change ICP to teams **under delivery pressure** (post-funding shipping crunch) where build-vs-buy is losing. |

---

## 7. Related docs (on feature branches if not yet merged)

- [`ICPResearchMemo.md`](./ICPResearchMemo.md) — why B/C are faster than regulated enterprise for first dollars  
- [`MonetizationProductDesign.md`](./MonetizationProductDesign.md) — tier anchors after you have logos  
- [`ProductFleetImplementationRoadmap.md`](./ProductFleetImplementationRoadmap.md) — what to build in order

---

**Bottom line:** optimize for **one paid pilot SOW** with a **fixed price** and **fixed scope** to **B2B SaaS / platform teams** you can reach in **two weeks**. Everything else is sequencing after the first receipt.
