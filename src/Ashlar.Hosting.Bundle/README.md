# Ashlar.Hosting.Bundle

This **metapackage** references the full set of `Ashlar.*` packages required to embed **Ashlar.Hosting** at one version.

**Consumer:** add a single package reference:

```xml
<PackageReference Include="Ashlar.Hosting.Bundle" Version="1.2.3" />
```

**Publisher:** run `scripts/pack-ashlar-hosting-graph.sh 1.2.3 <outdir>` first (so all dependency packages exist on your feed), then pack this project with the same `PackageVersion` and restore from that feed.

See `docs/PUBLISHING.md`.
