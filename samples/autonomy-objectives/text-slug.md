---
id: text-slug
title: Turn free text into a URL slug
status: pending
source: Human
priority: 50
tags:
  - dogfood
  - text
  - under-specified-on-purpose
touch:
  pathPrefixes:
    - applications/Nexo.Samples.Dogfood/Text/
  namespaces:
    - Nexo.Samples.Dogfood.Text
  capabilities:
    - repo.fs.write
---

Provide a deterministic brick that turns free text into a URL slug.

The brick is class `TextSlugBrick` in namespace `Nexo.Samples.Dogfood.Text`, with
`Id = "text-slug"`.

Contract:

- Input `text` (string): the text to slugify.
- Output `slug` (string): the slug — lowercase; words separated by a single `-`; only letters
  and digits otherwise; no leading or trailing `-`. The EMPTY STRING when the text has no
  letters or digits. NEVER null.

Skeleton (fill in `ExecuteAsync`; do not add, remove, or reorder members):

```csharp
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Samples.Dogfood.Text;

public sealed class TextSlugBrick : DomainBrick
{
    public TextSlugBrick()
    {
        Id = "text-slug";
        Name = "Text Slug";
        Description = "Turns free text into a URL slug.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("text", "string", "text")],
            Outputs = [new BrickOutputDefinition("slug", "string", "slug")]
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

Read the input with `input.Get<string>("text", string.Empty) ?? string.Empty` (missing or null becomes the empty string).
Write the output with `output.Set("slug", value)` on a `new BrickOutput()` and return
`Task.FromResult(output)`.

Deterministic only: no clock, no randomness, no I/O.
