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

The brick is class `TagScanClassifierBrick` in namespace `Nexo.Certification.Physical.Scanning`,
with `Id = "tag-scan-classifier"`.

Contract:

- Input `payload` (string): the raw scanned text.
- Output `isValid` (bool): true only when the payload decodes to a tag reference.
- Output `failureCode` (string): the codec's failure code when invalid; the EMPTY STRING when valid. NEVER null.
- The output's `Summary` property (`output.Summary = ...`): exactly `valid nexo-atom tag` when valid; `invalid tag: ` followed by the failure code when invalid.

Use `PhysicalAtomQrTagCodec.TryDecode` rather than re-implementing prefix or base64url
handling — the point is to surface the existing decision, not to duplicate it. Its shape is
`static bool TryDecode(string qrPayload, out PhysicalAtomTagReference? reference, out string? failureCode, out string? reason)`
in namespace `Nexo.Certification.Physical.Tagging`; on success `failureCode` is null.

Skeleton (fill in `ExecuteAsync`; do not add, remove, or reorder members):

```csharp
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Certification.Physical.Tagging;

namespace Nexo.Certification.Physical.Scanning;

public sealed class TagScanClassifierBrick : DomainBrick
{
    public TagScanClassifierBrick()
    {
        Id = "tag-scan-classifier";
        Name = "Tag Scan Classifier";
        Description = "Classifies a scanned payload as a valid nexo-atom QR tag or a named failure.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("payload", "string", "scan")],
            Outputs =
            [
                new BrickOutputDefinition("isValid", "bool", "valid"),
                new BrickOutputDefinition("failureCode", "string", "failure")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO
    }
}
```

Read the input with `input.Get<string>("payload", string.Empty) ?? string.Empty`. Write outputs
with `output.Set(name, value)` on a `new BrickOutput()`, set `output.Summary` as the contract
says, and return `Task.FromResult(output)`.

Deterministic only: no clock, no randomness, no I/O.
