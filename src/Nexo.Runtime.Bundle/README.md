# Nexo.Runtime.Bundle

Metapackage that references the **Nexo execution kernel** libraries at one version.

## What is included

See `docs/architecture/runtime-vs-application.md` in this repository for the boundary definition.

## What is not included

- **`Nexo.Hosting`** — composition root (`AddNexo`). Reference it separately when you want the stock DI graph, or register services yourself in your application repo.
- **`Nexo.API`**, **`Nexo.CLI`**, **`Nexo.GameDomain`** — product / application surfaces stay out of this bundle by design.

## Related

- **`Nexo.Hosting.Bundle`** — includes **`Nexo.Hosting`** for full stock composition from NuGet.
