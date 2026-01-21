# GeoTerrain → Nexo Pattern Mapping

This maps GeoTerrain’s needs to **existing Nexo patterns** (tools, ports/adapters, bricks, commands, orchestrators).

## GeoTerrain capabilities

| Capability | Description |
|---|---|
| Parse elevation files | Load SRTM HGT, GeoTIFF, ASCII Grid |
| Download elevation data | Fetch from SRTM, Mapbox Terrain-RGB, OpenElevation |
| Generate mesh | Convert heightmap to 3D mesh (grid + normals; later LOD/decimation) |
| Validate quality | Check mesh accuracy against source, triangle quality metrics |
| Export mesh | Write OBJ, FBX, glTF (start with OBJ) |

## Mapping table

| GeoTerrain Need | Nexo Pattern | Location (existing) | GeoTerrain placement | Notes |
|---|---|---|---|---|
| Parse local elevation files | **Domain logic + Tool wrapper** | `Nexo.Tools.Dev/RepoFsWriteTool.cs` shows tool I/O shape | `Nexo.GeoTerrain` (domain parse), later `Nexo.Tools.GeoTerrain` | Parsing should be pure domain; file I/O belongs in Tools/Infrastructure. |
| Download elevation data | **Port + adapter implementations** | Asset pattern: `Nexo.Orchestration/Assets/Ports/*` + `Nexo.Adapters.Assets/*` | `Nexo.GeoTerrain.Orchestration/Ports/IElevationProvider` + `Nexo.Adapters.GeoTerrain.*` | Mirrors “multiple providers + DI switching + echo/offline adapter”. |
| Generate mesh | **Brick (dual-mode)** | `Nexo.Core.Domain.Bricks/*` + `Nexo.Infrastructure.Execution/BehaviorExecutor.cs` | `Nexo.GeoTerrain` defines brick contracts/models; later `Nexo.Demo.Bricks`-style bricks | Deterministic: grid triangulation + normals. Agentic: choose parameters (vertical scale, decimation %, LOD). |
| Validate quality | **Post-validator + analysis-style rules** | `Nexo.Core.Application/Orchestration/GenericCommandOrchestrator.cs` and analysis rules pattern | `Nexo.GeoTerrain.Application` post validators (later) | Start deterministic validators: bounds sanity, NaN checks, max slope, triangle quality. |
| Export mesh | **Tool or pure domain serializer** | Tools return `ToolResult` + delta | Domain: `ObjSerializer` producing string/bytes; Tool: write file | Keep “format writing” pure; keep filesystem writes in tool. |
| Orchestrate end-to-end pipeline | **Command + Orchestrator + Behavior (steps)** | `ICommand<TIn,TOut>` + `GenericCommandOrchestrator` + `BehaviorExecutor` | `Nexo.GeoTerrain.Application` (later) | Likely: `GenerateTerrainMeshCommand` orchestrates parse → mesh → validate → export. |
| CLI integration | **CLI command handlers** | `src/Nexo.CLI/Program.cs` registers command handlers | `Nexo.CLI/Commands/GeoTerrainCommand` (later) | Expose `nexo geoterrain generate ...` with `--airgap` and `--provider`. |

## Proposed structure (hybrid is correct)

GeoTerrain should follow the **Hybrid** model used elsewhere:

- **Domain**: pure models + algorithms (no I/O, no HttpClient)
- **Tools**: atomic operations that touch filesystem/interop
- **Adapters**: online providers + caching (behind ports)
- **Agentic orchestration**: optional agent that tunes settings / suggests pipeline

Proposed eventual layout:

```
src/Nexo.GeoTerrain/                  # Domain-only (first PR)
src/Nexo.Tools.GeoTerrain/            # Atomic ops (parse-from-file, export-to-file)
src/Nexo.Adapters.GeoTerrain/         # Online providers + local cache
src/Nexo.Agents.GeoTerrain/           # Optional agentic tuning + orchestration
src/Nexo.GeoTerrain.Application/      # Commands, validators, orchestrations (optional)
src/Nexo.Tests.GeoTerrain/            # Unit tests for domain + (later) integration tests
```

In the **first PR**, we only add `src/Nexo.GeoTerrain/` plus tests.

