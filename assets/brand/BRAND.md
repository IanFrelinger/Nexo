# ashlar brand kit

Palette: ink `#2B2420` · cream `#F7F2E5` · oat `#EFE8D8` · sage `#7E8F6E` · gold `#D1A23C` · clay `#C96F4A` · olive `#4A5540`

Type: Baloo 2 ExtraBold (wordmark) · Caveat SemiBold (annotations). Both embedded as subsets inside the SVGs — no font install needed to render them.

## Where each file goes

| File | Destination |
|------|-------------|
| `ashlar-logo-chaos.svg` | README hero — **wired**, centred `<img>` at the top of `README.md` |
| `ashlar-icon-nuget-128.png` | NuGet package icon — **wired** repo-wide in `Directory.Build.props` (see below) |
| `ashlar-icon-github-512.png` | GitHub org/repo avatar — Settings → upload |
| `ashlar-social-card-1280x640.png` | Repo → Settings → General → Social preview (100 KB, under the 1 MB limit) — also used as OG image for landing page |
| `ashlar-terminal-preview.svg` | docs/marketing use — mock CLI session; **used** in landing page bento grid |
| `marketing/ashlar-og-commercial-1280x720.png` | *Experimental, not used* — AI-generated collage style, kept for reference only |
| `marketing/ashlar-landing-hero-1600x900.png` | *Experimental, not used* — AI-generated hero reference, kept for reference only |
| `marketing/ashlar-linkedin-launch-1080.png` | *Experimental, not used* — AI-generated LinkedIn asset, kept for reference only |
| `*.svg` sources | keep in `assets/brand/` as the editable masters |
| `AshlarConsole.cs` | reference implementation, kept HERE not in the CLI: `application/**` changes need the application integration-branch flow. Wiring it into `Ashlar.CLI` is an open follow-up; see `docs/ashlar-terminal-style.md` |

**Note:** The landing page (`site/index.html`) uses only official brand SVGs and follows a clean, modern developer-tool aesthetic (Linear/Vercel/Notion-adjacent). AI-generated collage imagery (seals, tape, doodles, sparkles) is not used on the primary marketing site.

## NuGet icon wiring — already done

Wired repo-wide in `Directory.Build.props`; every packable project picks it up, no per-project
change needed:

```xml
<PackageIcon Condition="Exists('$(MSBuildThisFileDirectory)assets/brand/ashlar-icon-nuget-128.png')">icon.png</PackageIcon>
...
<ItemGroup Condition="'$(IsPackable)' == 'true' AND Exists('$(MSBuildThisFileDirectory)assets/brand/ashlar-icon-nuget-128.png')">
  <None Include="$(MSBuildThisFileDirectory)assets/brand/ashlar-icon-nuget-128.png" Pack="true" PackagePath="icon.png" Visible="false" />
</ItemGroup>
```

The asset is packed straight from `assets/brand/` rather than copied to a root `icon.png`, so
there is one copy of the bytes. Both parts are required: `PackageIcon` alone (without the packed
`None`) fails packing with NU5046, exactly as `PackageReadmeFile` alone fails with NU5039.

## Signature rules

- The node-"o" (bullseye + gold scribble ring) is the mark. Don't outline it, recolor it, or separate the dot from the ring.
- Gold means certified — in the logo, in the terminal, everywhere. Never use it decoratively.
- The doodles (tape, sparkles, `certified!` notes) are collage garnish: fine on hero/social surfaces, never on the icon-only marks beyond what's already there.
