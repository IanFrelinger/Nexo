## Summary

<!-- Brief description of what this PR does -->

## Changes

<!-- Bulleted list of specific changes -->

-

## Testing

<!-- How were these changes tested? Include commands, screenshots, or links to CI runs -->

-

### Testing strategy (blast radius)

See [Testing strategy pivot v1](docs/architecture/TestingStrategyPivot-v1.md).

| Change type | Minimum proof (check what applies) |
|-------------|-------------------------------------|
| `Ashlar.Core.Domain` | `dotnet test src/Ashlar.Tests.Domain` · `make kernel-coverage-gate` |
| Infrastructure adapter (small / branchy) | Focused unit or gap test in touched file · coverage gate |
| Hosting / API / routing / barriers / `AddAshlar` | `make test-prod-style` and/or `make application-gate-tier-c` |
| Docker / mesh / fleet / trust | `make mesh-lab-e2e` or relevant `*-gate` tier (see pivot doc) |
| Megaclass (`ProviderFactory`, Docker provisioners, …) | **ProdStyle / virtual host** — do not add new `*GapCoverageTests` files |

- [ ] `make kernel-coverage-gate` (if touching `src/Ashlar.Core.*` or `src/Ashlar.Infrastructure`)
- [ ] `make kernel-gate` (if touching kernel hosting / pipeline / profiles)
- [ ] `make test-prod-style` (if touching production DI / API / routing)

## Checklist

- [ ] `make test` passes locally
- [ ] Documentation updated (if applicable)
- [ ] No `TODO` or `NotImplementedException` left unresolved
- [ ] Breaking changes are documented

## Release (only when this PR ships a **versioned** NuGet/GHCR release)

- [ ] Not a versioned release — skip
- [ ] **Preflight:** `dotnet run --project application/src/Ashlar.CLI -- release preflight <semver>` (or `make release-preflight VERSION=<semver>`)
- [ ] **Track:** open a **Release checklist** issue (GitHub → New issue → *Release checklist* form) or link an existing release issue
- [ ] **After merge:** tag `v<semver>` and push (runs `release.yml`) — see `docs/RELEASE_RUNBOOK.md`
