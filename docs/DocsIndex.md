# Nexo Documentation Index

Use this page as the navigation starting point for docs.

## Start Here

1. `docs/GettingStarted.md` — install, first commands, first pipeline, and first trust checks.
2. `README.md` — two-lane quickstart (container-first and native setup).
3. `docs/OneClickInstall.md` — one-shot installer wrappers for Linux/macOS/Windows.
4. `scripts/install/container-bootstrap.sh` and `scripts/install/container-bootstrap.ps1` — one-shot container bootstrap (Docker + image pull + smoke run).
5. `scripts/setup/setup.sh` and `scripts/setup/setup.ps1` — cross-platform dependency bootstrap + restore helpers.
6. `scripts/start-nexo-api-dev.ps1` / `scripts/start-nexo-api-dev.sh` — Docker Ollama + host `Nexo.API` dev stack (see `docs/Configuration.md` → Ollama).
7. `docs/Architecture.md` — layered architecture and component boundaries.

## Operator / Production Readiness

- `docs/ProductionReadinessGate-v1.md` — production gate commands and expected assertions.
- `.github/workflows/environment-setup-gate-v1.yml` — environment bootstrap + dependency setup gate (Linux/macOS/Windows).
- `.github/workflows/compose-gate.yml` — validates `docker-compose.test.yml` and `docker-compose.ephemeral.yml` lanes.
- `.github/workflows/onboarding-quickstart-gate.yml` — runs first-run onboarding commands in native + container lanes.
- `.github/workflows/container-image-gate.yml` — container image buildability and smoke-run gate.
- `.github/workflows/container-image-publish.yml` — publish official GHCR CLI image (latest + sha tags).
- `.github/workflows/onboarding-docs-guard.yml` — prevent startup-doc regressions in quick-start commands.
- `docs/ReleaseCandidateChecklist-v1.md` — release candidate sign-off checklist.
- `docs/Testing.md` — test guard rails, timeout policy, and workflow guidance.

## Demo / Rollout Walkthroughs

- `scripts/oh-shit-demo.sh` — high-signal end-to-end demo script (bootstrap, chat, orchestration, dogfood).
- `scripts/unity-sidecar-demo.sh` — Unity sidecar demo entrypoint.
- `docs/UnitySidecarDemo.md` — sidecar demo behavior and commands.

## Security / Trust

- `docs/TrustAndInformationArchitecture.md` — sanitization, audit, access boundaries.
- `docs/TailscaleAndNexo.md` — Tailscale + Nexo exposure profile, ACL guidance, advisory endpoint.
- `docs/config/security-exposure.env.example` — `Nexo__Security__*` env template for operators.
- `docs/Configuration.md` — environment variables and configuration reference.
- `docs/AgentSandboxArchitecture.md` — project-scoped sandbox model for agent file/tool execution and host-app integration (Unity-friendly).

## API / SDK / Runtime

- `docs/api/index.md` — API docs index.
- `docs/sdk.md` — SDK integration guidance.
- `docs/samples/StableSdkHostSample/Program.cs` — reference host integration that only uses stable SDK extension points.
- `docs/runtime/ExecutionRouting.md` — NCR-based generation routing (local, peer network, RunPod), preferences, and resilience behavior.
- `docs/runtime/specs/README.md` — runtime spec documents.
- `docs/runtime/benchmarks/README.md` — runtime benchmark goals and notes.
- `apps/runtime-studio/README.md` — **hub** for the Runtime Studio agent-set JSON, CLI vs API-hosted background agents, and how the Director portal fits; anchor [How this fits](../apps/runtime-studio/README.md#how-runtime-studio-fits-with-nexo-api).
- `docs/SelfHostedGameServerPortal.md` — `docker-compose.portal.yml`: Director portal + dailies API (lighter stack).
- `docs/SelfHostedAgentServer.md` — `docker-compose.agent-server.yml`: mounted workspace + env template `docs/config/agent-server.env.example`.
- `docs/Phase1SecureCopilotWalkthrough.md` — first-success secure copilot MVP walkthrough using `docker-compose.agent-server.yml`.

## Planning & Roadmap

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
