# Nexo Forge

Vertical app configuration for adaptive multiplayer FPS prototyping: balancing weapons, validating maps, monitoring performance, suggesting content, and recommending macros.

## Agent set

Background agent definitions live in `config/agent_set.forge.json`. The set includes:

- **balance-analyzer** (optimizer): monitors weapon and ability kill-rate, damage-per-second, and pick-rate metrics over a sliding window; emits nerf/buff suggestions when thresholds are exceeded.
- **map-validator** (tester): validates map geometry — spawn distances, cover ratios, and sight-line lengths — against configurable limits and reports violations.
- **performance-monitor** (optimizer): samples frame-rate telemetry, detects GPU/CPU bottlenecks, and suggests LOD adjustments to maintain the target FPS.
- **content-suggester** (extender): proposes new weapons, map zones, or aesthetic packs based on current meta diversity and staleness criteria.
- **macro-recommender** (optimizer): analyses player-authored macro libraries, detects redundant macros, and recommends new macros for repeated action sequences.

This mirrors the structure of `apps/runtime-studio/config/agent_set.local.json` and `apps/release-manager/config/agent_set.release_manager.json` (roles, schedules, exfiltration policy). Tune intervals and parameters for your environment.

## Running

Point the Nexo background-agent daemon at this config file:

```bash
dotnet run --project application/src/Nexo.CLI -- background-agent daemon \
  --config apps/nexo-forge/config/agent_set.forge.json
```

Ensure the working directory is the repo root so that relative paths in `Parameters` resolve correctly.

## Agent details

### balance-analyzer

| Parameter | Default | Description |
|---|---|---|
| `MetricsWindow` | `5m` | Sliding window for aggregating kill/damage stats |
| `KillRateThreshold` | `2.0` | Kill-rate above which a nerf suggestion is emitted |
| `UsageMinimum` | `10` | Minimum sample size before analysis triggers |

Commands: `analyze_balance`, `suggest_nerf`, `suggest_buff`

### map-validator

| Parameter | Default | Description |
|---|---|---|
| `MaxSpawnDistance` | `50.0` | Max distance (units) between opposing spawn points |
| `MinCoverRatio` | `0.3` | Minimum ratio of cover nodes to open nodes |
| `SightlineMaxLength` | `120.0` | Longest acceptable unbroken sight-line |

Commands: `validate_map`, `check_spawns`, `check_sightlines`

### performance-monitor

| Parameter | Default | Description |
|---|---|---|
| `TargetFps` | `60` | Frame-rate floor before bottleneck detection triggers |
| `SampleIntervalMs` | `500` | Telemetry sampling interval in milliseconds |
| `LodDistanceThresholds` | `[25, 50, 100]` | Distance bands (units) for LOD tier transitions |

Commands: `sample_fps`, `detect_bottleneck`, `suggest_lod`

### content-suggester

| Parameter | Default | Description |
|---|---|---|
| `MaxSuggestionsPerRun` | `5` | Max content suggestions emitted per cycle |
| `DiversityWeight` | `0.7` | Weight given to meta diversity when ranking suggestions |
| `MetaFreshnessDays` | `14` | Days after which the meta is considered stale |

Commands: `suggest_weapon`, `suggest_map_zone`, `suggest_aesthetic`

### macro-recommender

| Parameter | Default | Description |
|---|---|---|
| `MinTriggerCount` | `3` | Minimum trigger occurrences before a macro is recommended |
| `SimilarityThreshold` | `0.85` | Cosine similarity above which macros are flagged as redundant |
| `MaxRecommendations` | `10` | Max macro recommendations emitted per cycle |

Commands: `analyze_macros`, `recommend_macro`, `detect_redundant`
