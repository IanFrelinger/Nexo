# Code Brick Template

This template is used by `ashlar new brick <Name>`.

Template tokens:

- `__BrickName__`
- `__DisplayName__`
- `__BrickId__`
- `__Namespace__`
- `__AshlarVersion__`

The generated project contains a code-authored `Brick` and a matching xUnit test project. The brick project references the `Ashlar.Authoring` package (`__AshlarVersion__`); it does not rely on a Ashlar repository checkout.

The scaffold certifies exactly as generated, since `0.1.2`:
`BrickCertificationProjectLoaderReferenceTests.The_brick_template_certifies_exactly_as_scaffolded`
substitutes the tokens, builds the project and runs all five gate legs over it. Two things keep that
true and are easy to lose when editing the template: every output key `ExecuteAsync` writes is
declared in `Interface.Outputs` (the analyzer leg refuses an undeclared one, `ASHLAR0002`), and the
project needs **no** `CopyLocalLockFileAssemblies` — the `0.1.2` gate reads references from the
compiler's own record of the build, not from the output folder. The `Ashlar.CLI 0.1.1` tool on
nuget.org embeds the *previous* template, which the `0.1.2` gate REJECTS; and this template REJECTS
under an `Ashlar.Infrastructure 0.1.1` host, whose loader still reads the output folder.

`Ashlar.Authoring` is **on nuget.org since `0.1.1`**, so a project scaffolded with `--ashlar-version 0.1.1` restores with no extra configuration. If you pin a version that exists only in a local folder feed, restore fails with `NU1101` until you point at it (`dotnet restore --source <feed> --source https://api.nuget.org/v3/index.json`); inside a checkout you can instead replace the `PackageReference` with a `ProjectReference`. Both recipes are in `docs/AuthoringBricks.md`, section "Restoring Ashlar.Authoring" — but note that a `ProjectReference` makes the brick **uncertifiable** (`docs/CertificationGate.md`).
