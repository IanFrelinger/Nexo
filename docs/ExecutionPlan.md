# Nexo Execution Plan

Comprehensive execution plan derived from the 30/60/90 roadmap, North Star gap analysis, and current codebase state. Each work item includes what already exists, what remains, dependencies, risks, and concrete implementation tasks.

**Guiding principle:** Every item is sequenced so that earlier work de-risks or enables later work. Items within the same phase can be parallelized unless a dependency is noted.

---

## Phase 1 — Foundation & Product Proof (Phase-30 items)

These items establish the first end-to-end product experience and close the most critical framework gaps. Complete these before moving to Phase 2.

---

### 1.1 Secure Engineering Copilot MVP

**What exists:**
- `POST /api/copilot/task` endpoint with orchestration + audit trail (`NexoEndpoints.cs`).
- Portal UI in `wwwroot/index.html` with chat-style copilot task flow.
- Trust dashboard/pause/rules endpoints (`/api/trust/*`).
- `docker-compose.agent-server.yml` for local deployment.
- Walkthrough doc (`docs/Phase1SecureCopilotWalkthrough.md`).

**What remains:**
- The copilot flow is synchronous request/response with no task persistence, correlation IDs, or execution history.
- No dedicated authz on copilot endpoints (relies on shared API-key middleware).
- Portal UI is minimal — no execution history view, no audit timeline visualization.
- No operator onboarding wizard or guided first-run experience.

**Implementation tasks:**
1. Add task persistence: store copilot task submissions and results in LiteDB with correlation IDs. Define `ICopilotTaskStore` port in `Nexo.Core.Application`, implement in `Nexo.Infrastructure`.
2. Add execution history endpoint: `GET /api/copilot/tasks` with pagination, filtering by status/date.
3. Add correlation ID propagation: generate a `copilotTaskId` on submission, thread it through `OrchestrateAsync` and into audit log entries so the audit trail is filterable per task.
4. Enhance portal UI: add task history sidebar, audit timeline per task, trust boundary status indicator.
5. Add operator first-run flow: detect empty state, show guided setup (API key, Ollama connection, trust pack selection).
6. Update `docs/Phase1SecureCopilotWalkthrough.md` with the full flow including history and audit drill-down.
7. Add E2E smoke test: submit task via API, verify result + audit entries + history retrieval.

**Files to modify:**
- `src/Nexo.Core.Application/` — new port `ICopilotTaskStore`
- `src/Nexo.Infrastructure/` — LiteDB implementation
- `src/Nexo.API/Endpoints/NexoEndpoints.cs` — history endpoint, correlation IDs
- `src/Nexo.API/wwwroot/index.html` — portal UI enhancements
- `src/Nexo.Hosting/NexoServiceCollectionExtensions.cs` — register new services
- `docs/Phase1SecureCopilotWalkthrough.md`

**Dependencies:** None — this is the first item to start.  
**Risks:** Orchestration latency may make synchronous copilot tasks feel slow for complex requests. Consider adding a simple polling/status pattern if tasks exceed ~10s.

---

### 1.2 Wire Observe → Improve Integration

**What exists:**
- `nexo observe` populates `IPatternStore` (LiteDB) with patterns (`repeated-edits`, `edit-then-build`).
- `nexo improve` runs static analysis → adaptation on a fixed or user-specified path.
- `nexo improve --self` runs `SelfImprovementLoop` which reads both `ITestFailureStore` and `IPatternStore`.
- `SelfImprovementLoop` already queries `repeated-edits` and `edit-then-build` patterns.

**What remains:**
- Default `nexo improve` (without `--self`) does not read `IPatternStore` at all — it targets a fixed path.
- No `--from-observation` flag to query recent patterns and prioritize analysis.
- No single `nexo loop` command that runs observe + improve sequentially.
- Test failure ingestion into `ITestFailureStore` is not wired from real test runs (only seeded in tests).

**Implementation tasks:**
1. Add `--from-observation` option to `ImproveCommand`: when set, query `IPatternStore` for recent patterns, extract affected file paths, and use them as analysis targets instead of the default path.
2. Register `IPatternStore` in the `ImproveCommand` service collection (currently only `ObserveCommand` registers it).
3. Wire TRX → `ITestFailureStore`: after `dotnet test` via `DotnetTestTool` / `TrxTestResultParser`, bridge parsed `TestResult` failures into `ITestFailureStore.RecordAsync`. Add this bridge in `Nexo.Infrastructure` (e.g. `TestFailureIngestionBridge`).
4. Add `nexo improve --continuous` that runs observe (N minutes configurable) then improve on observed paths, in a loop.
5. Add integration tests validating the observe → improve → self-context pipeline with pattern-driven targeting.
6. Document the wired flow in `docs/GapAnalysis.md` and update the recommendation section.

**Files to modify:**
- `src/Nexo.CLI/Commands/ImproveCommand.cs` — add `--from-observation`, wire `IPatternStore`
- `src/Nexo.Infrastructure/SelfImprovement/` — `TestFailureIngestionBridge`
- `src/Nexo.Infrastructure/Validation/` — bridge from `TrxTestResultParser` to `ITestFailureStore`
- `src/Nexo.Tests.Infrastructure/` — integration tests
- `docs/GapAnalysis.md`

**Dependencies:** None — can be done in parallel with 1.1.  
**Risks:** Pattern store may contain noisy or stale entries; add a staleness filter (e.g. patterns older than 7 days are excluded by default).

---

### 1.3 Mesh Trust Tiers — Routing Policy Enforcement

**What exists:**
- `PeerTrustTier` enum (`Unknown`, `Untrusted`, `Trusted`) on `PeerInfo`.
- CLI `--set-trust-tier` updates peer trust tier in `instances.json` with metadata preservation.
- `PeerTrustPolicyResolver`, `NcrCapabilityRouter`, `NexoPeerBrickExecutor` in routing infrastructure.

**What remains:**
- Verify routing policy actually blocks execution to disallowed tiers (audit the `NcrCapabilityRouter` behavior).
- Add routing policy options surface: `trusted-only`, `trusted-preferred`, `any` — confirm these are configurable and enforced.
- Surface trust tier in `nexo mesh` status output and API status endpoints.
- Add audit events for trust-tier routing decisions (accepted/rejected with reason).

**Implementation tasks:**
1. Audit `NcrCapabilityRouter` and `PeerTrustPolicyResolver` to confirm tier enforcement end-to-end. Document findings.
2. If routing policy options are not yet configurable via CLI/env, add config: `NEXO_MESH_TRUST_POLICY=trusted-only|trusted-preferred|any`.
3. Add tier display to `nexo mesh` list output (show tier next to each peer).
4. Add audit log entries when a peer is skipped due to trust tier policy.
5. Add integration tests: mixed-tier peer snapshots, `trusted-only` blocks `untrusted`, `trusted-preferred` falls back correctly.
6. Add smoke test for policy toggle + routing outcomes.

**Files to modify:**
- `src/Nexo.Infrastructure/Execution/Routing/` — policy enforcement, audit
- `src/Nexo.Infrastructure/Mesh/` — status display
- `src/Nexo.CLI/Commands/MeshCommand.cs` — tier display
- `src/Nexo.Tests.Infrastructure/` and `src/Nexo.Tests.CLI/` — tests

**Dependencies:** None — can be done in parallel with 1.1 and 1.2.  
**Risks:** Low. The data model and CLI surface already exist; this is mostly wiring and verification.

---

### 1.4 SDK and Port Stabilization v1

**What exists:**
- `Nexo.Sdk` with `NexoSdkBuilder` (thin wrapper over `Nexo.Client`).
- `docs/sdk.md` with stable/experimental/internal classification.
- `docs/samples/StableSdkHostSample/` with `Program.cs` showing brick/agent registration.
- `Nexo.Abstractions` contains core port definitions.

**What remains:**
- No CI lane validates the reference sample builds and runs independently.
- No explicit `[Stable]` / `[Experimental]` attributes on public APIs.
- No breaking-change policy document.
- `UseAdaptiveRouting()` is documented as a no-op / experimental.

**Implementation tasks:**
1. Add CI lane that builds and runs `StableSdkHostSample` in isolation (verify it compiles against published/local NuGet only — no project references to internals).
2. Add `docs/SdkCompatibilityPolicy.md`: semver rules, deprecation process, breaking-change notification.
3. Audit all public types in `Nexo.Sdk`, `Nexo.Client`, `Nexo.Abstractions`, `Nexo.Brick.Contracts` — classify each as Stable / Experimental / Internal. Add classification table to `docs/sdk.md`.
4. Add XML doc comments indicating stability level on key public interfaces.
5. Remove or clearly mark `UseAdaptiveRouting()` as experimental in `NexoSdkBuilder`.

**Files to modify:**
- `.github/workflows/` — new or extended workflow for SDK sample CI
- `docs/sdk.md` — classification table
- `docs/SdkCompatibilityPolicy.md` — new
- `src/Nexo.Sdk/NexoSdkBuilder.cs` — stability annotations
- `src/Nexo.Abstractions/` — stability annotations on ports

**Dependencies:** None — can be done in parallel.  
**Risks:** Premature stabilization of APIs that may need to change. Mitigate by keeping the stable surface minimal (only `Nexo.Sdk`, `Nexo.Client`, `Nexo.Brick.Contracts`).

---

### 1.5 SLO Evidence Pipeline Hardening

**What exists:**
- `RuntimeCommand` with `--emit-slo-evidence`, `--slo-warning-only`, env thresholds (`NEXO_RELEASE_SLO_*`).
- `RuntimeSloEvidence` / `RuntimeLaneSloEvidence` models.
- `ci release-bundle` includes runtime-gate step.
- Tests in `RuntimeCommandTests` validating SLO JSON output.

**What remains:**
- SLO evidence is runtime-gate scoped; broader release gates (cross-platform, trust, caching) don't emit SLO summaries.
- RC checklist (`docs/ReleaseCandidateChecklist-v1.md`) may not reference concrete SLO artifact paths.
- No trend comparison between releases.

**Implementation tasks:**
1. Extend `ci release-bundle` to collect SLO evidence from each gate step (not just runtime-gate).
2. Add SLO summary section to `release-bundle-report.md` output.
3. Update `docs/ReleaseCandidateChecklist-v1.md` to reference SLO artifact paths from release-bundle output.
4. Add `--compare-baseline` option to compare current SLO metrics against a stored baseline.
5. Add test for forced SLO threshold violation → promotion failure.

**Files to modify:**
- `src/Nexo.CLI/Commands/CiCommand.cs` — extend release-bundle SLO collection
- `src/Nexo.CLI/Commands/RuntimeCommand.cs` — baseline comparison
- `docs/ReleaseCandidateChecklist-v1.md`
- `src/Nexo.Tests.CLI/` — threshold violation test

**Dependencies:** Lightweight dependency on 1.1 (copilot MVP may inform which SLO metrics matter most).  
**Risks:** Low. SLO infrastructure is already functional; this is extension and documentation.

---

## Phase 2 — Framework Maturity (Phase-60 items)

These items deepen the framework's reliability, developer experience, and operational readiness. Start after Phase 1 core items (1.1, 1.2) are complete.

---

### 2.1 Capability Component Registry Completion

**What exists:**
- `CapabilityComponentRegistry` with in-memory store, registration, lookup, `ValidateSelection`.
- `ComponentDescriptor` model with `Id`, `Capability`, `DisplayName`, `Summary`, `InputSchema`, `OutputSchema`, `Version`, `SupportLevel`, `RequiredCapabilities`, `IncompatibleWith`.
- `ComponentDescriptorValidator` enforces core fields, rejects `TBD` implementation types, validates semver.
- Seed includes real components + North Star placeholders via `PlaceholderCapabilityComponent`.

**What remains:**
- `InputSchema` / `OutputSchema` are on the model but not validated by `ComponentDescriptorValidator`.
- Composition-time filtering by capability/constraint is not fully exposed in CLI.
- Placeholder components need replacement with real implementations or explicit exclusion from production composition.

**Implementation tasks:**
1. Add `InputSchema` / `OutputSchema` validation to `ComponentDescriptorValidator` (non-empty when `SupportLevel` is `Stable`).
2. Add `nexo compose --filter-capability <cap>` to filter components by capability during composition.
3. Add `nexo compose --list-components` to show registry contents with metadata completeness indicators.
4. Replace or remove `PlaceholderCapabilityComponent` entries that have real implementations.
5. Add registry audit report command: `nexo compose audit` showing completeness per component.
6. Add unit tests for schema validation and composition filtering.

**Files to modify:**
- `src/Nexo.Infrastructure/Composition/ComponentDescriptorValidator.cs`
- `src/Nexo.Infrastructure/Composition/CapabilityComponentRegistry.cs`
- `src/Nexo.CLI/` — compose subcommands
- `src/Nexo.Tests.Infrastructure/`

**Dependencies:** Benefits from 1.4 (SDK stabilization clarifies which components are stable).  
**Risks:** Schema validation may break existing tests that use minimal descriptors. Add migration path with warnings before hard enforcement.

---

### 2.2 Doctor `--fix` Remediation Hardening

**What exists:**
- `DoctorCommand` with `--fix` and `--yes` flags.
- `DoctorRemediation.cs` implements remediation runner.
- `scripts/onboarding/failure-taxonomy.sh` maps failures to remediation.
- `ci release-bundle` runs `doctor --json` (read-only check).

**What remains:**
- Verify remediation coverage: which failure categories does `--fix` actually handle?
- JSON output from `--fix` may not include before/after state for all remediation actions.
- No integration test with mocked failure scenarios validating end-to-end remediation.

**Implementation tasks:**
1. Audit `DoctorRemediation.cs` — catalog which failure types are handled and which are not.
2. Add before/after state to JSON remediation output for each action.
3. Add mocked-failure integration tests: simulate missing tools, wrong SDK version, broken config — verify `--fix` resolves them.
4. Add `nexo doctor --fix --dry-run` mode that reports what would be fixed without acting.
5. Update `docs/GettingStarted.md` with `doctor --fix` as part of the setup flow.
6. Align `failure-taxonomy.sh` categories with `DoctorRemediation` problem taxonomy.

**Files to modify:**
- `src/Nexo.CLI/Commands/DoctorCommand.cs`
- `src/Nexo.CLI/Commands/DoctorRemediation.cs`
- `src/Nexo.Tests.CLI/` — remediation integration tests
- `scripts/onboarding/failure-taxonomy.sh`
- `docs/GettingStarted.md`

**Dependencies:** None.  
**Risks:** Auto-remediation that modifies system state is inherently risky. Keep `--yes` as opt-in; default to interactive confirmation.

---

### 2.3 Onboarding Reliability Gate Expansion

**What exists:**
- `onboarding-quickstart-gate.yml` with weekly cron schedule (`0 7 * * 1`).
- Drift signals emitted as JSON (`driftSignals.isScheduledRun`, `isRetry`).
- Native + container lanes.

**What remains:**
- Drift detection is a signal in JSON but there is no automated comparison between scheduled runs.
- No failure categorization by lane/platform/root cause in the workflow output.
- No trend dashboard or alerting on regression.

**Implementation tasks:**
1. Add run-over-run comparison: store previous scheduled run summary as a workflow artifact, diff against current run.
2. Add failure categorization step: parse test/setup outputs and classify by `failure-taxonomy.sh` categories.
3. Add summary artifact with platform × lane × failure-category matrix.
4. Add optional notification (GitHub issue or workflow annotation) when new failures appear vs. previous scheduled run.
5. Add documentation in `docs/Testing.md` explaining the drift detection mechanism.

**Files to modify:**
- `.github/workflows/onboarding-quickstart-gate.yml`
- `scripts/onboarding/failure-taxonomy.sh` — extend for workflow integration
- `docs/Testing.md`

**Dependencies:** Benefits from 2.2 (doctor `--fix` provides remediation for detected drift).  
**Risks:** Scheduled CI runs consume minutes; keep the workflow lean. Use `workflow_dispatch` for on-demand deep checks.

---

### 2.4 Release Gate Orchestration — Unified Reports

**What exists:**
- `ci release-bundle` with `--profile` (`default` | `quick`).
- Steps: verify → runtime-gate → doctor JSON.
- Outputs `release-bundle-report.json` and `release-bundle-report.md`.

**What remains:**
- Only three steps in the bundle; cross-platform, trust, and caching gates are not included.
- Report may not aggregate sub-gate pass/fail status clearly.
- No `--profile full` that runs all gates.

**Implementation tasks:**
1. Add `--profile full` that includes: verify, runtime-gate, doctor, trust-gate, cross-platform-smoke, caching-smoke.
2. For each step, capture exit code + summary + artifact paths.
3. Enhance `release-bundle-report.md` with a clear pass/fail matrix per gate.
4. Add `--fail-fast` option (default true) to stop on first failure, and `--continue` to run all gates.
5. Add integration test for pass-path and forced-failure-path.
6. Update `docs/ReleaseCandidateChecklist-v1.md` to reference `ci release-bundle --profile full`.

**Files to modify:**
- `src/Nexo.CLI/Commands/CiCommand.cs` — profiles, steps, report format
- `src/Nexo.Tests.CLI/` — integration tests
- `docs/ReleaseCandidateChecklist-v1.md`

**Dependencies:** Benefits from 1.5 (SLO evidence feeds into release report).  
**Risks:** Full gate profile may be slow. Mitigate with `--profile quick` as default and `full` for RC cuts.

---

### 2.5 Trust Policy Pack Maturation

**What exists:**
- Three packs: `air-gapped.json`, `internal-only.json`, `strict-enterprise.json`.
- `TrustPolicyPackRegistry` with load, activate, persist to `active-pack.json`.
- CLI `trust pack list` / `trust pack apply --id`.
- `IAccessBoundary.ApplyPolicyPack` integration.

**What remains:**
- Verify pack behavior actually enforces the described boundaries (not just metadata).
- No trust integration tests that run across all packs.
- Pack version visibility in `nexo status` or API status output may be incomplete.

**Implementation tasks:**
1. Add trust integration tests for each pack: apply pack, attempt operations that should be blocked, verify enforcement.
2. Add pack version and active status to `GET /api/status` response.
3. Add `nexo trust pack describe --id <id>` to show pack rules in human-readable form.
4. Add pack migration guidance: document how to transition between packs.
5. Add operator docs mapping deployment scenarios to recommended packs.

**Files to modify:**
- `src/Nexo.Tests.Infrastructure/` — trust pack integration tests
- `src/Nexo.API/Endpoints/NexoEndpoints.cs` — status enrichment
- `src/Nexo.CLI/Commands/TrustCommand.cs` — `describe` subcommand
- `docs/` — operator pack selection guide

**Dependencies:** None.  
**Risks:** Low. Infrastructure exists; this is verification and documentation.

---

## Phase 3 — Production Readiness & Vision (Phase-90 items)

These items push Nexo toward production use and external adoption. Start after Phase 2 core items are stable.

---

### 3.1 First Application-Suite Vertical

**What exists:**
- `apps/runtime-studio/` with agent-set JSON configs and shell scripts.
- Game director variant with dedicated agent set.
- Background agent infrastructure (scheduling, registry, lifecycle).
- Copilot MVP endpoint (from 1.1).

**What remains:**
- No standalone, product-quality vertical application.
- Runtime Studio is config + scripts, not a deployable product.

**Recommended vertical: Engineering Release Manager**
- Leverages existing: `ci release-bundle`, SLO evidence, trust packs, background agents, changelog generation.
- User journey: configure release criteria → agents monitor repo → automated release readiness assessment → operator approval → release.

**Implementation tasks:**
1. Define user journeys and acceptance criteria for the release manager vertical.
2. Build release manager agent set (JSON config for background agents): repo monitor, test runner, SLO collector, report generator.
3. Add release manager API endpoints or extend copilot flow for release-specific queries.
4. Build minimal dashboard UI (extend portal or standalone page) showing release readiness status.
5. Add deployment docs: compose file, env config, operator guide.
6. Add E2E scenario test: repo change → agent detection → readiness report → approval simulation.

**Files to modify:**
- `apps/` — new `release-manager/` directory with agent configs
- `src/Nexo.API/` — release-specific endpoints or views
- `src/Nexo.BackgroundAgents/` — release monitor agent role
- `docs/` — vertical docs

**Dependencies:** 1.1 (copilot MVP), 1.5 (SLO pipeline), 2.4 (release gate orchestration).  
**Risks:** Scope creep. Keep the first vertical minimal: readiness dashboard + automated checks. Defer approval workflows and notifications to a follow-up.

---

### 3.2 Multi-Instance Mesh Governance

**What exists:**
- Mesh discovery, `nexo mesh` commands, peer trust tiers (from 1.3).
- Shared adaptation cache, sneakernet export/import.
- `FileBasedInstanceDiscovery`, `FileBasedCapabilityAdvertisement`.

**What remains:**
- No peer admission workflow (peers are added by file manipulation).
- No revocation with immediate routing effect.
- No policy propagation between instances.

**Implementation tasks:**
1. Design peer admission state machine: `pending` → `admitted` → `active` (or `rejected`).
2. Implement admission workflow in `MeshCommand`: `nexo mesh admit <peerId>`, `nexo mesh revoke <peerId>`.
3. Wire revocation into routing: revoked peers are immediately excluded from `NcrCapabilityRouter`.
4. Add policy version field to mesh advertisements; receiving peers can detect and warn on mismatched policy versions.
5. Add governance audit trail: all admission/revocation events logged.
6. Add integration tests for admission → routing → revocation → routing-exclusion flow.

**Files to modify:**
- `src/Nexo.Core.Application/Mesh/` — admission models
- `src/Nexo.Infrastructure/Mesh/` — admission state persistence
- `src/Nexo.Infrastructure/Execution/Routing/` — revocation enforcement
- `src/Nexo.CLI/Commands/MeshCommand.cs`
- `src/Nexo.Tests.Infrastructure/` and `src/Nexo.Tests.CLI/`

**Dependencies:** 1.3 (mesh trust tiers provide the trust model that governance builds on).  
**Risks:** Distributed state consistency. Keep it simple: file-based state with eventual consistency, not distributed consensus.

---

### 3.3 Load/Performance Certification Lane

**What exists:**
- Runtime benchmarks directory (`docs/runtime/benchmarks/README.md`).
- SLO evidence with configurable thresholds.
- `dotnet test` with blame-hang timeouts.

**What remains:**
- No formal load test harness.
- No repeatable workload profiles.
- No trend retention or regression detection.

**Implementation tasks:**
1. Define 3 representative workload profiles: single-agent simple task, multi-agent orchestration, mesh-routed execution.
2. Implement benchmark harness using BenchmarkDotNet or a custom runner that emits structured JSON results.
3. Add CI workflow (`perf-certification.yml`) that runs workload profiles and stores results as artifacts.
4. Add regression detection: compare current run against stored baseline; fail on >10% degradation (configurable).
5. Add trend report generation: markdown summary with throughput/latency/error rates across recent runs.

**Files to modify:**
- `src/` — new `Nexo.Benchmarks` project or extend `Nexo.Tests.Infrastructure`
- `.github/workflows/` — `perf-certification.yml`
- `docs/runtime/benchmarks/` — workload profiles and trend docs

**Dependencies:** 1.1 (copilot MVP defines the primary workload), 2.4 (release gate can include perf lane).  
**Risks:** Benchmarks on CI runners are noisy. Use relative comparison (% change) rather than absolute thresholds. Run on dedicated runner class if available.

---

### 3.4 External Integrator Program

**What exists:**
- `docs/sdk.md` with stability tiers.
- `StableSdkHostSample` reference integration.
- `Nexo.Brick.Contracts` for plugin wire format.

**What remains:**
- Only one reference integration exists.
- No external integrator feedback loop.
- No compatibility matrix across versions.

**Implementation tasks:**
1. Build 2 additional reference integrations: (a) external brick provider (HTTP catalog), (b) custom background agent using SDK.
2. Add integration CI lane: build all reference integrations against current SDK.
3. Create compatibility matrix: SDK version × reference integration version → pass/fail.
4. Add `docs/IntegratorGuide.md`: onboarding steps, common patterns, troubleshooting.
5. Add feedback triage template (GitHub issue template for integrators).
6. Conduct dry-run with a non-core contributor; document friction points.

**Files to modify:**
- `docs/samples/` — additional reference integrations
- `.github/workflows/` — SDK compatibility CI
- `.github/ISSUE_TEMPLATE/` — integrator feedback template
- `docs/IntegratorGuide.md`

**Dependencies:** 1.4 (SDK stabilization defines what integrators build against).  
**Risks:** External adoption feedback may require breaking changes. Use the compatibility policy from 1.4 to manage expectations.

---

## Cross-Cutting Items (any phase)

These are smaller items that should be addressed opportunistically alongside the phased work.

---

### C.1 Fix `HttpBarrierContextMiddleware`

**Current state:** No-op middleware — `InvokeAsync` just calls `next(context)`. The TODO says: populate `BarrierResolutionContext` from `HttpContext` and run `IBarrierIdentityResolverPipeline`.

**Tasks:**
1. Inject `IBarrierIdentityResolverPipeline` into the middleware constructor.
2. Build `BarrierResolutionContext` from `HttpContext` (extract claims, headers, request metadata).
3. Call pipeline and handle results (block, allow, audit).
4. Add integration test with mock pipeline.

**Files:** `src/Nexo.Runtime/Barriers/Identity/HttpBarrierContextMiddleware.cs`  
**Risk:** Breaking change if existing deployments rely on the no-op behavior. Add feature flag (`NEXO_BARRIER_MIDDLEWARE_ENABLED`).

---

### C.2 Implement `PerfHeadroom` Policy

**Current state:** Always returns `Approve = true`. `_maxPerTool` is stored but unused.

**Tasks:**
1. Implement time-budget tracking per tool invocation.
2. Reject when cumulative tool time exceeds `_maxPerTool`.
3. Add unit tests for approval and rejection paths.

**Files:** `src/Nexo.Policies/PerfHeadroom.cs`  
**Risk:** Low. Policy is opt-in.

---

### C.3 Fix CI `continue-on-error` in Test Caching Workflow

**Current state:** `test-caching-multi-env.yml` has `continue-on-error: true` on Docker build, test, and publish steps. The summary job always succeeds.

**Tasks:**
1. Remove `continue-on-error: true` from build and test steps.
2. Keep `continue-on-error: true` only on optional publish/reporting steps.
3. Add proper failure aggregation in summary job: check `needs.test-caching.result`.
4. Fix Windows `|| true` to use proper error handling.

**Files:** `.github/workflows/test-caching-multi-env.yml`  
**Risk:** May expose currently-hidden test failures. Run the workflow once before and after to understand impact.

---

### C.4 Add PR Template and Issue Templates

**Tasks:**
1. Create `.github/PULL_REQUEST_TEMPLATE.md` with sections: Summary, Changes, Testing, Checklist.
2. Create `.github/ISSUE_TEMPLATE/bug_report.md` with reproduction steps template.
3. Create `.github/ISSUE_TEMPLATE/feature_request.md`.
4. Create `.github/ISSUE_TEMPLATE/integrator_feedback.md` (for 3.4).

**Files:** `.github/PULL_REQUEST_TEMPLATE.md`, `.github/ISSUE_TEMPLATE/`  
**Risk:** None.

---

### C.5 Add Self-Improvement as Background Agent

**Current state:** `SelfImprovementLoop` is only triggered via `nexo improve --self`. Background agents support `optimizer`, `tester`, `extender` roles but not a `self-improver` role.

**Tasks:**
1. Add `self-improver` role to `BackgroundAgentRegistry.ExecuteAgentAsync`.
2. Wire role to call `ISelfImprovementLoop.RunOnceAsync`.
3. Add sample agent config in `apps/runtime-studio/config/`.
4. Document in `docs/Configuration.md`.

**Files:** `src/Nexo.BackgroundAgents/Registry/BackgroundAgentRegistry.cs`, `apps/runtime-studio/config/`  
**Risk:** Self-improvement running automatically requires safety guardrails. Use `SemiActive` aggressiveness mode by default (requires approval gate).

---

### C.6 Trust Wiring in `ImproveCommand`

**Current state:** `ImproveCommand` builds its own service collection and registers `ProviderFactory` directly, bypassing hosting-level trust registration. If improve uses cloud LLM providers, trust sanitization may not be active.

**Tasks:**
1. Conditionally register `SanitizingProviderFactory` in `ImproveCommand`'s DI graph when trust is enabled.
2. Add a test that validates sanitization is active when improve is configured for cloud-backed fix generation.

**Files:** `src/Nexo.CLI/Commands/ImproveCommand.cs`, `src/Nexo.Tests.CLI/`  
**Risk:** Low. Defensive wiring.

---

## Execution Sequence Diagram

```
Phase 1 (parallel tracks):
├── Track A: 1.1 Copilot MVP ──────────────────┐
├── Track B: 1.2 Observe→Improve ──────────────┤
├── Track C: 1.3 Mesh Trust Tiers ─────────────┤
├── Track D: 1.4 SDK Stabilization ────────────┤
└── Track E: 1.5 SLO Evidence ─────────────────┘
    + Cross-cutting: C.1–C.6 (opportunistic)    │
                                                 ▼
Phase 2 (after 1.1 + 1.2 complete):
├── 2.1 Registry Completion (needs 1.4)
├── 2.2 Doctor --fix Hardening
├── 2.3 Onboarding Gate Expansion (needs 2.2)
├── 2.4 Release Gate Orchestration (needs 1.5)
└── 2.5 Trust Pack Maturation
                                                 │
                                                 ▼
Phase 3 (after Phase 2 core stable):
├── 3.1 First Vertical (needs 1.1, 1.5, 2.4)
├── 3.2 Mesh Governance (needs 1.3)
├── 3.3 Perf Certification (needs 1.1, 2.4)
└── 3.4 External Integrators (needs 1.4)
```

---

## Success Metrics

| Phase | Metric | Target |
|-------|--------|--------|
| 1 | Copilot E2E flow works in compose | User submits task, gets result with audit trail |
| 1 | Observe→improve connected | `nexo improve --from-observation` targets pattern-affected files |
| 1 | Mesh trust enforcement | Routing blocks untrusted peers under `trusted-only` policy |
| 2 | Release bundle covers all gates | `ci release-bundle --profile full` runs ≥6 gate steps |
| 2 | Doctor fixes common issues | ≥3 failure categories have automated remediation |
| 3 | First vertical deployed | Release manager dashboard shows live readiness status |
| 3 | External integration validated | ≥2 reference integrations pass CI on current SDK |
