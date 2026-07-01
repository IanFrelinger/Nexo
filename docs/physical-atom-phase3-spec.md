# Physical-Atom Certificate — Phase 3 Formal Spec

**Maturity:** `MaturityLevel.Prototype` for all artifacts in this phase.

**Depends on:** Phase 0 (verifier), Phase 1 (resolution store), Phase 2 (tag codecs).

## Purpose

Wire the tower end-to-end headlessly: HTTP-style resolution routing and tag→verify orchestration — no live server socket, no XR client.

## Scope (Phase 3)

| In scope | Out of scope |
|----------|--------------|
| `HttpAssetResolutionRouter` (headless GET routing) | Kestrel host wiring in `Nexo.API` |
| `PhysicalAtomTagVerifyOrchestrator` | QR image rendering |
| End-to-end pipeline test (certify → tag → verify) | Asset generation from 3D scans |
| JSON cert responses | Release channel logic |

## HTTP resolution router

Headless request/response records — invoke `Handle(method, path, store)` directly in tests.

| Route | Response |
|-------|----------|
| `GET /nexo/atoms/{atomId}/cert` | `200 application/json` certificate or `404 atom-unresolved` |
| `GET /nexo/assets/{assetHash}/{assetVersion}` | `200` asset bytes or `404 asset-unresolved` |
| Other | `404 route-not-found` |
| Non-GET | `405 method-not-allowed` |

Backing store: `IAssetResolutionStore` (prototype uses `InMemoryAssetResolutionStore`).

## Tag verify orchestrator

`PhysicalAtomTagVerifyOrchestrator.VerifyQr(qr, store, issuerPublicKey)`:

1. Decode QR tag (Phase 2)
2. Verify issuer fingerprint matches public key
3. Resolve registered certificate by `atom_id`
4. Confirm tag fields match registered cert (`asset_hash`, `asset_version`)
5. Delegate to `PhysicalAtomResolutionVerifier` → Phase 0 verifier

Failure codes include: `tag-prefix-invalid`, `tag-issuer-fingerprint-mismatch`, `tag-reference-mismatch`, `atom-unresolved`, plus Phase 0/1 codes.

## Implementation

- HTTP: `src/Nexo.Certification.Physical/Resolution/Http/`
- Orchestrator: `src/Nexo.Certification.Physical/Resolution/PhysicalAtomTagVerifyOrchestrator.cs`
- Tests: `PhysicalAtomTagVerifyOrchestratorTests`, `HttpAssetResolutionRouterTests`, `PhysicalAtomEndToEndFlowTests`
