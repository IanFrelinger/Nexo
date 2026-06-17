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
- `Nexo.Authoring`
- `Nexo.Hosting.Bundle` (metapackage: references the `Nexo.Hosting` graph at a single version)

#### Code-brick authoring surface

The `nexo new brick` code-brick path references `Nexo.Authoring` and exposes the following authoring types as a stable contract. Their namespaces are preserved for source compatibility with existing consumers; their implementation is hosted in the stable brick contract assembly.

- `Nexo.Core.Domain.Bricks.Brick`
- `Nexo.Core.Domain.Bricks.BrickCategory`
- `Nexo.Core.Domain.Bricks.BrickInterface`
- `Nexo.Core.Domain.Bricks.BrickInputDefinition`
- `Nexo.Core.Domain.Bricks.BrickOutputDefinition`
- `Nexo.Core.Domain.Execution.BrickInput`
- `Nexo.Core.Domain.Execution.BrickOutput`
- `Nexo.Core.Domain.Bricks.ImplementationType`
- `Nexo.Core.Domain.Execution.IExecutionContext`

### Experimental

APIs or entire packages may be marked with the .NET [`Experimental`](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute) attribute. They may change or be removed without a MAJOR version bump. Consumers should treat them as preview-only.

### Internal

All other assemblies and packages are internal to the Nexo repository and tooling. They are not covered by this compatibility promise unless explicitly promoted to a stable package.

## Breaking change process

1. Prefer additive changes (new types, new optional parameters, new overloads) over modifying existing contracts.
2. For stable packages, deprecate first: use `[Obsolete]` with a clear message and migration path when possible; remove or change behavior only in the next MAJOR version.
3. Document notable changes in release notes and, when applicable, in migration notes for integrators.
4. Experimental APIs may change in MINOR or PATCH releases; announce significant shifts in release notes when practical.
