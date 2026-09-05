# Code Brick Template

This template is used by `ashlar new brick <Name>`.

Template tokens:

- `__BrickName__`
- `__DisplayName__`
- `__BrickId__`
- `__Namespace__`
- `__AshlarVersion__`

The generated project contains a code-authored `Brick` and a matching xUnit test project. The brick project references the `Ashlar.Authoring` package (`__AshlarVersion__`); it does not rely on a Ashlar repository checkout.

`Ashlar.Authoring` is on nuget.org at the pin in `ci/published-version`. Restore from nuget.org, from a local folder feed of packed `Ashlar.*` packages at the same version (`dotnet restore --source <feed> --source https://api.nuget.org/v3/index.json`), or replace the `PackageReference` with a `ProjectReference` into a Ashlar checkout. Recipes are in `docs/AuthoringBricks.md`, section "Packages on nuget.org".
