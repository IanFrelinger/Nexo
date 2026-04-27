## Summary

<!-- Brief description of what this PR does -->

## Changes

<!-- Bulleted list of specific changes -->

-

## Testing

<!-- How were these changes tested? Include commands, screenshots, or links to CI runs -->

-

## Checklist

- [ ] `make test` passes locally
- [ ] Documentation updated (if applicable)
- [ ] No `TODO` or `NotImplementedException` left unresolved
- [ ] Breaking changes are documented

## Release (only when this PR ships a **versioned** NuGet/GHCR release)

- [ ] Not a versioned release — skip
- [ ] **Preflight:** `dotnet run --project src/Nexo.CLI -- release preflight <semver>` (or `make release-preflight VERSION=<semver>`)
- [ ] **Track:** open a **Release checklist** issue (GitHub → New issue → *Release checklist*) or link an existing release issue
- [ ] **After merge:** tag `v<semver>` and push (runs `release.yml`) — see `docs/RELEASE_RUNBOOK.md`
