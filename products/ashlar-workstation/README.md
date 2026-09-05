# ashlar-workstation

Offline IDE daemon and VS Code extension host. Consumes Ashlar via
`AddAshlarWorkstation()`, which selects `AshlarDeploymentProfile.SecureWorkstation`
and enables trust. That is **not** `AshlarDeploymentProfile.AirGapped` — the slim
offline server profile strips trust, agents, RAG, and observation.

This is a composition library, not a shipped executable. Wire
`AddAshlarWorkstation()` into your daemon (or keep growing this tree here).
Setting `ASHLAR_DEPLOYMENT_PROFILE=workstation` on `Ashlar.API` selects the
module set but still needs `ASHLAR_TRUST_ENABLED=1` unless you call
`AddAshlarWorkstation()`.

This tree is extractable to `github.com/IanFrelinger/ashlar-workstation`. Until
then it points at in-repo kernel projects. After extraction, replace
`ProjectReference`s with the published `Ashlar.Hosting` / `Ashlar.Contracts`
packages.

Existing in-repo surfaces this product will own after extraction:

- `extensions/ashlar-vscode/`
- IDE HTTP routes on `application/src/Ashlar.API/Endpoints/IdeEndpoints.cs`

The daemon must not `dlopen` generated native code. Native workloads go through
`INativeExecutionHost` in `ashlar-native`.
