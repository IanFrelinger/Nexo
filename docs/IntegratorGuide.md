# Nexo integrator guide

This guide is for teams embedding Nexo, extending bricks, or hosting custom background agents alongside the core platform.

## Getting started with the Nexo SDK

The managed SDK entry point is the `Nexo.Sdk` project (`src/Nexo.Sdk/Nexo.Sdk.csproj`). Add a project reference from your integrator assembly:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/nexo/src/Nexo.Sdk/Nexo.Sdk.csproj" />
</ItemGroup>
```

Build and test from the repository root:

```bash
dotnet build Nexo.sln
dotnet test Nexo.sln --filter "FullyQualifiedName~YourIntegrator.Tests"
```

Use the Nexo CLI for local validation and gates:

```bash
dotnet run --project src/Nexo.CLI -- --help
dotnet run --project src/Nexo.CLI -- validate
```

For container or native install paths, see `docs/GettingStarted.md` and `docs/OnboardingAutomation.md`.

## Building a custom brick

Bricks exchange structured payloads; shared DTOs and versioning live in **Nexo.Brick.Contracts** (`src/Nexo.Brick.Contracts`). Reference that project and implement your brick against the contract types (for example `BrickMetadataDto`, `BrickExecuteRequestDto`, `BrickExecuteResponseDto`, and capability manifests under `Capabilities/`).

Recommended steps:

1. Add `ProjectReference` to `Nexo.Brick.Contracts`.
2. Define stable JSON-serializable request/response shapes aligned with the DTOs.
3. Register and exercise your brick through the host’s brick pipeline and existing OWASP sample (`Nexo.Bricks.Owasp`) as a reference implementation.
4. Run `dotnet run --project src/Nexo.CLI -- validate` before publishing.

## Building a custom background agent

Background agents are configured via JSON agent sets (see `apps/runtime-studio/config/agent_set.local.json` and `apps/release-manager/config/agent_set.release_manager.json`). Each agent specifies `Role`, `ModelProvider`, `Commands`, `Schedule`, and `ExfiltrationPolicy`.

To run a custom set locally:

```bash
dotnet run --project src/Nexo.CLI -- background-agent daemon --config path/to/your/agent_set.json
```

Match sensitivity and exfiltration settings to your deployment tier. For mesh-related peer lists used at runtime, see `nexo mesh` commands and `NEXO_MESH_INSTANCES_PATH` if you relocate `instances.json`. To call a remote mesh **director** HTTP API from a headless host (CI, bare metal worker), use **`nexo mesh director`** with **`NEXO_MESH_DIRECTOR_BASE_URL`** and optional **`NEXO_MESH_MUTATING_TOKEN`** / **`NEXO_MESH_API_KEY`** — see **`docs/MeshPhase7EdgeAlignment.md`**.

## Compatibility matrix (placeholder)

| Nexo version | .NET runtime | Notes |
|--------------|--------------|--------|
| _TBD_        | .NET 8 / 9   | Fill in after your release qualification. |

Update this table when you pin Nexo packages or CLI images for production.

## Troubleshooting

- **CLI or doctor failures after dependency changes**: Run `dotnet run --project src/Nexo.CLI -- doctor --fix --dry-run` to see planned remediation without executing commands, then `doctor --fix --yes` if appropriate.
- **Restore or build errors**: Confirm SDK versions in `global.json` and run `bash scripts/setup/setup.sh restore` from the repo root.
- **Mesh peer not visible**: Verify `instances.json` (default: `~/.nexo/instances.json` or `NEXO_MESH_INSTANCES_PATH`) and use `nexo mesh admit <peerId>` / `nexo mesh revoke <peerId>` to toggle the `admitted` flag.
- **Release gate bundle**: Use `dotnet run --project src/Nexo.CLI -- ci release-bundle --profile full` for an extended step list; reports land under `.nexo/release-bundle/last-run/`.

For onboarding CI failure categories, the quickstart gate workflow runs `scripts/onboarding/failure-taxonomy.sh` on lane logs and publishes consolidated artifacts from the categorization job.
