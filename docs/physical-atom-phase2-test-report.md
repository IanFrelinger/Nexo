# Physical-Atom Phase 2 — Test Report

**Sprint:** Phase 2 — QR/NFC Tag Encoding  
**Maturity:** Prototype  
**Methodology:** Rejection-first; codec witness independent of issuance tests.

## Rejection coverage (`PhysicalAtomTagCodecTests`)

| ID | Case | Expected failure | Status |
|----|------|------------------|--------|
| R1 | Invalid QR prefix | `tag-prefix-invalid` | PASS |
| R2 | Corrupted base64url | `tag-payload-malformed` | PASS |
| R3 | Truncated binary payload | `tag-payload-truncated` | PASS |
| R4 | Tampered CRC32 | `tag-integrity-mismatch` | PASS |
| R5 | Unsupported version byte | `tag-version-unsupported` | PASS |
| R6 | NFC NDEF type mismatch | `ndef-type-mismatch` | PASS |

## Happy-path coverage

| ID | Case | Status |
|----|------|--------|
| A1 | QR encode/decode round-trip | PASS |
| A2 | NFC NDEF encode/decode round-trip | PASS |
| A3 | Issue tags from certified bundle | PASS |
| A4 | Sample QR decodes (`design-scope.tag-qr.txt`) | PASS |

## Issuance coverage (`PhysicalAtomTagIssuingTests`)

| Case | Status |
|------|--------|
| Bundle → QR + NFC tags | PASS |
| Missing issuer key refused | PASS |

## Phase 2 gate

All malformed tag payloads rejected; valid references round-trip through QR and NFC codecs. **11/11 Phase 2 tests** in cert-gate (88 total).
