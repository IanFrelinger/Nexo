---
id: tag-scan-classifier
title: Classify a scanned tag payload as a valid nexo-atom QR tag or a named failure
status: pending
source: Human
priority: 10
tags:
  - physical-atom
  - dogfood
touch:
  pathPrefixes:
    - applications/Nexo.Certification.Physical/Scanning/
  namespaces:
    - Nexo.Certification.Physical.Scanning
  capabilities:
    - repo.fs.write
---

A scanner front-end needs to tell a user *why* a scan failed, not merely that it did.
`PhysicalAtomQrTagCodec.TryDecode` already produces that distinction internally; nothing
exposes it as a brick.

Provide a deterministic brick that takes one scanned payload string and reports whether it
is a valid nexo-atom v1 QR tag, plus the specific failure code when it is not.

Contract:

- Input `payload` (string): the raw scanned text.
- Output `isValid` (bool): true only when the payload decodes to a tag reference.
- Output `failureCode` (string): the codec's failure code when invalid; the EMPTY STRING when valid. NEVER null.

Use `PhysicalAtomQrTagCodec.TryDecode` rather than re-implementing prefix or base64url
handling — the point is to surface the existing decision, not to duplicate it.

Deterministic only: no clock, no randomness, no I/O.
