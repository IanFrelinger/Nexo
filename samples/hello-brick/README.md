# Hello Brick sample

This is the complete code-brick reference sample used by [`docs/AuthoringBricks.md`](../../docs/AuthoringBricks.md), and the **smallest brick the certification gate admits**. It has the documented package-only shape: one project, one source file, one `PackageReference` to `Ashlar.Brick.Contracts` (on nuget.org at `0.1.1`), and a witness beside the source. No `ProjectReference` into `src/`, because the gate's dependency leg refuses any `ProjectReference` and allows exactly two packages (`Ashlar.Brick.Contracts`, `Ashlar.Authoring`; `src/Ashlar.Infrastructure/Certification/BrickDependencyChecker.cs`). See [`docs/CertificationGate.md`](../../docs/CertificationGate.md).

Prerequisites: a repository checkout and the .NET SDK (`global.json` pins the version). Restore needs nuget.org for the one package. Run the smoke test from the repository root:

```bash
dotnet test samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj
```

Expected: the test project builds `HelloBrick`, then the xUnit smoke test in `HelloBrickTests.cs` passes (`ExecuteAsync` returns `Hello, <name>!` in the `message` output).

## Certifying it

Certification is what this sample exists to show. From the repository root:

```bash
dotnet run --project tools/Ashlar.ExportCertifiedBrick/ExportCertifiedBrick.csproj -- \
  /tmp/hello-brick-record.json samples/hello-brick/HelloBrick
```

Expected: `Wrote content-bound record to /tmp/hello-brick-record.json` and a `contentHash=` line, exit code `0`. The tool replays `HelloBrick/hello-brick.witness.json` (found by its `*.witness.json` name; pass a path as a third argument to use another), runs the analyzer, mutation, determinism and dependency legs, and writes a signed record bound to the SHA-256 of `HelloBrick.cs`. Exit codes: `2` = the gate rejected the brick, `4` = refused before the gate ran (the message names the fix), `3` = the written record failed to re-verify.

The cert-gate suite drives the same loader and gate against this directory as checked in (`src/Ashlar.Tests.Infrastructure/Tests/Certification/ShippedSampleCertificationTests.cs`), so a change that stops this sample certifying fails `bash scripts/run-cert-gate.sh`.

The sample contains:

- `HelloBrick/HelloBrick.csproj` — code-authored brick project: `PackageReference` to `Ashlar.Brick.Contracts 0.1.1`, versions pinned in the project (`ManagePackageVersionsCentrally=false`, the shape a brick has outside the checkout). No `CopyLocalLockFileAssemblies`: since `0.1.2` the gate reads the brick's references from the compiler's own record of the build, not from the output folder. (The `Ashlar.Infrastructure 0.1.1` package on nuget.org still reads the output folder, so this sample REJECTS under a `0.1.1` host; `docs/CertificationGate.md` opens with the details.)
- `HelloBrick/HelloBrick.cs` — `public sealed class HelloBrick : Brick` (`Ashlar.Core.Domain.Bricks.Brick`, from the package). The whole brick is this one file; the certificate binds one content hash over one text.
- `HelloBrick/hello-brick.witness.json` — the two witness cases the gate replays, covering both outputs (`message`, `implementation`) so every mutant the mutation leg derives from `ExecuteAsync` is observable.
- `HelloBrick.Tests/HelloBrick.Tests.csproj` — xUnit test project.
- `HelloBrick.Tests/HelloBrickTests.cs` — smoke test for `ExecuteAsync`.

If your own brick's namespace starts with `Ashlar.`, the short name `Brick` resolves to the `Ashlar.Brick` namespace the contracts package also ships; write `Ashlar.Core.Domain.Bricks.Brick` in full, as `samples/certified-brick-reuse/Ashlar.Certified.DamageResolver/` does.

To scaffold a standalone brick outside the checkout instead, see the `ashlar new brick` and "Restoring Ashlar.Authoring" sections of `docs/AuthoringBricks.md`.
