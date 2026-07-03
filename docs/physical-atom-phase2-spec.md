# Physical-Atom Certificate — Phase 2 Formal Spec

**Maturity:** `MaturityLevel.Prototype` for all artifacts in this phase.

**Depends on:** Phase 0 (certificate + verifier), Phase 1 (bundle + resolution).

## Purpose

Encode certified atom **references** into compact QR and NFC NDEF payloads — pure byte/text codecs with no rendering, no device I/O, no network.

## Tag reference (`PhysicalAtomTagReference`)

| Field | Size | Notes |
|-------|------|-------|
| `kind` | enum | `CertRef`, `BundleRef`, `AtomOnly` |
| `atomId` | UUID | Physical atom identifier |
| `assetHash` | 32 bytes | Raw SHA-256 (not hex) |
| `assetVersion` | UTF-8 | Max 64 bytes |
| `issuerFingerprint` | 8 bytes | First 8 bytes of SHA-256(issuer public key) |

Tags carry **references**, not full certificates (NFC ~180 byte limit).

## Binary payload v1 (`PhysicalAtomTagBinaryCodec`)

```
| version (1) | kind (1) | atomId (16) | assetHash (32) | verLen (1) | assetVersion | issuerFp (8) | crc32 (4) |
```

CRC32 (IEEE) over all bytes except the trailing CRC field. Integrity check only — cryptographic trust remains in Phase 0 Ed25519 verification.

## QR encoding (`PhysicalAtomQrTagCodec`)

```
nexo-atom:v1:<base64url(binary-payload)>
```

## NFC encoding (`PhysicalAtomNfcNdefCodec`)

Simplified NDEF **external type** short record:

- Type: `nexo:atom`
- Payload: binary v1 payload above

Headless byte layout suitable for NTAG213-class tags (~127 byte payload fits).

## Issuance (`PhysicalAtomTagIssuingBrick`)

Deterministic encoder from `PhysicalAtomCertificate` or `PhysicalAtomCertBundle` → QR string + NDEF bytes.

## Out of scope (Phase 2)

QR image rasterization, NFC writer hardware, HTTP resolution backend, XR clients, release channels.

## Implementation

- Codecs: `src/Nexo.Certification.Physical/Tagging/`
- Issuer: `src/Nexo.Infrastructure/Certification/Physical/PhysicalAtomTagIssuingBrick.cs`
- Sample: `samples/physical-atom-cert/design-scope.tag-qr.txt`
