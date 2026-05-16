# Application layer

Deployable and product-facing projects that consume the runtime under `src/`.

| Project | Role |
|---------|------|
| `Nexo.API` | HTTP host |
| `Nexo.CLI` | `nexo` global tool |
| `Nexo.GameDomain` | Forge / game descriptors |

Tests: `Nexo.Tests.CLI`, `Nexo.Tests.GameDomain`.

Build:

```bash
dotnet build application/Nexo.Application.sln
```

Run CLI:

```bash
dotnet run --project application/src/Nexo.CLI -- --help
```

See `docs/architecture/runtime-vs-application.md`.
