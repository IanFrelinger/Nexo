# Nexo.Hosting.Bundle

This **metapackage** references the full set of `Nexo.*` packages required to embed **Nexo.Hosting** at one version.

**Consumer:** add a single package reference:

```xml
<PackageReference Include="Nexo.Hosting.Bundle" Version="1.2.3" />
```

**Publisher:** run `scripts/pack-nexo-hosting-graph.sh 1.2.3 <outdir>` first (so all dependency packages exist on your feed), then pack this project with the same `PackageVersion` and restore from that feed.

See `docs/PUBLISHING.md`.
