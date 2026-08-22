---
id: semver-parse
title: Parse a semantic version string into its numeric parts and prerelease tag
status: pending
source: Human
priority: 30
tags:
  - dogfood
  - parsing
touch:
  pathPrefixes:
    - applications/Ashlar.Samples.Dogfood/Versions/
  namespaces:
    - Ashlar.Samples.Dogfood.Versions
  capabilities:
    - repo.fs.write
---

Provide a deterministic brick that parses a semantic version string of the form
`MAJOR.MINOR.PATCH` with an optional `-PRERELEASE` suffix, and reports its parts.

The brick is class `SemverParseBrick` in namespace `Ashlar.Samples.Dogfood.Versions`, with
`Id = "semver-parse"`.

Contract:

- Input `version` (string): the text to parse.
- Output `isValid` (bool): true only when the whole string is a valid version as defined below.
- Output `major` (int), `minor` (int), `patch` (int): the three numeric parts; all 0 when invalid.
- Output `prerelease` (string): the text after the first `-` when present; the EMPTY STRING when
  there is no prerelease or the version is invalid. NEVER null.

Valid means, exactly:

- three numeric parts separated by single `.` characters, each one or more ASCII digits;
- no leading zeros: a part is either `0` or starts with `1`–`9` (so `01` is invalid);
- optionally a single `-` followed by one or more dot-separated identifiers, each one or more
  characters from `0-9`, `A-Z`, `a-z`, `-` (an empty identifier, as in `1.0.0-` or `1.0.0-a..b`,
  is invalid);
- nothing else: no leading `v`, no whitespace, no build metadata (`+…` is invalid).

Skeleton (fill in `ExecuteAsync`; do not add, remove, or reorder members):

```csharp
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Samples.Dogfood.Versions;

public sealed class SemverParseBrick : DomainBrick
{
    public SemverParseBrick()
    {
        Id = "semver-parse";
        Name = "Semver Parse";
        Description = "Parses a semantic version string into its numeric parts and prerelease tag.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("version", "string", "version text")],
            Outputs =
            [
                new BrickOutputDefinition("isValid", "bool", "valid"),
                new BrickOutputDefinition("major", "int", "major"),
                new BrickOutputDefinition("minor", "int", "minor"),
                new BrickOutputDefinition("patch", "int", "patch"),
                new BrickOutputDefinition("prerelease", "string", "prerelease")
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

Read the input with `input.Get<string>("version", string.Empty) ?? string.Empty` (missing or
null becomes the empty string). Write outputs with `output.Set(name, value)` on a
`new BrickOutput()` — ints as `int`, the flag as `bool`, the prerelease as `string` — and
return `Task.FromResult(output)`.

Deterministic only: no clock, no randomness, no I/O, no regular expressions with timeouts.
