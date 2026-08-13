# Changelog

All notable changes to Nexo are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). Commit messages follow Conventional Commits (enforced by commitlint); `scripts/changelog-snippet-for-release.sh` drafts release notes from the commits since the last `v*` tag.

At release time, move the `[Unreleased]` notes under a new `[X.Y.Z] - YYYY-MM-DD` heading, bump `VERSION`, tag `vX.Y.Z`, and publish a GitHub Release.

## [Unreleased]

Initial public platform, heading toward the first tagged release (`v0.1.0`).

### Added

- Kernel spine: `Core.Domain`/`Abstractions` contracts, `Core.Application` use cases and ports, orchestration (architect, agents, coordination), background agents (scheduler, RAG, observe loop), and infrastructure (provider factory, persistence, adaptation, execution routing).
- Trust path on the execution route: sanitization with PII/secret filters, policy gates, audit trails, and barrier identity.
- Execution targets: local-first (Ollama / ONNX / offline), opt-in cloud providers, and peer/mesh execution including RunPod.
- Entry surfaces: `Nexo.CLI` (`nexo`), `Nexo.API` (HTTP + portal), and embedded hosting via `AddNexo()` for NuGet consumers.
- Mesh/federation with gRPC transport and AWS ingress; four `apps/` host configurations.
- Distribution paths: NuGet packages, GHCR container images, and Docker Compose deployments.
- CI gate suite covering kernel build/test/coverage, compose, container images, dependency and layer boundaries, cross-platform tests, docs link checking, and release readiness.
