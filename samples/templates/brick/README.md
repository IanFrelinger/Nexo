# Code Brick Template

This template is used by `ashlar new brick <Name>`.

Template tokens:

- `__BrickName__`
- `__DisplayName__`
- `__BrickId__`
- `__Namespace__`
- `__AshlarVersion__`

The generated project contains a code-authored `Brick` and a matching xUnit test project. The brick project references the `Ashlar.Authoring` package (`__AshlarVersion__`); it does not rely on a Ashlar repository checkout.

`Ashlar.Authoring` is **not yet published to nuget.org**, so `dotnet restore` fails with `NU1101` unless the package is available from a feed you supply. Either restore from a local folder feed that contains the packed `Ashlar.*` packages at the same version (`dotnet restore --source <feed> --source https://api.nuget.org/v3/index.json`), or replace the `PackageReference` with a `ProjectReference` into a Ashlar checkout. Both recipes are in `docs/AuthoringBricks.md`, section "Restoring Ashlar.Authoring".
