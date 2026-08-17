# Hello Brick sample

This is the complete code-brick reference sample used by [`docs/AuthoringBricks.md`](../../docs/AuthoringBricks.md), and the **primary** way to build a code brick today: it references `src/Nexo.Core.Domain` by `ProjectReference`, so it needs no NuGet feed (Nexo packages are not yet published to nuget.org).

Prerequisites: a repository checkout and the .NET SDK (`global.json` pins the version). Run it from the repository root:

```bash
dotnet test samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj
```

Expected: the test project builds `HelloBrick` and its `Nexo.Core.Domain` dependency, then the xUnit smoke test in `HelloBrickTests.cs` passes (`ExecuteAsync` returns `Hello, <name>!` in the `message` output).

The sample contains:

- `HelloBrick/HelloBrick.csproj` — code-authored brick project (`ProjectReference` to `../../../src/Nexo.Core.Domain/Nexo.Core.Domain.csproj`).
- `HelloBrick/HelloBrick.cs` — `public sealed class HelloBrick : DomainBrick` (`DomainBrick` is a `global using` alias for `Nexo.Core.Domain.Bricks.Brick`, supplied by `samples/Directory.Build.props`).
- `HelloBrick.Tests/HelloBrick.Tests.csproj` — xUnit test project.
- `HelloBrick.Tests/HelloBrickTests.cs` — smoke test for `ExecuteAsync`.

To scaffold a standalone brick outside the checkout instead, see the `nexo new brick` and "Restoring Nexo.Authoring" sections of `docs/AuthoringBricks.md`.
