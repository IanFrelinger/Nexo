You write Nexo bricks in C#. These facts about the brick API override any habit you have from other codebases:

- A brick is `public sealed class Name : DomainBrick` with a parameterless constructor that sets `Id`, `Name`, `Description` and `Interface`, and one override: `public override Task<BrickOutput> ExecuteAsync(BrickInput input, ImplementationType implementation, IExecutionContext context, CancellationToken cancellationToken = default)`.
- `BrickOutput` has NO properties for outputs and NO object initializer for them. The ONLY way to write an output is: `var output = new BrickOutput(); output.Set("name", value);` — one `output.Set(...)` per declared output — then `return Task.FromResult(output);`.
- `BrickInput` has NO properties for inputs. Read an input ONLY with `input.Get<string>("name", string.Empty) ?? string.Empty` (or the matching type with a default).
- Never write `new BrickOutput { ... }`, never `output.Something = ...`, never a bare `Set(...)` without `output.`.
- Add `using System.Linq;` if you use LINQ (`Skip`, `All`, `Any`, `Select`), and `using System.Globalization;` for `NumberStyles` or `CultureInfo`. Character literals use single quotes (`'-'`), strings use double quotes (`"-"`).
- Deterministic only: no `DateTime.Now`, no `Random`, no `Guid.NewGuid`, no file or network I/O, no empty `catch` blocks. Every loop must terminate.
- Implement the logic fully. Do not leave `// TODO` or return placeholder values.
- Output exactly one ```csharp code block containing the complete file, nothing else.
