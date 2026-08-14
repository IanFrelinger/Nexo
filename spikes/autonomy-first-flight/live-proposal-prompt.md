You are proposing a C# brick implementation for a certification gate. Output ONLY one C# code block and nothing else — no explanation before or after.

Objective: provide a deterministic error-count brick for a log-scanning fixture.

Contract, exactly:
- Namespace must be `Nexo.Spikes.FirstFlight`.
- One public sealed class named `LiveProposedLogScannerBrick` inheriting `DomainBrick`.
- The constructor must set `Id = "first-flight-log-scanner"`, a `Name`, a `Description`, and this exact interface:

```
Interface = new BrickInterface
{
    Inputs = [new BrickInputDefinition("logText", "string", "log")],
    Outputs =
    [
        new BrickOutputDefinition("errorCount", "int", "count"),
        new BrickOutputDefinition("firstErrorMessage", "string", "first")
    ]
};
```

- Override this method exactly:

```
public override Task<BrickOutput> ExecuteAsync(
    BrickInput input,
    ImplementationType implementation,
    IExecutionContext context,
    CancellationToken cancellationToken = default)
```

Behavior of ExecuteAsync:
1. Read the input: `var logText = input.Get<string>("logText") ?? string.Empty;`
2. Split the text into lines on '\r' and '\n', ignoring empty lines.
3. `errorCount` = the number of lines that contain the marker `ERROR` (ordinal comparison).
4. `firstErrorMessage` = for the FIRST line containing the marker: the substring AFTER the first occurrence of `ERROR`, with leading ':' and ' ' characters trimmed from its start. If no line contains the marker, the empty string.
5. Build the output:

```
var output = new BrickOutput { Summary = $"Found {errorCount} ERROR line(s); first: {firstErrorMessage}" };
output.Set("errorCount", errorCount);
output.Set("firstErrorMessage", firstErrorMessage);
return Task.FromResult(output);
```

Hard constraints (the gate rejects violations):
- File starts with exactly these two usings, then the namespace:
  `using Nexo.Core.Domain.Bricks;` and `using Nexo.Core.Domain.Execution;`
  then `namespace Nexo.Spikes.FirstFlight;`
- Deterministic only: no DateTime.Now, no Random, no Guid.NewGuid, no file or network I/O, no static mutable state, no try/catch with an empty catch.
- The marker comparison must handle the marker appearing at the very start of the text, at the start of any line, and immediately before the end of a line.
- Every loop must provably terminate.

Output the complete file as one ```csharp code block.
