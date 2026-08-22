You are proposing a C# brick implementation for a certification gate. Below is a complete file with one method body missing. Output the COMPLETE file — every line shown, byte-for-byte unchanged, including the constructor — with the `ExecuteAsync` body implemented. Output ONLY one ```csharp code block, nothing else.

```csharp
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Spikes.FirstFlight;

public sealed class LiveProposedLogScannerBrick : DomainBrick
{
    public LiveProposedLogScannerBrick()
    {
        Id = "first-flight-log-scanner";
        Name = "Live Proposed Log Scanner";
        Description = "Live model-proposed deterministic error-count brick.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("logText", "string", "log")],
            Outputs =
            [
                new BrickOutputDefinition("errorCount", "int", "count"),
                new BrickOutputDefinition("firstErrorMessage", "string", "first")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        // IMPLEMENT THIS BODY.
    }
}
```

Behavior of ExecuteAsync:
1. Read the input: `var logText = input.Get<string>("logText") ?? string.Empty;`
2. Split the text into lines on '\r' and '\n', ignoring empty lines.
3. `errorCount` = the number of lines that contain the marker `ERROR` (ordinal comparison).
4. `firstErrorMessage` = for the FIRST line containing the marker: the substring AFTER the first occurrence of `ERROR`, with leading ':' and ' ' characters trimmed from its start. If the marker ends the line, the empty string. If no line contains the marker, the empty string.
5. Build the output:

```
var output = new BrickOutput { Summary = $"Found {errorCount} ERROR line(s); first: {firstErrorMessage}" };
output.Set("errorCount", errorCount);
output.Set("firstErrorMessage", firstErrorMessage);
return Task.FromResult(output);
```

Hard constraints (the gate rejects violations):
- Deterministic only: no DateTime.Now, no Random, no Guid.NewGuid, no file or network I/O, no static mutable state, no empty catch blocks.
- Handle the marker at the very start of the text, at the start of any line, immediately followed by ':' or the end of the line.
- Every loop must provably terminate.
- Do not add, remove, or reorder any member shown in the skeleton.
