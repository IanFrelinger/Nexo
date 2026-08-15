# nexo brand kit

Palette: ink `#2B2420` · cream `#F7F2E5` · oat `#EFE8D8` · sage `#7E8F6E` · gold `#D1A23C` · clay `#C96F4A` · olive `#4A5540`

Type: Baloo 2 ExtraBold (wordmark) · Caveat SemiBold (annotations). Both embedded as subsets inside the SVGs — no font install needed to render them.

## Where each file goes

| File | Destination |
|------|-------------|
| `nexo-logo-chaos.svg` | README hero: `![nexo](assets/brand/nexo-logo-chaos.svg)` |
| `nexo-icon-nuget-128.png` | NuGet package icon (see csproj snippet below) |
| `nexo-icon-github-512.png` | GitHub org/repo avatar — Settings → upload |
| `nexo-social-card-1280x640.png` | Repo → Settings → General → Social preview (100 KB, under the 1 MB limit) |
| `nexo-terminal-preview.svg` | docs/marketing use — mock CLI session |
| `*.svg` sources | keep in `assets/brand/` as the editable masters |
| `src/NexoConsole.cs` | your CLI project; see `docs/nexo-terminal-style.md` |

## NuGet icon wiring

```xml
<PropertyGroup>
  <PackageIcon>icon.png</PackageIcon>
</PropertyGroup>
<ItemGroup>
  <None Include="assets/brand/nexo-icon-nuget-128.png" Pack="true" PackagePath="icon.png" />
</ItemGroup>
```

## Signature rules

- The node-"o" (bullseye + gold scribble ring) is the mark. Don't outline it, recolor it, or separate the dot from the ring.
- Gold means certified — in the logo, in the terminal, everywhere. Never use it decoratively.
- The doodles (tape, sparkles, `certified!` notes) are collage garnish: fine on hero/social surfaces, never on the icon-only marks beyond what's already there.
