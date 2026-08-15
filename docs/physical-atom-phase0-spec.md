# Physical-Atom Certificate — Phase 0 Formal Spec

**Maturity:** `MaturityLevel.Prototype` for all artifacts in this phase.

## Purpose

Bind a physical atom (real-world object) to a hosted digital-twin asset via an Ed25519-signed certificate. Phase 0 delivers schema, standalone verification, and deterministic issuance — no rendering, device I/O, or network dependencies.

## Schema (`PhysicalAtomCertificate`, version 1)

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `schemaVersion` | int | yes | Must be `1` |
| `maturity` | enum | yes | `Prototype` \| `Preview` \| `Stable` |
| `atomId` | UUID | yes | Non-empty GUID |
| `bindingScope` | enum | yes | `Design` \| `Instance` \| `Batch` |
| `assetHash` | string | yes | Lowercase hex SHA-256 (64 chars) of asset bytes |
| `assetVersion` | string | yes | SemVer 2.0 |
| `geoAnchor` | object | no | When present, load-bearing for geo consistency |
| `manufactureMeta` | object | scope-dependent | See binding policy |
| `extensions` | map<string, base64-bytes> | no | Signed but uninterpreted by core verifier |
| `issuerSignature` | string | yes (issued certs) | Base64 Ed25519 signature (64 bytes) |

### `geoAnchor`

| Field | Type | Notes |
|-------|------|-------|
| `latitude` | double | [-90, 90] |
| `longitude` | double | [-180, 180] |
| `resolution` | int | H3 resolution [0, 15] |
| `h3Index` | string | 15-char hex; must match lat/lon at resolution |

### `manufactureMeta`

| Field | Type | Notes |
|-------|------|-------|
| `batchId` | string? | Required for `Batch` scope |
| `serialNumber` | string? | Required for `Instance` scope |
| `manufacturedAt` | ISO-8601? | Optional timestamp |

## Binding-scope validation policy

| Scope | `manufactureMeta` | Rule |
|-------|-------------------|------|
| **Design** | Must be **absent/null** | Populated `manufactureMeta` is an **error** (not silently ignored) |
| **Instance** | Required | Must include non-empty `serialNumber` |
| **Batch** | Required | Must include non-empty `batchId` |

When `geoAnchor` is present, `h3Index` must be consistent with `latitude`/`longitude` at `resolution` (computed via H3).

## Signing

Canonical JSON payload (camelCase, nulls omitted) over:

```
schemaVersion, maturity, atomId, bindingScope, assetHash, assetVersion,
geoAnchor, manufactureMeta, extensions (keys sorted, values base64)
```

`issuerSignature` is **excluded** from the signed payload. Algorithm: **Ed25519** (NSec/libsodium).

## Verification (`PhysicalAtomCertificateVerifier`)

Order of checks:

1. Structural / binding-scope policy (`PhysicalAtomCertificateValidationPolicy`)
2. Ed25519 signature against issuer public key
3. `assetHash` consistency vs. provided asset bytes

Failure codes (kebab-case): `signature-invalid`, `asset-hash-mismatch`, `binding-scope-manufacture-meta-required`, `binding-scope-manufacture-meta-forbidden`, `geo-anchor-inconsistent`, etc.

## Issuance (`BundleCertificationBrick`)

Deterministic brick in `Nexo.Infrastructure.Certification.Physical`. Computes `assetHash` from asset bytes, validates binding-scope policy, signs with issuer private key. Refuses inconsistent inputs before signing.

## Out of scope (Phase 0)

Asset generation, QR/NFC encoding, XR clients, hosting/resolution backend, release channel logic.

## Implementation

- Library: `applications/Nexo.Certification.Physical/`
- Issuance: `src/Nexo.Infrastructure/Certification/Physical/BundleCertificationBrick.cs`
- Tests: `src/Nexo.Tests.Infrastructure/Tests/Certification/PhysicalAtomCertificateVerifierTests.cs`, `BundleCertificationBrickTests.cs`
