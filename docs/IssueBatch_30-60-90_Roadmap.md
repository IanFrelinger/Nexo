# Nexo 30/60/90 Gap-Closure Issue Batch

Use this file to create GitHub issues in the recommended execution order.

## Suggested labels

- `roadmap`
- `phase-30` / `phase-60` / `phase-90`
- `area:product`
- `area:trust`
- `area:mesh`
- `area:sdk`
- `area:runtime`
- `area:devex`
- `area:release`

---

## 1) [Phase 30] Product pilot: secure engineering copilot MVP

**Title:** Product pilot: secure engineering copilot MVP on top of existing Nexo surfaces  
**Labels:** `roadmap`, `phase-30`, `area:product`, `area:trust`

### Problem statement
Nexo has a broad framework and CLI capability set, but lacks a focused user-facing product pilot that proves end-user value.

### Scope
- Build a thin product UX on top of existing `chat`, `orchestrate`, `trust`, and `background-agent` capabilities.
- Support one complete coding-task flow with trust/audit visibility.

### Non-goals
- Full multi-tenant SaaS architecture.
- Generalized plugin marketplace.

### Implementation tasks
- Define MVP workflow (task submission -> execution -> result + audit trail).
- Implement minimal UI/API flow using existing API/portal lanes.
- Expose trust boundary state and recent audit events in the workflow.
- Add operator docs for local and compose-based launch.

### Acceptance criteria
- User can submit a coding task and receive output with auditable execution context.
- Trust dashboard/boundary controls are visible and operable from product flow.
- End-to-end flow works in `docker-compose.agent-server.yml` against mounted repo.
- “First success” walkthrough documented in `docs/`.

### Test plan
- Manual E2E flow test in local compose lane.
- CLI/API integration smoke checks for task lifecycle.

### Sign-off artifacts
- Demo recording/screenshots.
- Sample request/response trace with audit evidence.

### Rollback plan
- Feature-flag or route-disable pilot endpoints and UI surfaces.

---

## 2) [Phase 30] Mesh trust tiers (trusted vs untrusted peers)

**Title:** Add mesh trust tiers and routing policy enforcement for peer execution  
**Labels:** `roadmap`, `phase-30`, `area:mesh`, `area:trust`, `area:runtime`

### Problem statement
Peer mesh routing exists, but explicit trust-tier policy for peers is not completed.

### Scope
- Add peer trust classification (`trusted`, `untrusted`, optionally `unknown`).
- Enforce trust tier in routing decisions.
- Surface trust tier in CLI/API status and audit output.

### Non-goals
- PKI redesign for mutual auth.
- Cross-org federated trust exchange.

### Implementation tasks
- Extend peer capability/ad metadata with trust tier.
- Add routing policy options (`trusted-only`, `trusted-preferred`, `any`).
- Implement CLI commands to view/update peer tier.
- Add audit events for trust-tier based routing decisions.

### Acceptance criteria
- Routing honors selected trust policy under all peer availability states.
- Attempts to route to disallowed tier are blocked with explicit reason.
- CLI shows peer tier and current routing trust policy.
- Tests validate trusted-only behavior and failover semantics.

### Test plan
- Integration tests with mixed-tier peer snapshots.
- Smoke test for policy toggles + routing outcomes.

### Sign-off artifacts
- Test logs for trusted-only and trusted-preferred scenarios.

### Rollback plan
- Default to previous routing behavior via config switch.

---

## 3) [Phase 30] Unified knowledge query layer

**Title:** Create unified cross-store knowledge query layer with provenance  
**Labels:** `roadmap`, `phase-30`, `area:runtime`, `area:product`

### Problem statement
Stores exist for patterns/knowledge/adaptation context, but unified queryability is incomplete.

### Scope
- Add read API that can query across relevant knowledge stores.
- Include provenance pointers in query results.

### Non-goals
- New storage backend migration.
- Full semantic search overhaul.

### Implementation tasks
- Define cross-store query contracts (filters, pagination, source selectors).
- Implement aggregation service and DTO model.
- Add provenance metadata in response schema.
- Document canonical query patterns used by product surfaces.

### Acceptance criteria
- Query API supports paginated, filtered multi-source reads.
- Returned entries include source/provenance references.
- At least three documented query recipes are validated in tests.
- Service can power self-context style “what changed” summaries.

### Test plan
- Integration tests for deterministic query results across seeded stores.

### Sign-off artifacts
- API examples and test outputs for cross-store queries.

### Rollback plan
- Route consumers back to source-specific query paths.

---

## 4) [Phase 30] SDK and port stabilization v1

**Title:** Stabilize SDK/port surface v1 for external integrators  
**Labels:** `roadmap`, `phase-30`, `area:sdk`

### Problem statement
SDK and port definitions are present but still partially complete from an external integrator perspective.

### Scope
- Define and document stable public integration surface.
- Mark internal-only contracts.
- Provide one reference host integration sample.

### Non-goals
- Full backward-compatibility guarantee for all internal namespaces.
- Multi-language SDKs.

### Implementation tasks
- Audit public interfaces and classify support level.
- Add compatibility/versioning policy doc.
- Publish reference sample that uses only supported extension points.
- Add docs for registration and lifecycle expectations.

### Acceptance criteria
- Stable/experimental/internal boundaries are explicit in docs.
- Reference integration runs without internal API dependencies.
- Breaking-change policy is documented and linked in SDK docs.

### Test plan
- Build and run reference sample in CI lane.
- API compatibility smoke checks on supported surface.

### Sign-off artifacts
- SDK support matrix and sample app output.

### Rollback plan
- Keep previous docs path and classify new guidance as advisory until adopted.

---

## 5) [Phase 30] SLO evidence pipeline for release decisions

**Title:** Automate NCR SLO evidence collection and gate enforcement  
**Labels:** `roadmap`, `phase-30`, `area:runtime`, `area:release`

### Problem statement
SLO targets are defined, but release decisions need automated evidence and strict enforcement.

### Scope
- Produce machine-readable SLO evidence artifacts.
- Integrate evidence with runtime/release gating commands/workflows.

### Non-goals
- Building a new observability backend.
- Rewriting existing gate workflows end-to-end.

### Implementation tasks
- Define SLO evidence JSON schema.
- Emit SLO summaries from runtime gate lanes.
- Fail promotion when required thresholds breach.
- Link evidence artifacts in RC checklist docs.

### Acceptance criteria
- Gate workflows emit SLO summary artifacts per run.
- Promotion profile fails on SLO threshold violations.
- RC checklist references concrete artifact names/locations.

### Test plan
- Simulated pass/fail SLO scenarios in CI.

### Sign-off artifacts
- Passing and failing sample SLO gate outputs.

### Rollback plan
- Keep SLO checks in warning mode behind config toggle.

---

## 6) [Phase 60] Capability component registry completion

**Title:** Complete capability component registry metadata and composition filters  
**Labels:** `roadmap`, `phase-60`, `area:runtime`, `area:product`

### Problem statement
Composition engine exists, but capability registry completeness is still partial.

### Scope
- Fill required metadata model for capability components.
- Enable composition filtering by capabilities and constraints.

### Non-goals
- External marketplace UI.
- Dynamic remote package installs.

### Implementation tasks
- Finalize required metadata fields and validation rules.
- Add composition-time filter and compatibility checks.
- Improve diagnostics for missing/invalid component metadata.

### Acceptance criteria
- Registry entries satisfy required metadata schema.
- Compose command supports metadata-driven filtering.
- Validation errors are actionable and deterministic.

### Test plan
- Unit/integration tests for metadata completeness and selection behavior.

### Sign-off artifacts
- Registry audit report and compose filter test output.

### Rollback plan
- Fallback to existing compose behavior when metadata enforcement disabled.

---

## 7) [Phase 60] `doctor --fix` safe remediation mode

**Title:** Add safe `doctor --fix` remediation workflow for onboarding failures  
**Labels:** `roadmap`, `phase-60`, `area:devex`

### Problem statement
Onboarding still has high manual friction for common setup issues.

### Scope
- Add guided, safe remediation mode to doctor flow.
- Emit clear remediation outcomes in human + JSON output.

### Non-goals
- Force-install all optional dependencies automatically.
- Enterprise policy bypass behavior.

### Implementation tasks
- Add fixable problem taxonomy.
- Implement explicit-confirmation remediation actions.
- Add JSON reporting for attempted fixes and outcomes.
- Update onboarding docs with `doctor --fix` flow.

### Acceptance criteria
- Doctor can detect and remediate selected common setup issues.
- No remediation runs without explicit consent.
- Output includes remediation status and follow-up guidance.

### Test plan
- Integration tests with mocked failure scenarios.

### Sign-off artifacts
- Before/after doctor JSON outputs showing successful remediation.

### Rollback plan
- Disable `--fix` path via feature flag if instability appears.

---

## 8) [Phase 60] Onboarding reliability gate expansion

**Title:** Expand onboarding reliability gate with scheduled drift detection  
**Labels:** `roadmap`, `phase-60`, `area:devex`, `area:release`

### Problem statement
Onboarding drift can break setup over time between release cycles.

### Scope
- Add scheduled onboarding gate runs and trend visibility.
- Improve failure diagnostics and remediation links.

### Non-goals
- Replace existing setup gate.
- Add all distro permutations immediately.

### Implementation tasks
- Add scheduled workflow trigger.
- Add failure categorization and summary artifact.
- Link failures to troubleshooting documentation.

### Acceptance criteria
- Scheduled runs execute and retain artifacts.
- Failures are categorized by lane/platform/root cause class.
- Teams can identify regressions from prior scheduled runs.

### Test plan
- Dry-run workflow on branch + one scheduled verification cycle.

### Sign-off artifacts
- Scheduled run summaries and failure taxonomy report.

### Rollback plan
- Keep schedule disabled by default if noise is too high.

---

## 9) [Phase 60] Single-shot release gate orchestration command

**Title:** Add single-shot release gate orchestration command and unified report  
**Labels:** `roadmap`, `phase-60`, `area:release`, `area:devex`

### Problem statement
Release checklist spans many workflows and artifacts, increasing execution burden.

### Scope
- Implement one orchestrator command to run required gate subsets.
- Emit unified PASS/FAIL summary with linked artifacts.

### Non-goals
- Deprecating existing individual commands.
- Replacing all GH workflows in one iteration.

### Implementation tasks
- Add command entrypoint and profile presets.
- Aggregate gate statuses into JSON + markdown report.
- Standardize artifact naming and location references.

### Acceptance criteria
- One command executes configured gate bundle and returns unified verdict.
- Report includes each sub-gate status and artifact pointers.
- RC checklist can be satisfied from orchestrator report outputs.

### Test plan
- Integration test for pass path + forced failure path.

### Sign-off artifacts
- Unified report examples from both pass and fail runs.

### Rollback plan
- Continue using existing individual gate execution paths.

---

## 10) [Phase 60] Regulated-environment trust policy packs

**Title:** Ship predefined trust policy packs for regulated deployment modes  
**Labels:** `roadmap`, `phase-60`, `area:trust`, `area:product`

### Problem statement
Trust features exist, but operators need easy, opinionated policy baselines.

### Scope
- Provide policy packs (e.g. strict enterprise, internal-only, air-gapped).
- Add activation and version visibility through CLI/config.

### Non-goals
- Full policy-as-code platform.
- Per-tenant policy management.

### Implementation tasks
- Define policy pack schema and versioning.
- Add pack loader and activation workflow.
- Expose active pack status in trust dashboard/CLI.
- Add docs mapping packs to deployment scenarios.

### Acceptance criteria
- Operators can apply policy pack in one step.
- Active policy pack/version is visible in status outputs.
- Tests verify pack behavior enforces expected boundaries.

### Test plan
- Trust integration tests across all policy packs.

### Sign-off artifacts
- Pack activation logs and trust behavior test output.

### Rollback plan
- Revert to manual trust config with prior defaults.

---

## 11) [Phase 90] First application-suite vertical

**Title:** Build first application-suite vertical on Nexo kernel primitives  
**Labels:** `roadmap`, `phase-90`, `area:product`

### Problem statement
Application suite remains missing; framework value needs a flagship vertical.

### Scope
- Select one vertical (recommended: engineering release manager or docs assistant).
- Build production-usable workflow on kernel APIs.

### Non-goals
- Multiple verticals at once.
- Full enterprise feature parity.

### Implementation tasks
- Finalize use-case scope and user journeys.
- Implement UX + API + operator controls.
- Add trust/audit/operator observability integration.

### Acceptance criteria
- Pilot users complete defined high-value workflow end-to-end.
- Vertical runs with documented deployment and rollback procedure.
- Uses core primitives (`orchestrate`, trust controls, pipelines) without bypasses.

### Test plan
- End-to-end scenario tests + manual pilot validation.

### Sign-off artifacts
- Pilot runbook, usage metrics, and demo evidence.

### Rollback plan
- Disable vertical routes/features and keep kernel baseline intact.

---

## 12) [Phase 90] Multi-instance mesh governance

**Title:** Add multi-instance mesh governance (admission, revocation, policy propagation)  
**Labels:** `roadmap`, `phase-90`, `area:mesh`, `area:trust`, `area:runtime`

### Problem statement
As mesh adoption grows, governance controls are required for safe operations.

### Scope
- Add peer admission workflow.
- Add peer revocation and policy propagation mechanics.

### Non-goals
- Full external identity provider integration.
- Federated governance across unrelated organizations.

### Implementation tasks
- Implement peer admission state machine.
- Implement revocation path with immediate routing effect.
- Add policy versioning and propagation events.
- Expand audit trail for governance actions.

### Acceptance criteria
- New peers require explicit admission before routing participation.
- Revoked peers are excluded from routing immediately.
- Governance state transitions are visible and auditable.

### Test plan
- Integration tests for admission/revocation propagation.

### Sign-off artifacts
- Governance event logs + routing behavior verification output.

### Rollback plan
- Fallback to static peer allowlist behavior.

---

## 13) [Phase 90] Load/performance certification lane

**Title:** Add formal load/performance certification lane beyond functional gates  
**Labels:** `roadmap`, `phase-90`, `area:runtime`, `area:release`

### Problem statement
Functional release gates exist, but formal load/performance certification remains separate.

### Scope
- Define representative workload matrix.
- Add pass/fail thresholds and retained trend artifacts.

### Non-goals
- Infinite-scale benchmark harness.
- Vendor-specific perf tuning in first pass.

### Implementation tasks
- Finalize workload profiles and thresholds.
- Add CI lane executing load profiles and collecting telemetry.
- Add regression detection and trend report.

### Acceptance criteria
- Certification lane produces repeatable throughput/latency/error metrics.
- Release blocked when thresholds regress beyond tolerance.
- Trend history retained for release candidate comparison.

### Test plan
- Baseline run + controlled regression simulation.

### Sign-off artifacts
- Certification report and trend chart data exports.

### Rollback plan
- Keep lane as advisory (non-gating) until stable thresholds are validated.

---

## 14) [Phase 90] External integrator program and reference integrations

**Title:** Launch external integrator program with reference SDK integrations  
**Labels:** `roadmap`, `phase-90`, `area:sdk`, `area:product`

### Problem statement
SDK maturity depends on real external integration pressure and feedback loops.

### Scope
- Deliver 2–3 reference integrations built only on supported SDK surface.
- Capture and close top external integration pain points.

### Non-goals
- Public marketplace launch.
- Paid partner program setup.

### Implementation tasks
- Select representative integration archetypes.
- Build and document reference integrations.
- Add compatibility matrix and migration guidance.
- Create feedback triage loop for integrator issues.

### Acceptance criteria
- Reference integrations are reproducible and CI-validated.
- Docs clearly separate stable vs experimental integration paths.
- Top external friction issues are tracked and prioritized.

### Test plan
- CI runs for all reference integrations.
- External dry-run by non-core contributors.

### Sign-off artifacts
- Integration docs, CI logs, and feedback summary.

### Rollback plan
- Maintain current SDK guidance while marking program outputs preview.

