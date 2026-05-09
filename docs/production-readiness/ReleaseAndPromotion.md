# Release and promotion

## Goals

- Every production deploy is **traceable** (version, git SHA, image digest).
- Promotion is **repeatable** (same pipeline for staging and prod).
- **Rollback** is documented and tested at least once.

## Checklist

### Versioning and artifacts

- [ ] Version scheme documented (SemVer for packages, calendar or SemVer for images).
- [ ] Build embeds: git commit, build time (optional), version string in CLI/API health if applicable.
- [ ] Release notes or changelog entry per significant release (manual or automated).
- [ ] Published artifacts: image tags **and** digests recorded in release notes or deploy manifest.

### Promotion path

- [ ] Single documented path: e.g. `build → scan (optional) → push registry → deploy staging → smoke → deploy prod`.
- [ ] Staging environment mirrors prod topology (smaller scale is fine).
- [ ] Human or policy gate before prod (approval, change ticket).

### Rollback

- [ ] Previous image tag(s) or package versions kept available.
- [ ] Database or state migrations: **forward-only** with expand/contract pattern, or documented rollback migration.
- [ ] Rollback drill performed at least once and recorded.

### Secrets in release

- [ ] CI uses short-lived credentials (OIDC to cloud) where possible.
- [ ] No long-lived tokens in release YAML committed to the repo.

## Fill in (org-specific)

| Item | Your value |
| ---- | ---------- |
| Container registry | |
| Staging namespace / stack name | |
| Production namespace / stack name | |
| Release owner / on-call | |
