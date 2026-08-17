# Certified brick reuse (Project A certifies, Project B trusts)

Reference for **Phase 2: cross-project reuse** in [`docs/certification-evidence.md`](../../docs/certification-evidence.md). It shows the smallest honest shape of "ship a certified brick to another project": the consumer verifies a **content-bound, signed certification record** and runs the brick untouched — it never re-certifies, and it references **no generator and no gate**.

## What is here

| Path | Role |
|------|------|
| `Nexo.Certified.DamageResolver/DamageResolverBrick.cs` | The certified brick: `finalDamage = max(0, (isCrit ? baseDamage * critMultiplierPercent / 100 : baseDamage) - armor)`. |
| `Nexo.Certified.DamageResolver/Nexo.Certified.DamageResolver.csproj` | Packable artifact (`PackageId` `Nexo.Certified.DamageResolver`, `0.1.0`); packs `certification-record.json` under `content/certified-brick/`. References only `Nexo.Brick.Contracts`. |
| `Nexo.Certified.DamageResolver/damage-resolver.witness.json` | The six witness cases the certification gate replays (e.g. `50 / 100% / armor 10 / no crit -> 40`). Not packed. |
| `Nexo.Certified.DamageResolver/certification-record.json` | The signed record Project A produced: `contentHash` = SHA-256 of the brick source, mutation summary (`totalMutants: 7`, `survivingMutants: 0`), HMAC `signature`. This is the sidecar Project B verifies. |
| `ProjectB/ProjectB.csproj` | The consumer. `PackageReference`s only `Nexo.Brick.Contracts`, `Nexo.Authoring`, `Nexo.Certification.Contracts` and the packed `Nexo.Certified.DamageResolver` (all `0.1.0`). |
| `ProjectB/Program.cs` | `CertificationTrustVerifier.Verify(record, source)`; on TRUSTED runs the brick with `baseDamage 50, crit 100%, armor 10, no crit` and prints `TRUSTED finalDamage=40`. Exit `2` (`UNTRUSTED: <code>`) on signature or content-hash failure. |

Trust model (v0): same-owner reuse via a shared dev HMAC key (`NEXO_CERT_DEV_HMAC_KEY`, default dev key when unset). Cross-organization trust would need PKI and is out of scope.

## How it is exercised (CI)

The **cert-gate** workflow (`.github/workflows/cert-gate.yml` -> `scripts/run-cert-gate.sh`) runs `src/Nexo.Tests.Infrastructure/Tests/Certification/CrossProjectReuseTests.cs`, which certifies the damage resolver in-process, then plays Project B against the result:

| Test | Proves |
|------|--------|
| `HonestCertifiedBrick_ProjectB_TrustsAndRunsUntouched` | `ProjectB.csproj` has no gate/generator references; verifier says TRUSTED; brick returns `finalDamage == 40`. |
| `TamperedBrick_ProjectB_RejectsContentHashMismatch` | Changing `Math.Max(0, raw - armor)` -> `content-hash-mismatch`. |
| `ForgedSignature_ProjectB_Rejects` | Altered signature -> `signature-invalid`. |

Reproduce locally from the repository root (Docker not required):

```bash
bash scripts/run-cert-gate.sh
```

## Running Project B by hand (local feed)

Prerequisites: .NET SDK per `global.json`; nothing here is on nuget.org, so the `0.1.0` packages come from a **local folder feed** that `scripts/pack-certified-brick-reuse.sh` builds at `artifacts/certified-brick-feed` (override with `NEXO_CERTIFIED_REUSE_FEED`; version with `NEXO_CERTIFIED_REUSE_VERSION`). The script packs `Nexo.Brick.Contracts`, `Nexo.Authoring` and `Nexo.Certification.Contracts`, writes a `NuGet.Config` for the feed + nuget.org, re-runs certification through `tools/Nexo.ExportCertifiedBrick` (rewriting `certification-record.json`), and finally packs the certified artifact:

```bash
bash scripts/pack-certified-brick-reuse.sh
```

Then restore and run Project B against that feed, passing the brick source and the record it must verify:

```bash
dotnet restore samples/certified-brick-reuse/ProjectB/ProjectB.csproj \
  --configfile artifacts/certified-brick-feed/NuGet.Config
dotnet run --no-restore --project samples/certified-brick-reuse/ProjectB/ProjectB.csproj -- \
  samples/certified-brick-reuse/Nexo.Certified.DamageResolver/DamageResolverBrick.cs \
  samples/certified-brick-reuse/Nexo.Certified.DamageResolver/certification-record.json
```

Expected output: `TRUSTED finalDamage=40` (exit code `0`). Edit one character of `DamageResolverBrick.cs` and rerun to see `UNTRUSTED: content-hash-mismatch` (exit code `2`).

Note: `Nexo.Authoring` declares its `Nexo.Core.*` / `Nexo.Hosting` project references as package dependencies at the same version, so if restore reports `NU1101` for those, pack the hosting graph into the same feed first: `bash scripts/pack-nexo-hosting-graph.sh 0.1.0 artifacts/certified-brick-feed`. The tracked `certification-record.json` is the last export the maintainers committed; re-running the pack script re-signs it with your local key.
