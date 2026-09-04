# ashlar-workstation

Air-gapped IDE daemon and VS Code extension host. Consumes Ashlar via
`AddAshlarWorkstation()`, which selects `AshlarDeploymentProfile.SecureWorkstation`
and enables trust.

This tree is extractable to `github.com/IanFrelinger/ashlar-workstation`. Until
then it points at in-repo kernel projects. After extraction, replace
`ProjectReference`s with the published `Ashlar.Hosting` / `Ashlar.Contracts`
packages.

Existing in-repo surfaces this product will own after extraction:

- `extensions/ashlar-vscode/`
- IDE HTTP routes on `application/src/Ashlar.API/Endpoints/IdeEndpoints.cs`

The daemon must not `dlopen` generated native code. Native workloads go through
`INativeExecutionHost` in `ashlar-native`.
