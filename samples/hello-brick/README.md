# Hello Brick sample

This is the complete code-brick reference sample used by [`docs/AuthoringBricks.md`](../../docs/AuthoringBricks.md).

Run it from the repository root:

```bash
dotnet test samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj
```

The sample contains:

- `HelloBrick/HelloBrick.csproj` — code-authored brick project.
- `HelloBrick/HelloBrick.cs` — `public sealed class HelloBrick : Brick`.
- `HelloBrick.Tests/HelloBrick.Tests.csproj` — test project.
- `HelloBrick.Tests/HelloBrickTests.cs` — smoke test for `ExecuteAsync`.
# Code Brick Template

This template is used by `nexo new brick <Name>`.

Template tokens:

- `Hello`
- `Hello Brick`
- `hello`
- `HelloBrick`
- `../../../src/Nexo.Core.Domain/Nexo.Core.Domain.csproj`

The generated project contains a code-authored `Brick` and a matching xUnit test project.
