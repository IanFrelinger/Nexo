# Hello Brick sample

This is the complete code-brick reference sample used by [`docs/AuthoringBricks.md`](../../docs/AuthoringBricks.md), and the **primary** way to build a code brick from a checkout: it references `src/Ashlar.Core.Domain` by `ProjectReference`, so it needs no NuGet feed. Published packages pin `ci/published-version` (see [`docs/AuthoringBricks.md`](../../docs/AuthoringBricks.md) § Packages on nuget.org).

Prerequisites: a repository checkout and the .NET SDK (`global.json` pins the version). Run it from the repository root:

```bash
dotnet test samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj
```

Expected: the test project builds `HelloBrick` and its `Ashlar.Core.Domain` dependency, then the xUnit smoke test in `HelloBrickTests.cs` passes (`ExecuteAsync` returns `Hello, <name>!` in the `message` output).

The sample contains:

- `HelloBrick/HelloBrick.csproj` — code-authored brick project (`ProjectReference` to `../../../src/Ashlar.Core.Domain/Ashlar.Core.Domain.csproj`).
- `HelloBrick/HelloBrick.cs` — `public sealed class HelloBrick : DomainBrick` (`DomainBrick` is a `global using` alias for `Ashlar.Core.Domain.Bricks.Brick`, supplied by `samples/Directory.Build.props`).
- `HelloBrick.Tests/HelloBrick.Tests.csproj` — xUnit test project.
- `HelloBrick.Tests/HelloBrickTests.cs` — smoke test for `ExecuteAsync`.

To scaffold a standalone brick outside the checkout instead, see the `ashlar new brick` and "Restoring Ashlar.Authoring" sections of `docs/AuthoringBricks.md`.
