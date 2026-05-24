# Nexo Documentation Index

Documentation index for the Nexo platform. Start here to find what you need.

## Start Here

1. `README.md` — **container-first** quickstart (Dev Container, quickstart image, GHCR CLI, compose); native paths are documented as escape hatches.
2. `docs/RELEASE.md` — **shipping** NuGet + GHCR (one hub linking runbook, publishing, variables, staging, signing).
3. `.devcontainer/devcontainer.json` — default development environment (Cursor / VS Code).
4. `docs/GettingStarted.md` — first commands, first pipeline, and first trust checks (aligned with container + CLI).
5. `docs/DistributionModels.md` — how to **consume and ship** Nexo (NuGet, HTTP, CLI, compose, mesh) and the **distribution-matrix** CI workflow.
6. `CONTRIBUTING.md` — recommended **Dev Container** workflow and PR checks.
7. `scripts/install/container-bootstrap.sh` and `scripts/install/container-bootstrap.ps1` — one-shot container bootstrap (Docker + image pull + smoke run).
8. `scripts/setup/setup.sh` (forwards to `setup-unix.sh` on macOS/Linux), `scripts/setup/setup-unix.sh`, and `scripts/setup/setup.ps1` — cross-platform **native** dependency bootstrap + restore helpers (CI and escape hatch).
9. `docs/SetupMatrixVerification.md` + `scripts/setup/verify-setup-matrix.ps1` / `verify-setup-matrix.sh` — brute-force style setup combination checks (local + CI).
10. `scripts/start-nexo-api-dev.ps1` / `scripts/start-nexo-api-dev.sh` — Docker Ollama + host `Nexo.API` dev stack (see `docs/Configuration.md` → Ollama).
11. `docs/Architecture.md` — layered architecture and component boundaries.

## Operator / Production Readiness

- **`docs/DistributionModels.md`** — how Nexo is **distributed** (NuGet, HTTP, CLI, compose, source, mesh), **pinning**, and the **distribution matrix** CI workflow that gates each channel.
- **`docs/production-readiness/README.md`** — **hub** for supporting SMB, enterprise, SaaS, and air-gapped production: release, security, ops, data/compliance, reliability, testing, operator deployment; includes [catalog by deployment type](production-readiness/CatalogByDeploymentType.md) and [runbook template](production-readiness/RunbookTemplate.md).
- `docs/DEPLOYMENT.md` — **golden paths** (portal stack, CLI image, agent server), **pinning** images vs `latest`, NuGet/CI notes.
- `docs/RELEASE.md` — **release hub** (preflight, dispatch, tag, deep links).
- `docs/RELEASE_RUNBOOK.md` — release **decision table** (tag vs NuGet-only vs branch images); **`scripts/release-preflight-local.sh`** / **`make release-preflight`** / **`dotnet run … release preflight`** for one-command local preflight.
- `docs/GitHubRepoVariables.md` — **Actions variables** for NuGet publish mode, post-push verify, SBOM, cross-verify.
- `docs/GitHubBranchProtection.md` — **branch protection** guidance (merge gates vs tag releases).
- `.github/ISSUE_TEMPLATE/release_checklist.yml` — **GitHub issue form** for a release ticket.
- `docs/StagingFeed.md` — optional **NuGet staging** push before nuget.org.
- `docs/NuGetPackageSigning.md` — optional **package signing** notes.
- `docs/PUBLISHING.md` — NuGet pack/push, **post-push verification** (registration API, SHA-256 match, SBOM/Grype vars), operator checklist.
- `.github/workflows/pack-hosting-graph-alignment.yml` — pack script vs `Nexo.Hosting` MSBuild graph.
- `docs/ProductionReadinessGate-v1.md` — production gate commands and expected assertions (binary PASS/FAIL technical gate).
- `.github/workflows/production-readiness-gate-v1.yml` — automated production readiness gate.
- `.github/workflows/environment-setup-gate-v1.yml` — environment bootstrap + dependency setup gate (Linux/macOS/Windows).
- `.github/workflows/compose-gate.yml` — validates `docker-compose.test.yml` and `docker-compose.ephemeral.yml` lanes.
- `.github/workflows/devcontainer-gate.yml` — validates `.devcontainer/post-create.sh` restore + `Nexo.CLI` build inside the dev image.
- `.github/workflows/setup-smoke-suite.yml` — **parallel** dev container + compose `config` + native Ubuntu `setup.sh`; run manually before target-hardware iteration (`docs/CiFirstHardwareSecond.md`).
- `docs/CiFirstHardwareSecond.md` — **CI first, hardware second**: which workflows to run and what still needs a physical host.
- `.github/workflows/onboarding-quickstart-gate.yml` — runs first-run onboarding commands in native + container lanes.
- `.github/workflows/container-image-gate.yml` — container image buildability and smoke-run gate.
- `.github/workflows/distribution-matrix-gate.yml` — **parallel** gates: NuGet local-pack consumer, CLI image + subcommand help smoke, API image + `curl` `/health` + `/api/status`, `Nexo.Client` in-process test, pack-graph alignment (plus **weekly** schedule).
- `.github/workflows/release.yml` — **one entry**: tag `v*.*.*` → GHCR (`nexo-cli`, `nexo-api`) + NuGet; run summary with pin lines.
- `.github/workflows/container-image-publish.yml` — GHCR on **main** path-filtered pushes + manual (tags use `release.yml` only).
- `.github/workflows/release-nuget.yml` — **NuGet-only** manual dispatch; after push to nuget.org, **Verify NuGet consumer** (same reusable job as **release.yml**).
- `.github/workflows/docs-link-check.yml` — **lychee** link validation on **`README.md`** + **`docs/**/*.md`** (loopback URLs ignored via **`.lycheeignore`**).
- `.github/workflows/onboarding-docs-guard.yml` — prevent startup-doc regressions in quick-start commands.
- `.github/workflows/cross-platform-tests.yml` — cross-platform tests on Ubuntu, macOS, and Windows.
- `.github/workflows/runtime-release-gate.yml` — runtime release quality gate.
- `.github/workflows/runtime-release-promotion.yml` — runtime release promotion workflow.
- `.github/workflows/installer-bruteforce-gate.yml` — installer robustness gate.
- `.github/workflows/perf-certification.yml` — performance certification workflow.
- `.github/workflows/workflow-regression-gate.yml` — workflow regression gate.
- `.github/workflows/test-trust-multi-env.yml` — trust tests across multiple Docker environments.
- `.github/workflows/test-caching-multi-env.yml` — caching tests across multiple Docker environments.
- `.github/workflows/test-persistence-multi-os.yml` — persistence tests across multiple OS targets.
- `.github/workflows/test-air-gapped-no-network.yml` — air-gapped validation with zero network egress.
- `docs/ReleaseCandidateChecklist-v1.md` — release candidate sign-off checklist.
- `docs/Testing.md` — test guard rails, timeout policy, and workflow guidance.

## Demo / Rollout Walkthroughs

- `scripts/oh-shit-demo.sh` — high-signal end-to-end demo script (bootstrap, chat, orchestration, dogfood).
- `scripts/unity-sidecar-demo.sh` — Unity sidecar demo entrypoint.
- `docs/UnitySidecarDemo.md` — sidecar demo behavior and commands.

## Security / Trust

- `docs/FriendMeshPrefab.md` — prefab Docker Compose + env template for a small shared **Nexo.API** hub (friends / tailnet).
- `docs/MeshPhase8OperatorHardening.md` — **Mesh Phase 8:** discovery admission, trust alias, `nexo mesh hub` / `mesh director`, TLS example.
- `docs/MeshVirtualLab.md` — **Virtual mesh lab:** two Nexo.API nodes in Docker + verify script (no extra hardware); **`scripts/bootstrap-cloud-mesh-lab.sh`** for Ubuntu/Debian cloud VMs.
- `docs/MeshAgentSetupCapabilityBreakdown.md` — mesh agent setup **tear sheet**: capability tiers, ports, and ops checklist mapped to mesh DI surfaces.
- `docs/TrustAndInformationArchitecture.md` — sanitization, audit, access boundaries.
- `docs/TailscaleAndNexo.md` — Tailscale + Nexo exposure profile, ACL guidance, advisory endpoint.
- `docs/config/security-exposure.env.example` — `Nexo__Security__*` env template for operators.
- `docs/Configuration.md` — environment variables and configuration reference.
- `docs/AgentSandboxArchitecture.md` — project-scoped sandbox model for agent file/tool execution and host-app integration (Unity-friendly).

## API / SDK / Runtime

- `docs/api/index.md` — API docs index.
- `docs/sdk.md` — SDK integration guidance.
- `docs/PUBLISHING.md` — pack and publish `Nexo.Hosting` (and its `Nexo.*` graph) to NuGet / GitHub Packages; operator checklist.
- `docs/NuGetConsumerVerify.md` — validate NuGet-only consumption (local pack vs published feed); workflow **`.github/workflows/nuget-consumer-verify.yml`**.
- `docs/samples/StableSdkHostSample/Program.cs` — reference host integration that only uses stable SDK extension points.
- `docs/runtime/ExecutionRouting.md` — NCR-based generation routing (local, peer network, RunPod), preferences, and resilience behavior.
- `docs/AgentExecutionIsolation.md` — per-agent isolation tiers (in-process through container-per-agent), JSON field, and invocation metadata for transports.
- `docs/runtime/specs/README.md` — runtime spec documents.
- `docs/runtime/benchmarks/README.md` — runtime benchmark goals and notes.
- `apps/runtime-studio/README.md` — **hub** for the Runtime Studio agent-set JSON, CLI vs API-hosted background agents, and how the Director portal fits; anchor [How this fits](../apps/runtime-studio/README.md#how-runtime-studio-fits-with-nexo-api).
- `docs/SelfHostedGameServerPortal.md` — `docker-compose.portal.yml`: Director portal + dailies API (lighter stack).
- `docs/SelfHostedAgentServer.md` — `docker-compose.agent-server.yml`: mounted workspace + env template `docs/config/agent-server.env.example`.
- `docs/Phase1SecureCopilotWalkthrough.md` — first-success secure copilot MVP walkthrough using `docker-compose.agent-server.yml`.

## Planning & Roadmap

- `docs/MeshPhase0NorthStar.md` — **Phase 0 (executed):** federated mesh north star, capability matrix by profile, trust boundary, SLOs (feeds mesh Phases 1–7).
- `docs/ExecutionPlan.md` — phased execution plan with implementation tasks, dependencies, and success metrics.
- `docs/IssueBatch_30-60-90_Roadmap.md` — 30/60/90 gap-closure issue batch (issue templates).
- `docs/NorthStarGapAnalysis.md` — North Star vs codebase gap analysis with status tracking.
- `docs/GapAnalysis.md` — dogfood, observe→improve, trust, and documentation gap analysis.

## Additional Material

- `docs/Persistence.md` — persistence behavior and options.
- `docs/ComponentLibrary.md` — component catalog references.
- `docker-compose.test.yml` — containerized test lane (`test-ubuntu`) with mounted test artifacts.
- `docker-compose.ollama.yml` — Ollama-only stack (named volume for models; pair with host-run Nexo).
- `docker-compose.ephemeral.yml` — disposable local dependencies (Ollama, optional Postgres profile).
