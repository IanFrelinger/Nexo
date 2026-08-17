# Changelog

All notable changes to Nexo are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). Commit messages follow Conventional Commits by convention (no commitlint hook or workflow enforces it); `scripts/changelog-snippet-for-release.sh` drafts release notes from the commits since the last `v*` tag.

At release time, move the `[Unreleased]` notes under a new `[X.Y.Z] - YYYY-MM-DD` heading, bump `VERSION`, tag `vX.Y.Z`, and publish a GitHub Release.

## [Unreleased]

Initial public platform, heading toward the first tagged release (`v0.1.0`). No version has been cut yet; everything below is unreleased. Backfilled on 2026-08-16 from `git log e6682152..master` (the commit that added this file, 2026-08-13, through PR #337); PR numbers are given where a change landed as one PR. Wave 1/2 of the readiness pass are PRs #325–#337.

### Added

**Baseline platform (before 2026-08-13)**

- Kernel spine: `Core.Domain`/`Abstractions` contracts, `Core.Application` use cases and ports, orchestration (architect, agents, coordination), background agents (scheduler, RAG, observe loop), and infrastructure (provider factory, persistence, adaptation, execution routing).
- Trust path on the execution route: sanitization with PII/secret filters, policy gates, audit trails, and barrier identity.
- Execution targets: local-first (Ollama / ONNX / offline), opt-in cloud providers, and peer/mesh execution including RunPod.
- Entry surfaces: `Nexo.CLI` (`nexo`), `Nexo.API` (HTTP + portal), and embedded hosting via `AddNexo()` for NuGet consumers.
- Mesh/federation with gRPC transport and AWS ingress; four `apps/` host configurations.
- Distribution paths: NuGet packages, GHCR container images, and Docker Compose deployments.
- CI gate suite covering kernel build/test/coverage, compose, container images, dependency and layer boundaries, cross-platform tests, docs link checking, and release readiness.

**Contracts**

- Public API contract: `Microsoft.CodeAnalysis.PublicApiAnalyzers` on every stable-tier package (`Nexo.Sdk`, `Nexo.Client`, `Nexo.Brick.Contracts`, `Nexo.Authoring`, `Nexo.Hosting.Bundle`) plus `Nexo.Abstractions`; the autonomy (self-extension) surface is `[Experimental("NEXOEXP001")]`; HTTP API versioning policy in `docs/api/versioning.md`; the v0.1.0 stability promise in `docs/SdkCompatibilityPolicy.md`.

**Trust loop / certification gate**

- Analyzer fence gate runs first in the brick gate chain; manifest-derived analyzer rules are enforced semantically at the gate; trust-loop analyzer catalog `NEXO0003`–`NEXO0009`; touch-set enforcement (`NEXO0013`/`NEXO0014`); generation-side analyzer fence validator (V3); diagnostic probes on gate failure (landed through #269).
- Certification records bind the artifact content hash into the signed payload; the summary is witnessable under a reserved key; analyzer-dead mutants count as kills; new coalesce-degrade mutation operator; mutation survivors carry the edit that produced them (#326).
- Adversarial validation campaigns for the analyzer half and the session half of the trust spec (#277, #278) and for unattended Tier-0 autonomy (#285).
- Ledger corrections: the S5 `semver-parse` survivor is an equivalent mutant — a gate soundness hole, not a weak witness (#325).

**Attested execution sessions**

- Sandboxed session runner with deadline-label reaper and TestKit fake; proposer confinement (one declaration derives tool allowlist and mounts); session attestation, certificate environment inputs, and provenance events (landed through #269).
- In-session candidate build (P3) and in-session witness/determinism/mutation execution (P5) — untrusted candidate code never runs in the harness process (#306).

**Autonomy loop (experimental, hold mode by default)**

- Objective intake (source trust, touch-sets, tier classifier), generation depth and recursion ceilings, judge growth and capability narrowing, swap-host tier gate, generation retention, no-build rollback, revocation chains, watch window with auto-rollback and quarantine, global pause / cadence floor / in-flight blocking / digest, ledger scan feeding the digest, host composition for the loop (landed through #269).
- Proposal-iteration harness (#290); LIVE model proposals at flight time with witness-blind prompts and committed recordings (#307).
- Standing loop over the objective store, hold-by-default (#312); tracked worked example under `samples/autonomy-objectives/` (#314).
- Repair channel: policy-projected, bounded repair feedback measured through the shipped path (#316); compile failures enter the repair channel; build repairs carry the whole objective; operator preamble with the brick API (#323).
- Dogfood campaigns 1 and 2 — five human-authored objectives, live in-loop proposer, then the same objectives across three proposer models (`-Models`, per-model dials) (#323, #324).
- Self-extend invariant D — cross-cycle extension ceilings; the last self-extend audit GAP closes (#321).
- `spikes/autonomy-first-flight/` — the flight runner (`-SessionBuild`, `-SessionExecute`, `-Proposed`, `-Live`, `-Sweep`, `-Models`) and its recordings; `spikes/README.md` documents every spike and which ledger rows cite it.

**Protocols**

- MCP server bridge over `ITool` with allowlists, argument overrides, policy gate, concurrency ceiling, audit, and a stdio host (#268); MCP client tool adapters over external MCP servers; A2A server core + client transport with scheme-dispatched remotes (#266); `Nexo.API` wiring of `/api/mcp` and `/api/a2a/{agentId}` behind auth and rate limits (#269) with ProdStyle coverage and a CI gate (#270); compose stacks set both explicitly off (#289).

**Repository shape, docs, and brand**

- `applications/` layer for open products built on the core (physical-atom certification, provenance graph, spatial); `Nexo.Kernel.sln` lists the spine only; the dependency-boundary gate forbids core → `applications/` references (#310).
- Compose stacks live under `deploy/compose/` (#262).
- Brand kit under `assets/brand/` with terminal style guide and console reference (#308); NuGet icon and README hero wired (#309).
- Community health files, `CHANGELOG.md`, README badges, and the branching policy in `CONTRIBUTING.md` (#261).
- Onboarding docs guard: backtick-quoted repo paths in README/docs/scripts/Makefile must exist; the meta-gate and hub deleted in `0bcc2718` restored and every dead reference fixed (#332).
- Authoring docs say what is not yet published (nothing on nuget.org), make `samples/hello-brick` the primary path, document the local feed, and pack the CLI on release (#333).
- `docs/TesterQuickstart.md` — one lane from clone to an audited job to the certification gate; README front door rewritten around auditable workflows / certified artifacts / your infrastructure, with a trust-loop section; `docs/DocsIndex.md` restructured; stale status docs marked historical.

### Changed

- net8.0 executables and test hosts roll forward onto the 9.x runtime (`RollForward=Major` in `Directory.Build.targets`), so an SDK-9-only machine runs the CLI/API without a separate .NET 8 runtime (#327).
- One Ollama resolution order across `NEXO_OLLAMA_*`, `Nexo:Meai:*` / `Nexo:NodeCapabilityRuntime:Ollama:*`, and legacy `OLLAMA_*` (#330).
- Compose defaults: repo mounts default to `../..` (the repo root) so `deploy/compose/` stacks see the repository; quickstart/Ollama/Neo4j publish on loopback; Neo4j password comes from the environment, not the file (#331).
- Docker images select the target framework with `-f`; `TARGETFRAMEWORK` no longer leaks into the MSBuild environment.
- Dependency bumps: `Microsoft.Extensions.*` 10.0.11 and MEAI 10.9.0 (#263), AWS Bedrock SDKs and CsCheck (#322), AWSSDK.DynamoDBv2 4.0.103.1 (#299), coverlet.collector 10.0.1 (#301), Avalonia (#295), `actions/checkout` v7 (#240), `actions/setup-dotnet` v6 (#242), `lycheeverse/lychee-action` 2.9.0 (#241).

### Fixed

- `nexo trust` is registered on the root command again (dropped in #162) (#328).
- `Nexo.API` defaults `RequireExplicitBarrier=false` and surfaces `BARRIER_CONTEXT_MISSING` instead of a silent `0 agent(s) executed`; exception text stays redacted outside Development in the orchestration summary (#329).
- Full Platform Readiness Gate green on `master`: skipped is not failed in `nexo validate`, per-project target framework, templates and hidden trees skipped, cwd sink flake (#317); prebuilt CLI in the E2E smoke and the extender armed in the daemon-claim smoke (#318); NCR routing flip waited on instead of a fixed delay (#319); API image smoke probes `/health` instead of hanging 90 minutes (#320); the four remaining identifiable flakes fixed at the root, air-gapped refusal kept in the MCP/A2A gate filter (#335).
- `onboarding-quickstart-gate` YAML indentation (red since July) (#311); CLI built before Ops Tier E, mesh-lab typo, egress out of the coverage badge (#334).
- Pack graph: `Certification.Physical` dropped from the hosting pack graph and the `Hosting.Bundle` metapackage after the application split (#313); `Nexo.Analyzers` packed as a runtime dependency of Infrastructure.
- Kernel-coverage-gate timeout hang nets sized so the gate is deterministic (#260).
- Flight runner: ASCII-only strings for PowerShell 5.1, comma-joined `-Models` token split, `\uXXXX` unescaped when extracting model proposals (#323, #324).

### Security

- Remote container-execution routes (`/api/execution/*`) are unmapped unless `Nexo__Execution__ServeRemoteExecution=true` and are behind auth; the API refuses to start on `Lan`/`Tailnet`/`Public` exposure without built-in auth (escape hatch `Nexo__Security__AllowUnauthenticatedNetworkExposure=true`); no shipped dev keys (#336).
- Session containers run with `--cap-drop ALL`, `--security-opt no-new-privileges`, `--pull never`, an image digest pin, and a read-only rootfs with declared scratch paths; applied containment is attested on the certificate (#337).
- MCP/A2A surfaces: `Enabled=false` defaults, empty allowlists, `ValidateOnStart`, hard refusal under `NEXO_DEPLOYMENT_PROFILE=airgapped`, all-verbs auth and per-IP rate limits on `/api/mcp` and `/api/a2a/*` (#268, #266, #269).
