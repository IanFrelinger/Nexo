# Application layer

Deployable and product-facing projects that consume the runtime under `src/`.

| Project | Role |
|---------|------|
| `Ashlar.API` | Open HTTP host (kernel status, mesh worker, etc.) |
| `Ashlar.CLI` | `ashlar` global tool |

Forge HTTP (`/api/forge/*`) is served by **`Ashlar.Commercial.GameDirector.Host`** in `commercial/`, not `Ashlar.API`.

Tests: `Ashlar.Tests.CLI`. (Commercial game-domain projects and their tests live under `commercial/` and are not part of `Ashlar.Application.sln`; see LICENSING.md.)

Build:

```bash
dotnet build application/Ashlar.Application.sln
```

Run CLI:

```bash
dotnet run --project application/src/Ashlar.CLI -- --help
```

See `docs/architecture/runtime-vs-application.md`.
