# Application layer

Deployable and product-facing projects that consume the runtime under `src/`.

| Project | Role |
|---------|------|
| `Nexo.API` | Open HTTP host (kernel status, mesh worker, etc.) |
| `Nexo.CLI` | `nexo` global tool |

Forge HTTP (`/api/forge/*`) is served by **`Nexo.Commercial.GameDirector.Host`** in `commercial/`, not `Nexo.API`.

Tests: `Nexo.Tests.CLI`. (Commercial game-domain projects and their tests live under `commercial/` and are not part of `Nexo.Application.sln`; see LICENSING.md.)

Build:

```bash
dotnet build application/Nexo.Application.sln
```

Run CLI:

```bash
dotnet run --project application/src/Nexo.CLI -- --help
```

See `docs/architecture/runtime-vs-application.md`.
