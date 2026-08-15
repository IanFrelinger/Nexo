# Trust Loop Integration — Dynamic Self-Extension via Certified Hot Reload

Maps `nexo-trust-loop-spec.md` v1.0 onto the Nexo codebase as of master
`5fc48684` (Aug 12, 2026). Goal: agent clusters propose bricks; only
certified artifacts hot-reload into a running host. "vibeOS shape,
deterministic guts."

---

## 1. What already conforms (don't rebuild)

| Spec | Existing implementation | Status |
|---|---|---|
| §0 core invariant | `SelfProducedBrickCertificationPolicy` refuses brick writes on the self-extend edge without a verified cert record (Invariant A tests) | ✅ at admission edge |
| R2.2 hash binding | `CertificationTrustVerifier.Verify(record, content)` re-checks content hash + signature | ✅ |
| R2.3 fail-closed | Policy refuses on missing record, missing content, un-inferable brick id | ✅ |
| R4.6 budgets (partial) | `ToolCallingAgent.DefaultMaxIterations = 5`; recursion ceiling (Invariant D) | ⚠️ iterations only — no wall-clock, no throughput guard |
| Cross-process durability | `FileCertificationRecordStore` wired as of `f43ffcd5` (yesterday) | ✅ prerequisite for hot reload just landed |
| Hot-load primitive | `MutantAssemblyLoadContext` — collectible ALC, named for leak attribution, loads serialized via `MutantContextGate` semaphore | ✅ reusable pattern |
| HITL | Invariant C human-admission gate | ✅ keep — see §5 tiering |

## 2. Gaps vs. spec (build these)

**G1 — Certificate schema (§2.1).** `CertificationRecordData` is
mutation-testing-centric (escape rate, mutant lists). Missing:
`gates_passed` (ordered, with gate versions/config), `inputs`
(hashes of truth sources + context), `proposer` (agent identity +
params), `attempts` (effort ledger). Extend the record; keep old
fields as the payload of one gate entry ("mutation-gate") so existing
sidecars stay valid.

**G2 — Signing algorithm (R2.6 chain composition).** Records carry
**HMAC-SHA256** — symmetric. Fine inside one trust domain; composed
certificate chains across processes/machines/marketplaces need
asymmetric verification (verifier ≠ minter key). Migrate to Ed25519
sidecar signatures with a dual-write transition window. This is also
the honest version of the pitch-deck trust story.

**G3 — Deterministic context assembly (§1, R1.2–R1.5).** No formal
stage-1 component. Build `ProposalContextAssembler` in
`Nexo.BackgroundAgents`: retrieval + closure walks + canonicalization,
per-source-class char budgets, records included/truncated/provenance-
tier. Output is a hashable `ProposalContext` — its hash goes in the
certificate's `inputs`.

**G4 — Probe/fence layer (§3).** Gate failures currently return raw
tool output. Add `IDiagnosticProbe` (runs on gate failure, emits
cause + minimal facts) and `ICandidateFence` (deterministic
detect-and-fix / detect-and-reject on ingest: fence stripping, guard
form, forbidden-namespace). Fences run before any gate; catalog grows
monotonically. Constraint manifests (R3.5): one machine-checkable
manifest per brick class, injected into proposer instructions AND
enforced as pre-gate — kills the prose-drift problem.

**G5 — Progress discipline (§4.5).** Only the iteration cap exists.
Add to the agent harness: candidate hash history (oscillation
interrupt), exact-repeat read rejection, N-consecutive-non-improving
abandon-increment, wall-clock budget. Terminal states restricted to
certified-success | explained-failure-with-artifacts (R4.7).

**G6 — Run ledger (§5.2).** Append-only (action, observation-digest)
ledger per proposal session, replayed into each turn, embedded in the
final certificate or failure report, and projected into the Neo4j
provenance graph as first-class nodes. Failed candidates persist
(R5.1) — they're the fence catalog's raw material.

**G7 — Adversarial campaign (§7).** SX invariant tests cover the
policies; the spec demands a scripted-adversary campaign against the
*harness* (sandbox escapes, premature completion, anchor abuse,
oscillation, garbage output, ends-bad run). New suite:
`Nexo.Tests.BackgroundAgents/TrustLoop/AdversarialCampaignTests`.
Acceptance: zero false certificates across all scenarios.

## 3. The hot-reload host (new)

`CertifiedBrickHotSwapHost` in `Nexo.Runtime`:

1. **Verify-at-load, not just verify-at-admission.** Before loading
   any assembly/source, re-run `CertificationTrustVerifier` against
   the exact bytes being loaded (R2.2 defense in depth — the file may
   have changed since admission; hash mismatch = refuse load).
2. **Generation model.** One collectible ALC per *generation* of the
   brick set, never per brick. The `0x80131506` LoaderAllocator crash
   means overlapping collectible ALC churn is the enemy: serialize
   swap operations exactly like `MutantContextGate` does today.
   Swap = load gen N+1 → route new invocations → drain gen N →
   `Unload()` → verify collection (leak attribution via ALC name,
   same trick as `MutantAssemblyLoadContext`).
3. **Fail-closed swap.** Any verification or load failure leaves
   gen N serving. There is no "partial swap."
4. **Provenance event.** Every swap (and refused swap) is a
   provenance-graph event: cert id, content hash, generation,
   outcome.

## 4. Agent clusters (the vibeOS part)

Concurrency goes in stages 1–3, not in the swap:

- **Parallel proposers, serial certifier, serial swapper.** N agents
  propose candidates concurrently (cheap, no ALC involved). The gate
  runner certifies candidates one at a time per brick lineage
  (mutation runs already serialize for the ALC reason). The swap host
  batches certified bricks into the next generation.
- Proposers hold zero authority (R1.1): they emit candidates into a
  queue; nothing a proposer writes can reach the runtime except
  through cert + swap host.
- Cluster-level budget: per-session wall-clock and a throughput guard
  vs. session median (R4.6), so one degenerate agent can't starve
  the generation cadence.

## 5. Admission tiering (keeps Invariant C honest)

Dynamic ≠ unsupervised. Tier by blast radius:

- **Tier 0 (auto-swap):** leaf bricks with a constraint manifest, no
  policy/security surface, passing full gate chain → hot-swap without
  human gate.
- **Tier 1 (human admission):** anything touching policies,
  certification, tool whitelists, or the swap host itself → existing
  Invariant C human gate. The trust loop must never be able to
  self-modify its own gates without a human — that's the recursion
  the SX-HARDEN work exists to prevent.

## 6. Build order (each lands independently green)

1. **PR-A:** Extend `CertificationRecordData` (G1) + dual-write
   Ed25519 (G2). Pure contracts + verifier; no behavior change with
   old records.
2. **PR-B:** `CertifiedBrickHotSwapHost` with verify-at-load and the
   generation/serialization model (§3). Testable today against
   existing certified bricks — no agents needed.
3. **PR-C:** `ProposalContextAssembler` (G3) + constraint manifests
   (G4 manifests only).
4. **PR-D:** Probe/fence catalog v1 (G4) — start with the failure
   classes already in your CI history (the triage skill's clusters
   feed this directly).
   *Status: the analyzer half landed via the extension spec
   ("Analyzer Gate & Container Isolation" Part A): catalog v1
   NEXO0003–0009 with per-rule triads in `Nexo.Analyzers`, and the
   `analyzer-gate` running first in `CertificationGate` — fail-closed
   on non-compiling candidates, unresolvable brick anchors, and
   analyzer crashes (A1.4); A1.5 metadata recorded on `gates_passed`;
   A3-conformant verbatim feedback via
   `AnalyzerGateOutcome.FormatProposerFeedback`. Constraint manifests are
   now also enforced semantically at certification (A2):
   `BrickConstraintManifestAnalyzer` is constructed with the manifest
   instance carried on `CertificationRequest.ConstraintManifest`
   (NEXO0010–0012 — using allowlist, forbidden APIs, forbidden namespaces —
   symbol-resolved, so aliasing cannot dodge them). The diagnostic-probe
   half (`IDiagnosticProbe`) remains open.*
5. **PR-E:** Harness progress discipline + ledger (G5, G6).
6. **PR-F:** Adversarial campaign (G7) — gates production-readiness
   of the whole loop, per spec §7.

Rationale for order: PR-B delivers the demo-able "hot reload certified
code" capability immediately using bricks you certify by hand; the
agent-cluster layers (C–E) then feed that same host. You get the
vibeOS demo before the full autonomy loop is done.

## 7. Open questions for you

- Does `Gate` (singular, string) on today's record ever hold multiple
  values in practice? Determines whether G1 is additive or a v2 schema.
  **Resolved (PR-A):** always a single hard-coded gate type name; nothing
  reads it and no test asserts its shape. G1 landed additively —
  `SchemaVersion` (null = legacy v1) selects the signing payload, so
  existing sidecars verify byte-for-byte while v2 records sign the
  extended payload (which also closes the unsigned-`Gate` hole).
- Where should Tier 0/1 classification live — constraint manifest
  field, or path convention like `BrickAdmissionPathHelper` uses?
  **Resolved (autonomy U1):** neither — tiers classify from the
  objective's declared `TouchSet` (`ObjectiveTierClassifier`), a pure
  function of blast radius against the static `TrustKernel`
  enumeration. Manifests stay behavioral constraints; paths stay
  admission plumbing.
- Throughput guard baseline: session median (spec) or fixed floor?
  Median needs ≥3 completed tasks before it's meaningful.
- Should the analyzer fence also run in
  `CompositionCertificationGate`'s chain ("every brick gate chain")?
  **Resolved (backlog V6): no.** A `CompositionCertificationRequest`
  carries no candidate source — compositions are graph specs over
  already-certified bricks, so the fence's subject does not exist at
  that level and requiring it would be enforcement theater. The
  guarantee compositions need is that every constituent IS
  brick-certified (where the fence already ran), which the composition
  admission path checks. If compositions ever gain inline glue code,
  that code becomes a brick-shaped candidate and the fence applies to
  it as such.
