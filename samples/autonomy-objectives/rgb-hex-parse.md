---
id: rgb-hex-parse
title: Parse a 6-digit hexadecimal RGB colour into its channels and a normalized form
status: pending
source: Human
priority: 40
tags:
  - dogfood
  - parsing
touch:
  pathPrefixes:
    - applications/Ashlar.Samples.Dogfood/Colours/
  namespaces:
    - Ashlar.Samples.Dogfood.Colours
  capabilities:
    - repo.fs.write
---

Provide a deterministic brick that parses a 6-digit hexadecimal RGB colour, with or without a
leading `#`, into its three channels and a normalized lowercase form.

The brick is class `RgbHexParseBrick` in namespace `Ashlar.Samples.Dogfood.Colours`, with
`Id = "rgb-hex-parse"`.

Contract:

- Input `hex` (string): the text to parse.
- Output `isValid` (bool): true only when the text is exactly six hexadecimal digits
  (`0-9`, `a-f`, `A-F`), optionally preceded by a single `#`, and nothing else.
- Output `red` (int), `green` (int), `blue` (int): the channels 0–255; all 0 when invalid.
- Output `normalized` (string): `#` followed by the six digits in lowercase when valid; the
  EMPTY STRING when invalid. NEVER null.

Three-digit shorthand (`#abc`), whitespace, and eight-digit RGBA are all invalid.

Skeleton (fill in `ExecuteAsync`; do not add, remove, or reorder members):

```csharp
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Samples.Dogfood.Colours;

public sealed class RgbHexParseBrick : DomainBrick
{
    public RgbHexParseBrick()
    {
        Id = "rgb-hex-parse";
        Name = "RGB Hex Parse";
        Description = "Parses a 6-digit hexadecimal RGB colour into its channels and a normalized form.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("hex", "string", "colour text")],
            Outputs =
            [
                new BrickOutputDefinition("isValid", "bool", "valid"),
                new BrickOutputDefinition("red", "int", "red"),
                new BrickOutputDefinition("green", "int", "green"),
                new BrickOutputDefinition("blue", "int", "blue"),
                new BrickOutputDefinition("normalized", "string", "normalized")
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

Read the input with `input.Get<string>("hex", string.Empty) ?? string.Empty` (missing or null becomes the empty string).
Write outputs with `output.Set(name, value)` on a `new BrickOutput()` — channels as `int`, the
flag as `bool`, `normalized` as `string` — and return `Task.FromResult(output)`.

Deterministic only: no clock, no randomness, no I/O.
