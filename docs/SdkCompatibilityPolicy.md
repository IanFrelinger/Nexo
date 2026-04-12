# SDK compatibility policy

This document describes how Nexo classifies NuGet packages and public APIs, and how breaking changes are handled.

## Versioning

Stable public APIs in the packages listed below follow [Semantic Versioning 2.0.0](https://semver.org/). A MAJOR bump indicates breaking changes to those APIs. MINOR adds backward-compatible functionality. PATCH is for backward-compatible fixes.

## Package tiers

### Stable

These packages are intended for external integration. Breaking changes require a MAJOR version bump and follow the process below.

- `Nexo.Sdk`
- `Nexo.Client`
- `Nexo.Brick.Contracts`

### Experimental

APIs or entire packages may be marked with the .NET [`Experimental`](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute) attribute. They may change or be removed without a MAJOR version bump. Consumers should treat them as preview-only.

### Internal

All other assemblies and packages are internal to the Nexo repository and tooling. They are not covered by this compatibility promise unless explicitly promoted to a stable package.

## Breaking change process

1. Prefer additive changes (new types, new optional parameters, new overloads) over modifying existing contracts.
2. For stable packages, deprecate first: use `[Obsolete]` with a clear message and migration path when possible; remove or change behavior only in the next MAJOR version.
3. Document notable changes in release notes and, when applicable, in migration notes for integrators.
4. Experimental APIs may change in MINOR or PATCH releases; announce significant shifts in release notes when practical.
