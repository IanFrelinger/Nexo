# Nexo Trust Loop — Specification v1.0

Status: Draft for integration
Scope: Applies to any Nexo subsystem in which a model proposes artifacts
(code, config, documents, plans) that downstream systems will trust or
execute. Language is implementation-agnostic; "brick," "certificate,"
and "proposer" carry their standard Nexo meanings.

Keywords MUST / MUST NOT / SHOULD / MAY per RFC 2119.

---

## 0. Core Invariant

> **An artifact carries a certificate if and only if it passed every
> configured real gate, in order, at the recorded content hash.**

Every other requirement in this spec exists to protect this invariant.
Any change that could allow a certificate to exist without a genuine
gate pass — or a gate pass to occur without producing a certificate —
is a breaking change and MUST be treated as a security defect.

---

## 1. Pipeline Shape: Deterministic-First, Model-in-the-Middle, Real-Gates-After

### 1.1 Stages
Every trust-loop pipeline MUST decompose into three ordered stages:

1. **Deterministic preparation (aim).** Cheap, reproducible computation
   that assembles the proposer's context: retrieval, ranking, closure
   walks (e.g., include/dependency graphs), signature harvesting,
   canonicalization, mechanical rewrites. No model calls.
2. **Model proposal (author).** The proposer generates or edits an
   artifact using only the prepared context. The proposer holds no
   authority: its output is a *candidate*, never a result.
3. **Real-gate verification (judge).** External, deterministic
   executors evaluate the candidate: compilers, interpreters, test
   suites, schema validators, engines. Gate verdicts are the only
   source of acceptance.

### 1.2 Requirements
- R1.1 The proposer MUST NOT be able to mark its own output as
  accepted, skip a gate, or reorder the gate chain.
- R1.2 All deterministic preparation MUST be reproducible from inputs
  (same inputs → same context). Randomized steps MUST record their
  seed.
- R1.3 Mechanical transformations with known-correct rules (renames,
  guard fixes, fence stripping, shims) MUST be performed
  deterministically in stage 1 or on candidate ingest — never
  delegated to the proposer.
- R1.4 Context assembly MUST be budgeted (explicit char/token caps per
  source class) and MUST record what was included and what was
  truncated.
- R1.5 Where ground truth exists in machine-readable form (bindings,
  schemas, grammars, reference corpora), stage 1 MUST prefer it over
  documentation or model recall, and the provenance tier of each
  context element SHOULD be recorded.

---

## 2. Certification: Certificate-iff-Gate-Pass

### 2.1 Certificate contents
A certificate MUST contain at minimum:
- `artifact`: path/identifier of the certified artifact
- `content_hash`: cryptographic hash of the exact certified bytes
- `gates_passed`: ordered list of gate identifiers with their versions
  or configurations
- `inputs`: identifiers/hashes of the truth sources and context used
- `proposer`: model/agent identity and parameters
- `attempts` or `steps`: the effort record (see §5)
- `timestamp` (UTC)

### 2.2 Requirements
- R2.1 Certificates MUST be minted only by the gate runner, at the
  moment the final gate passes, over the exact bytes that passed.
- R2.2 Any mutation of the artifact after minting MUST invalidate the
  certificate (hash mismatch = uncertified).
- R2.3 Gate chains MUST be fail-closed: a missing, misconfigured, or
  erroring gate is a FAIL, not a skip.
- R2.4 Partial success MUST NOT certify. If gates 1..n-1 pass and gate
  n fails, no certificate exists; the run record notes the furthest
  gate reached.
- R2.5 Acceptance decisions MUST derive only from gate exit status and
  declared output contracts — never from proposer self-reports,
  confidence values, or textual claims of success.
- R2.6 A "trusted" upstream input (e.g., a truth model) MUST itself
  carry evidence of how it earned trust (calibration statistics,
  source provenance, adjudication records) so certificates compose
  into chains.

---

## 3. Feedback: Diagnosed Causes, Not Raw Symptoms

### 3.1 Principle
Proposer capability is multiplied or wasted by feedback quality. Raw
tool output (compiler errors, stack traces, diffs) describes symptoms
at the point of failure; the proposer needs causes at the point of
authorship. The loop, not the proposer, owns the translation.

### 3.2 Diagnostic probes
- R3.1 Each gate SHOULD be paired with cheap deterministic probes that
  run automatically on failure and convert symptoms into causes.
  Examples of the pattern:
  - preprocessing/expansion probes ("your X vanishes before the
    compiler sees it; conditional on undefined macro Y swallows it")
  - symbol-resolution probes ("unknown symbol 'Z'; nearest matches in
    the reference corpus: ...")
  - structural probes (namespace/section presence, guard form,
    linkage class)
- R3.2 Probe output MUST be pre-interpreted: a short causal statement
  plus the minimum supporting facts, not a raw dump.
- R3.3 Known recurring failure classes MUST be promoted to
  deterministic fences (detect-and-fix or detect-and-reject on
  candidate ingest) so the proposer never re-litigates a solved
  problem. The fence catalog is expected to grow monotonically;
  each new observed quirk becomes a fence or a probe.
- R3.4 Repair feedback MUST name a fix target when one is inferable
  ("unknown symbol 'foo' — locate the correct name, then edit"),
  and MUST restate the relevant constraint verbatim when the failure
  is a constraint violation.

### 3.3 Constraint manifests
- R3.5 Hard structural requirements on the artifact (required
  namespaces/sections, forbidden dependencies, registration points)
  MUST exist once as a machine-checkable manifest used twice:
  injected into the proposer's instructions AND enforced by a
  pre-gate. Prose-only constraints are non-conformant.

---

## 4. Progress Discipline: Fail Fast, Never Falsely

### 4.1 Repair loops (single-shot proposers)
- R4.1 After a substantial candidate exists, repair SHOULD proceed by
  anchored edits (find/replace with exact-unique-match verification,
  whitespace-tolerant fallback) rather than whole-artifact
  regeneration; whole regeneration remains the fallback after
  repeated edit failure.
- R4.2 Every candidate mutation MUST be immediately followed by
  automatic re-verification (at minimum the cheapest gate), with the
  result attached to the mutation's record.

### 4.2 Agentic loops (action-choosing proposers)
- R4.3 The action space MUST be a whitelist executed by a
  deterministic harness. Read access MUST be confined to declared
  roots and source-like file types; write access MUST be confined to
  the candidate. Compiler include paths MUST NOT widen the read
  sandbox.
- R4.4 Phase structure MUST be enforced: no edits before at least one
  probe and a recorded diagnosis; no completion before clean
  verification.
- R4.5 The harness MUST detect and interrupt non-progress:
  - exact-repeat read-only actions → reject with pointer to prior
    result
  - candidate-state revisits (hash history) → oscillation
    interruption demanding a fresh approach
  - N consecutive failed or non-improving edits → abandon-increment
    instruction (from-scratch rewrite demand)
  - escalation that itself repeats without progress → progressive
    escalation: guidance menu → rewrite demand → clean abort
- R4.6 All loops MUST carry hard budgets (steps, attempts, wall-clock,
  and where relevant per-task throughput guards vs. session median).
  Budget exhaustion and hard stalls MUST terminate cleanly with the
  best candidate preserved and labeled uncertified.
- R4.7 The only permitted terminal states are: (a) certified success,
  or (b) explicit, explained failure with preserved artifacts.
  Silent spin, partial ship, and optimistic timeout are
  non-conformant.

---

## 5. Observability: Evidence Is a Safety Property

- R5.1 Every rejected candidate, failed attempt, and abandoned state
  MUST be persisted (content + verdict + feedback given), not
  discarded. Failure artifacts are first-class outputs.
- R5.2 Agentic sessions MUST maintain an append-only ledger of
  (action, observation digest) pairs, replayed into each turn as the
  proposer's working memory, and embedded in full in the final
  certificate or failure report.
- R5.3 Long-running jobs MUST checkpoint results atomically after every
  unit of work and support lossless resume; derivation/preparation
  results SHOULD be cached and keyed by input signatures.
- R5.4 Console/log output MUST be flushed in real time (no buffered
  silence on long jobs) and MUST distinguish, per unit: verdict,
  effort (attempts/steps/seconds), and failure category.
- R5.5 Failure categories are part of the tool's contract. An "other"
  bucket exceeding a small share of failures indicates the taxonomy —
  not the workload — is defective, and MUST trigger taxonomy repair.
- R5.6 Where operation spans a disclosure boundary, tools MUST be
  safe-by-construction: aggregate counts, categories, and verdicts
  on the shareable side; content-bearing evidence persisted only on
  the protected side.

---

## 6. Environment Discipline

- R6.1 Sessions MUST begin with resource attestation (memory, swap,
  competing tenants) and refuse to start under the configured floor.
- R6.2 Memory budgets MUST account for the tool's own data structures,
  model residency, and cache behavior — not just the model.
- R6.3 Platform resource ceilings MUST be audited against hardware
  before architecture adapts to scarcity; inherited limits are
  configuration hypotheses, not facts.
- R6.4 After patching any tool, in-flight instances MUST be restarted;
  a patched file never reaches a running process.

---

## 7. Adversarial Validation (acceptance criteria for the loop itself)

A trust-loop implementation MUST NOT be considered production-ready
until it passes an adversarial campaign in which a scripted proposer
plays, at minimum:

- sandbox escape attempts (path traversal, out-of-root reads,
  non-source reads) → all walled
- premature completion on failing artifacts, repeated → all rejected
- malformed actions (null/missing args, unknown actions) → absorbed
  without crash
- anchor abuse (nonexistent and ambiguous edit anchors) → rejected
  without corruption
- oscillating and non-converging edit sequences → interrupted
- oversized/garbage outputs → rejected or safely failed
- known model-quirk profiles (format wrapping, idiom contamination,
  instruction decay, repetition under uncertainty) → absorbed by
  fences and escalation
- an "ends bad" run (proposer insists on shipping broken work) →
  terminates with no certificate
- a "recovers" run (proposer repairs to genuinely good work) →
  certifies with full provenance

Campaign requirement: at completion, zero false certificates across
all scenarios, and at least one fully clean end-to-end pass per
supported artifact class. Defects found in the harness during the
campaign MUST be fixed and the affected scenarios re-run to green.

---

## 8. Design Rationale (non-normative)

- Capability is rarely the bottleneck; the loop is. Feedback quality,
  tool access, and enforced discipline recover more performance than
  the next model tier, and they compound with any model upgrade.
- Autonomy is tool access wearing intelligence's clothes: probes
  discover facts, the proposer synthesizes over facts, gates verify.
  Keeping tool-wielding on the deterministic side preserves
  auditability without sacrificing autonomy's benefits.
- The fence catalog and probe library are the system's accumulated
  experience. They convert every observed failure, once diagnosed,
  into a permanent, free, deterministic capability — the mechanism by
  which the loop, not the model, gets smarter over time.
