# Code Brick Template

This template is used by `nexo new brick <Name>`.

Template tokens:

- `__BrickName__`
- `__DisplayName__`
- `__BrickId__`
- `__Namespace__`
- `__NexoVersion__`

The generated project contains a code-authored `Brick` and a matching xUnit test project. The brick project references the `Nexo.Authoring` package (`__NexoVersion__`); it does not rely on a Nexo repository checkout.

`Nexo.Authoring` is **not yet published to nuget.org**, so `dotnet restore` fails with `NU1101` unless the package is available from a feed you supply. Either restore from a local folder feed that contains the packed `Nexo.*` packages at the same version (`dotnet restore --source <feed> --source https://api.nuget.org/v3/index.json`), or replace the `PackageReference` with a `ProjectReference` into a Nexo checkout. Both recipes are in `docs/AuthoringBricks.md`, section "Restoring Nexo.Authoring".
