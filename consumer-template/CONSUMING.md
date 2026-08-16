# Consuming Nexo packages (external product template)

Starter pins for **nexo-ai-director**-style repos: authored brick + thin host + HTTP client. Copy `nuget.config` and `Directory.Packages.props` from this folder into your solution root.

**Package version:** `0.1.0` — must match a published release on your staging (or production) feed. In the Nexo repo, `VERSION` at the root is the single source of truth.

**Not yet on nuget.org.** No Nexo package (including `Nexo.CLI`, `Nexo.Authoring`, `Nexo.Hosting.Bundle`) has been published to nuget.org yet, so these pins only restore from a feed you supply: a staging feed you have pushed to (below), or a local folder feed packed from a checkout (`scripts/verify-external-product-shape.sh` builds one; `docs/AuthoringBricks.md`, section "Restoring Nexo.Authoring", has the hand recipe). Inside a checkout, a `ProjectReference` into `src/` (as in `samples/hello-brick/`) needs no feed at all.

## Package pins (`0.1.0`)

| Package | Role |
|---------|------|
| `Nexo.Brick.Contracts` | Authoring surface for code bricks (`Brick`, `BrickInput`, wire DTOs) |
| `Nexo.Authoring` | `AddNexoBrick<T>()` registration helpers |
| `Nexo.Hosting.Bundle` | Thin host: embed Nexo kernel + HTTP API (`AddNexo`, hosting graph) |
| `Nexo.Sdk` | Engine/client DI (`AddNexoClientSdk`) over HTTP |
| `Nexo.Client` | `INexoClient` (transitive via Sdk; pin explicitly if you reference it directly) |

### `PackageReference` form (without central package management)

```xml
<ItemGroup>
  <PackageReference Include="Nexo.Brick.Contracts" Version="0.1.0" />
  <PackageReference Include="Nexo.Authoring" Version="0.1.0" />
  <PackageReference Include="Nexo.Hosting.Bundle" Version="0.1.0" />
  <PackageReference Include="Nexo.Sdk" Version="0.1.0" />
  <PackageReference Include="Nexo.Client" Version="0.1.0" />
</ItemGroup>
```

With **central package management**, use `Directory.Packages.props` in this folder instead.

## Feed and token

1. Edit `nuget.config`: set `nexo-staging` to your feed URL (see `docs/StagingFeed.md`).
2. For private feeds, uncomment `packageSourceCredentials` in `nuget.config` and supply a read PAT via environment or local (untracked) config — **never commit tokens**.
3. Restore with `dotnet restore --configfile nuget.config`.

## Layout (typical PoP)

- **Brick project** — references `Nexo.Authoring` (+ `Nexo.Brick.Contracts` types); scaffold with `nexo new brick --nexo-version <version>` from a CLI tool-installed at the same version (`dotnet tool install --tool-path <dir> Nexo.CLI --version <version> --add-source <feed>`; the CLI is not on nuget.org yet either, so `--add-source` must point at your feed).
- **Host** — `Nexo.Authoring` + `Nexo.Hosting.Bundle`; register bricks with `AddNexoBrick<T>()` before `AddNexo()`; expose `GET /health` and `POST /api/bricks/{id}/execute` (see `scripts/verify-external-product-shape.sh` for a minimal reference).
- **Client** — `Nexo.Sdk`; call `INexoClient.InvokeAsync(HttpMethod.Post, "api/bricks/{id}/execute", …)` against the host base URL.

## Verification in Nexo

These pins are the same set exercised by:

- `scripts/verify-external-product-shape.sh` (local pack feed)
- `scripts/verify-external-product-shape-published.sh` (published staging feed)
- `scripts/consumer-surface-packages.txt` (machine-readable list)

After publishing to staging: `make verify-staging VERSION=0.1.0` with `NUGET_STAGING_READ_TOKEN` set.
