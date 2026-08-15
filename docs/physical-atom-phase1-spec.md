# Physical-Atom Certificate — Phase 1 Formal Spec

**Maturity:** `MaturityLevel.Prototype` for all artifacts in this phase.

**Depends on:** Phase 0 (`PhysicalAtomCertificate`, `PhysicalAtomCertificateVerifier`, `BundleCertificationBrick`).

## Purpose

Close the hosting/resolution loop headlessly: register digital-twin assets, resolve them by `(asset_hash, asset_version)` or `atom_id`, and verify certificates against resolved bytes — without XR rendering, device I/O, or live network dependencies.

## Scope (Phase 1)

| In scope | Out of scope (later phases) |
|----------|----------------------------|
| In-memory resolution store | HTTP hosting backend |
| `PhysicalAtomCertBundle` portable manifest | QR/NFC encoding |
| Resolution-aware verifier | XR client / simulator |
| `AssetBundleCertificationPipeline` | Release channel / release_class |
| JSON manifest serialize/deserialize | Asset *generation* from 3D scans |

## Resolution store (`IAssetResolutionStore`)

| Operation | Behavior |
|-----------|----------|
| `TryResolveAsset(hash, version)` | Returns hosted asset bytes + content type |
| `TryResolveCert(atomId)` | Returns registered certificate for atom |

Prototype implementation: `InMemoryAssetResolutionStore`.

## Certified bundle (`PhysicalAtomCertBundle`)

Self-contained artifact for offline verification:

| Field | Notes |
|-------|-------|
| `certificate` | Phase 0 `PhysicalAtomCertificate` |
| `assetBytes` | Raw digital-twin asset bytes |
| `contentType` | MIME type (default `application/octet-stream`) |
| `issuerPublicKey` | 32-byte Ed25519 public key |

JSON manifest: `PhysicalAtomCertBundleManifest` (`design-scope.bundle.json` sample).

## Verification

### `PhysicalAtomResolutionVerifier`

1. Resolve registered cert for `atom_id` (must match provided cert)
2. Resolve asset bytes for `(asset_hash, asset_version)`
3. Delegate to Phase 0 `PhysicalAtomCertificateVerifier`

Failure codes: `atom-unresolved`, `asset-unresolved`, `atom-cert-mismatch`, plus Phase 0 codes.

### `PhysicalAtomCertBundleVerifier`

1. Confirm bundle asset bytes hash to `certificate.asset_hash`
2. Confirm bundle issuer key matches expected public key
3. Delegate to Phase 0 verifier

Failure codes: `bundle-asset-hash-mismatch`, `bundle-issuer-key-mismatch`, plus Phase 0 codes.

## Issuance pipeline (`AssetBundleCertificationPipeline`)

Deterministic flow:

1. `BundleCertificationBrick.Issue` (Phase 0)
2. Register asset + cert in resolution store
3. Emit `PhysicalAtomCertBundle`

Refuses inconsistent binding-scope inputs before registration (same policy as Phase 0).

## Implementation

- Resolution types: `applications/Nexo.Certification.Physical/Resolution/`
- Pipeline: `src/Nexo.Infrastructure/Certification/Physical/AssetBundleCertificationPipeline.cs`
- Tests: `PhysicalAtomResolutionVerifierTests`, `AssetBundleCertificationPipelineTests`, `PhysicalAtomCertBundleManifestTests`
