# Ashlar.Runtime.Bundle

Metapackage that references the **Ashlar execution kernel** libraries at one version.

## What is included

See `docs/architecture/runtime-vs-application.md` in this repository for the boundary definition.

## What is not included

- **`Ashlar.Hosting`** — composition root (`AddAshlar`). Reference it separately when you want the stock DI graph, or register services yourself in your application repo.
- **`Ashlar.API`**, **`Ashlar.CLI`**, and commercial Forge/GameDirector projects — product / application surfaces stay out of this bundle by design.

## Related

- **`Ashlar.Hosting.Bundle`** — includes **`Ashlar.Hosting`** for full stock composition from NuGet.
