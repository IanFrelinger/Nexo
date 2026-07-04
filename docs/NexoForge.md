# Nexo Forge

Nexo Forge is a vertical application built on top of the Nexo platform for **adaptive multiplayer FPS prototyping**. It provides game designers with a real-time sandbox for authoring weapons, maps, abilities, and aesthetic packs while background agents continuously analyse balance, validate geometry, and suggest content — all without leaving the forge UX.

## Architecture overview

Nexo Forge maps to Nexo's layered architecture:

```
┌──────────────────────────────────────────────────────┐
│  Forge Bar UX (CLI / future Unity editor panel)      │
├──────────────────────────────────────────────────────┤
│  Nexo.Commercial.GameDomain (Session, Macros, etc.)  │
├──────────────────────────────────────────────────────┤
│  Nexo.Commercial.GameDirector.Host (/api/forge)    │
├──────────────────────────────────────────────────────┤
│  Background Agents (balance, map, perf, content, macro)│
├──────────────────────────────────────────────────────┤
│  Nexo Core.Application / Orchestration               │
├──────────────────────────────────────────────────────┤
│  Infrastructure (providers, persistence, mesh)       │
└──────────────────────────────────────────────────────┘
```

- **Nexo.Commercial.GameDomain** — domain models (`SessionState`, `MacroDefinition`, weapon/map/ability descriptors) and exporter utilities for JSON round-tripping. Types retain the `Nexo.Commercial.GameDomain` namespace.
- **Background Agents** — the `agent_set.forge.json` configuration drives five deterministic agents that run on a schedule.
- **Core.Application / Orchestration** — MediatR use cases, the orchestrator, and the agent scheduler from the base Nexo platform.
- **Infrastructure** — LLM providers (used by the content-suggester when wired to a real model), persistence, and mesh networking.

## Game descriptor types

Game descriptors are the building blocks authored in a forge session:

| Type | Controls |
|---|---|
| `WeaponDescriptor` | Damage, fire-rate, range, magazine size, reload time, custom stats |
| `MapDescriptor` | Biome, dimensions, spawn points, cover/open node counts |
| `AbilityDescriptor` | Cooldown, duration, effect description |
| `AestheticPack` | Theme name, asset paths, LOD levels |

All descriptors are stored inside a `SessionState` and serialised via `SessionExporter`.

## Scoped settings system

Forge supports hierarchical, composable settings that cascade from global defaults down to per-descriptor overrides:

```
Global defaults
  └─ Session-level overrides
       └─ Descriptor-category overrides (e.g. all weapons)
            └─ Per-descriptor overrides (e.g. weapon "Rail Gun")
```

**Precedence**: a more-specific scope wins. Settings are stored in `SessionState.ScopedSettings` as a flat dictionary with dotted keys (e.g. `weapons.railgun.damage`). At runtime the resolver walks the key hierarchy and returns the most specific match.

**Composition**: multiple scoped settings can be merged — useful when forking a session or importing a partial config from another designer.

## Macro system

Macros automate repetitive forge actions.

### Creation

A `MacroDefinition` contains an ordered list of `MacroStep` entries, each with an `Action` name and `Arguments` dictionary. Macros can declare typed `MacroParameter` slots so they can be re-used with different inputs.

### Parameters

Each parameter has a `Type` (string, number, bool, etc.), an optional `DefaultValue`, a `Required` flag, and a human-readable `Description`.

### Triggers

The `MacroTrigger` on a definition controls when the macro fires:

| Trigger type | Field | Description |
|---|---|---|
| `manual` | — | Fired explicitly from the forge bar |
| `event` | `EventName` | Fired when a named forge event occurs |
| `schedule` | `CronExpression` | Fired on a cron schedule |
| `condition` | `Condition` | Fired when a runtime condition evaluates to true |

### Sharing

Setting `IsShared = true` publishes the macro so other designers on the same Nexo mesh can discover and import it. The `MacroExporter.ExportMany` / `ImportMany` methods handle bulk transfer.

## Aesthetic packs and dynamic LOD

An `AestheticPack` groups visual and audio assets under a named theme. Each pack declares how many LOD levels its assets support (`LodLevels`, default 3). The **performance-monitor** agent watches frame-rate telemetry and suggests LOD tier transitions using configurable distance thresholds (`LodDistanceThresholds`), keeping the prototype above the target FPS without manual tuning.

## Background agents

The forge agent set (`apps/nexo-forge/config/agent_set.forge.json`) defines five agents:

### balance-analyzer

Monitors weapon and ability statistics over a configurable sliding window. When a weapon's kill-rate exceeds `KillRateThreshold` (default 2.0) with at least `UsageMinimum` samples, the agent emits `suggest_nerf`. Under-performing weapons receive `suggest_buff`.

### map-validator

Validates map geometry against three checks:

- **Spawn distance** — opposing spawn points must be within `MaxSpawnDistance` units.
- **Cover ratio** — the ratio of cover nodes to total nodes must meet `MinCoverRatio`.
- **Sight-lines** — no unbroken sight-line may exceed `SightlineMaxLength` units.

### performance-monitor

Samples frame telemetry every `SampleIntervalMs` milliseconds. When FPS drops below `TargetFps`, the agent runs bottleneck detection and emits LOD adjustment suggestions keyed to `LodDistanceThresholds`.

### content-suggester

Proposes new weapons, map zones, or aesthetic packs when the meta becomes stale (no significant change in `MetaFreshnessDays`). Suggestions are ranked by a `DiversityWeight` factor and capped at `MaxSuggestionsPerRun`.

### macro-recommender

Scans the session's macro library for redundant entries (cosine similarity above `SimilarityThreshold`) and recommends new macros when it detects repeated action sequences with at least `MinTriggerCount` occurrences.

## Session management

### Export / Import

```csharp
// Export
string json = SessionExporter.ExportToJson(session);

// Import
SessionState restored = SessionExporter.ImportFromJson(json);
```

Round-trip safe via `System.Text.Json` with camelCase web defaults.

### Fork

Forking creates a new `SessionState` whose `ForkedFromSessionId` points to the parent. All descriptors, settings, and macros are deep-copied so the fork can diverge independently.

### Macro export

```csharp
string json = MacroExporter.ExportMany(macros);
IReadOnlyList<MacroDefinition> restored = MacroExporter.ImportMany(json);
```

## The forge bar UX

The forge bar is a persistent command palette (CLI today, Unity editor panel in the future) that exposes:

- **Quick-edit** — inline adjustment of descriptor values (e.g. `set weapon railgun damage 85`).
- **Agent status** — live readout of background agent state and last suggestion.
- **Macro runner** — fire any manual-trigger macro with parameter prompts.
- **Session controls** — save, load, fork, export.

## How to run

Start the forge background-agent daemon:

```bash
dotnet run --project application/src/Nexo.CLI -- background-agent daemon \
  --config apps/nexo-forge/config/agent_set.forge.json
```

The daemon loads the five agents defined in `agent_set.forge.json` and schedules them at their configured intervals. The CLI must be run from the repository root so relative paths resolve.

## API endpoints reference

The base Nexo API (mapped in `NexoEndpoints.cs`) exposes the endpoints below. Forge-specific workflows compose these generic endpoints with forge-domain data.

**Forge HTTP API** (`ForgeEndpoints.cs`, prefix `/api/forge`): session create/import/export, scoped settings, macros, aesthetics (`GET /aesthetics`, `POST /aesthetic/apply`, `POST /aesthetic/apply-pack`). Session and macro state use `IForgeStateService`: in-process memory by default, or LiteDB when `Nexo:ForgeSession:LiteDbPath` is configured (see `docs/Persistence.md`).

| Method | Path | Summary |
|---|---|---|
| `GET` | `/health` | Health check |
| `POST` | `/api/agent` | Invoke an agent by name |
| `POST` | `/api/validate` | Run validation tests |
| `POST` | `/api/orchestrate` | Run orchestration workflow |
| `POST` | `/api/copilot/task` | Run copilot task with trust audit |
| `GET` | `/api/copilot/tasks` | List recent copilot tasks |
| `GET` | `/api/status` | Background agent status and mode |
| `POST` | `/api/execution/build` | Build a container image |
| `POST` | `/api/execution/run` | Run a container |
| `GET` | `/api/capabilities` | Node capability manifest |
| `GET` | `/api/security/advisory` | Operator exposure profile |
| `GET` | `/api/trust/status` | Trust boundary status |
| `GET` | `/api/trust/dashboard` | Trust dashboard with audit events |
| `POST` | `/api/trust/pause` | Pause / resume trust observation |
| `POST` | `/api/trust/rule` | Update trust allow / deny rules |
| `GET` | `/api/knowledge/query` | Query knowledge timeline |
| `GET` | `/api/preferences` | Get user preferences |
| `POST` | `/api/preferences` | Save user preferences |
| `GET` | `/api/activity/feed` | Recent activity feed |
| `POST` | `/api/changelog/generate` | Generate project changelog |
| `GET` | `/api/onboarding/status` | First-run wizard status |
| `POST` | `/api/director/run` | Run one directorial iteration |
| `GET` | `/api/director/dailies` | List persisted dailies |
| `GET` | `/api/director/dailies/{dailyId}` | Get one daily entry |
| `GET` | `/api/background-agents/summary` | Background agent health summary |

Forge workflows typically use `/api/agent` to invoke forge-specific commands (e.g. `analyze_balance`) and `/api/background-agents/summary` to display agent health in the forge bar.

## Unity integration overview

Nexo Forge is designed to integrate with the Unity game engine via the **Unity sidecar** pattern (see `tools/Nexo.UnitySidecarDemo`):

1. **Sidecar process** — `dotnet run --project application/src/Nexo.CLI` runs alongside the Unity editor, exposing the API on localhost.
2. **Editor script** — a C# editor window in Unity calls the sidecar API to push/pull `SessionState` snapshots.
3. **Live preview** — weapon, map, and ability descriptors are mapped to Unity `ScriptableObject` assets. Changes made in the forge bar propagate to the editor in near-real-time.
4. **LOD control** — the performance-monitor agent's LOD suggestions are applied to Unity's `LODGroup` components via the sidecar bridge.
5. **Macro playback** — macros can script sequences of Unity editor actions (place spawn points, adjust lighting, swap materials) through the sidecar command channel.

`Nexo.Commercial.GameDomain` targets `netstandard2.0` alongside `net8.0` so it can be referenced directly from Unity 2019+ projects.
