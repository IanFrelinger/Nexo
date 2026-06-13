# Versioning

Nexo uses SemVer for packages, images, and release tags.

## Version-locked artifact set

A Nexo release is a version-locked set:

- NuGet packages use the same version, for example `0.1.0`.
- GHCR images use the same tag, for example `ghcr.io/ianfrelinger/nexo-cli:0.1.0`.
- Git tags use a leading `v`, for example `v0.1.0`.

Consumers should pin exact versions or image digests in production. `latest` is only a convenience tag for smoke tests and demos.

## 0.x compatibility policy

While Nexo is in `0.x`:

- Minor versions may contain breaking changes when release notes include migration guidance.
- Patch versions must not introduce intentional breaking changes.
- Breaking changes must be listed in the GitHub Release under **Breaking changes** and **Migration notes**.

## Deprecation policy

Starting no later than `1.0.0`, public APIs that are being removed should be marked `[Obsolete]` for at least one minor release before removal whenever practical.

## Release workflow

The release workflow runs on tags matching `v*.*.*`. The version published to NuGet and GHCR is the tag without the leading `v`.
