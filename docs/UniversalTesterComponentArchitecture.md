# Universal Tester: Component-Based Architecture Investigation

This document investigates making the Universal Tester agent configurable and component-based, enabling all world-model and game-tester recommendations to be pluggable.

## Current Architecture Summary

### Configuration
| Layer | Class | Purpose |
|-------|-------|---------|
| Session | `UniversalTesterConfig` | Target, Goal, Depth, MaxDuration, WatchOnly, CaptureIntervalMs, etc. |
| Runtime | `UniversalTesterRuntimeConfig` | Per-brick impl selection (agentic/deterministic), fallback chain, MultiFrameCount |
| Context | `IExecutionContext` | Provider (ollama/openai), IsAirGapped, AuditMode |

### Hardcoded Components
- **Adapters**: `CreateAdapter(TargetType)` – switch on WebApp, Game, Api, Cli, DesktopApp
- **Bricks**: Fixed pipeline – Perception → Understanding → Exploration → Action → Validation → Reporting
- **Understanding prompts**: Hardcoded in `UnderstandingBrick.BuildVisionUnderstandingPrompt` and `BuildUnderstandingPrompt`
- **Provider selection**: Passed via `IExecutionContext.Provider`; no per-brick provider override

### What Is Already Configurable
- `UniversalTesterRuntimeConfig` loads from JSON (`--agent-spec`, `--agent-spec-json`)
- Per-brick `Prefer` and `Fallback` chain
- `MultiFrameCount` for temporal vision
- `IProviderFactory` – provider routing (ollama, video, openai, etc.) via env vars

---

## Recommendations Mapped to Configurability

### Option A: Temporal Prompting (SmolVLM2-Video)
**Goal**: Enhance UnderstandingBrick prompts when multi-frame / video provider is used.

| Need | Current | Proposed |
|------|---------|----------|
| Multi-frame context | `MultiFrameCount` in runtime config | Keep; add `UnderstandingMode: "temporal"` to enable game-specific prompt variants |
| Prompt templates | Hardcoded in UnderstandingBrick | Extract to config; support `promptTemplates.temporal`, `promptTemplates.pixelOnly` |
| Provider routing | VIDEO_SERVICE_URL → "video" provider | Already done |

**Config shape**:
```json
{
  "understanding": {
    "prefer": "agentic",
    "mode": "temporal",
    "promptTemplate": "temporal-gameplay"
  }
}
```

---

### Option B: World Model Prediction (iVideoGPT, Matrix-Game)
**Goal**: Optional brick that predicts outcomes of actions before execution.

| Need | Current | Proposed |
|------|---------|----------|
| Optional brick | Pipeline is fixed | **Brick pipeline config** – define ordered list of brick IDs; allow optional steps |
| New brick type | N/A | `WorldModelPredictionBrick` – inputs: frames, candidate action; output: predicted next frame / outcome |
| Service URL | N/A | `WORLD_MODEL_SERVICE_URL` or `predictionService` in config |
| When to run | N/A | Conditional: `"runWhen": "candidateActions.length > 0"` or always in planning phase |

**Config shape**:
```json
{
  "pipeline": [
    "perception",
    "understanding",
    "world-model-prediction",
    "exploration",
    "action",
    "validation",
    "reporting"
  ],
  "bricks": {
    "world-model-prediction": {
      "enabled": true,
      "serviceUrl": "${WORLD_MODEL_SERVICE_URL}",
      "prefer": "agentic"
    }
  }
}
```

---

### Option C: GameState-First (Unity Plugin)
**Goal**: When GameState is available, prioritize it over pixel analysis.

| Need | Current | Proposed |
|------|---------|----------|
| GameState in perception | `PerceptionBrick` already calls `adapter.GetGameStateAsync()` | ✅ Done |
| Understanding uses GameState | `BuildUnderstandingPrompt` includes `perception.GameState` when non-null | Partially – only in text prompt path, not vision path |
| Mode selection | N/A | `UnderstandingMode: "gameStateFirst"` when adapter provides GameState; auto-detect or config |

**Config shape**:
```json
{
  "understanding": {
    "mode": "gameStateFirst",
    "prioritizeStructuredState": true
  }
}
```

**Code change**: In UnderstandingBrick, when `perception.GameState != null`, inject structured state prominently into vision prompt and prefer LLM path with rich context over pure vision.

---

### Option D: Game-Specific Fine-Tuning
**Goal**: Use a fine-tuned model for a specific game.

| Need | Current | Proposed |
|------|---------|----------|
| Model override | Provider (ollama/openai) is global | **Per-brick model/config** – e.g. `understanding.model = "my-game-vlm"` |
| Ollama model | `ollama pull <model>` | `OLLAMA_MODEL` or `models.understanding` in config |
| Custom endpoint | N/A | Support `OPENAI_BASE_URL`-style overrides; or adapter-specific model in `BrickRuntimeSpec` |

**Config shape**:
```json
{
  "bricks": {
    "understanding": {
      "prefer": "agentic",
      "model": "my-game-vlm:7b",
      "provider": "ollama"
    }
  }
}
```

---

## Proposed Component-Based Design

### 1. Adapter Registry (Factory)
```
IAdapterRegistry
  - Register(TargetType, Func<ITargetAdapter>)
  - CreateAdapter(TargetType) → ITargetAdapter
  - GetRegisteredTypes() → TargetType[]
```
- Default: current switch in CreateAdapter
- Extensible: plugins or config can register custom adapters (e.g. `game://unity` with custom factory)

### 2. Brick Pipeline Configuration
```
IPipelineConfig
  - GetBrickIds() → string[]   // ordered
  - GetBrickSpec(string id) → BrickPipelineSpec?
  - IsOptional(string id) → bool

BrickPipelineSpec
  - BrickId, Enabled, Optional, RunCondition?, Config
```
- Pipeline defined in JSON; UniversalTesterAgent iterates config instead of fixed list
- Bricks resolved via `IBrickRegistry` (existing domain concept) or a dedicated Universal Tester brick registry

### 3. Per-Brick Configuration
Extend `BrickRuntimeSpec` (or add `BrickConfig`):
```
BrickRuntimeSpec (existing)
  - Prefer, Fallback

BrickConfig (new, per-brick)
  - Mode?: string           // "temporal", "gameStateFirst", "pixelOnly"
  - Model?: string           // override for this brick
  - Provider?: string        // override for this brick
  - PromptTemplate?: string  // key into template registry
  - CustomConfig?: Dictionary<string, object>  // service URLs, etc.
```

### 4. Understanding Mode
Add to config and wire into UnderstandingBrick:
- `pixelOnly` – current default; vision + optional DOM/elements
- `gameStateFirst` – when GameState present, lead with it; vision as supplement
- `temporal` – multi-frame emphasis; gameplay-specific prompt variant
- `worldModel` – (future) pass to prediction service before exploration

### 5. Configuration File Schema (JSON)
```json
{
  "version": 1,
  "prefer": "agentic",
  "multiFrameCount": 4,
  "pipeline": ["perception", "understanding", "exploration", "action", "validation", "reporting"],
  "bricks": {
    "perception": { "prefer": "agentic", "fallback": ["agentic", "deterministic"] },
    "understanding": {
      "prefer": "agentic",
      "mode": "temporal",
      "model": "llava:7b",
      "provider": "ollama"
    },
    "exploration": { "prefer": "agentic" },
    "action": { "prefer": "deterministic" },
    "validation": { "prefer": "agentic" },
    "reporting": { "prefer": "agentic" }
  },
  "adapters": {
    "game": { "type": "GameAdapter", "host": "localhost", "port": 9999 }
  }
}
```

---

## Implementation Phases

### Phase 1: Config-Driven Understanding Mode ✅ Implemented
- Add `UnderstandingMode` and `BrickConfig` to `UniversalTesterRuntimeConfig`
- In UnderstandingBrick, branch prompts based on mode + `perception.GameState`
- Supports Options A, C immediately

### Phase 2: Adapter Registry ✅ Implemented
- Extract `CreateAdapter` to `IAdapterRegistry` with default registrations
- Allow config to override adapter params (host, port) for Game
- Enables custom adapters without code change

### Phase 3: Pipeline Configuration ✅ Implemented
- Define pipeline as ordered list of brick IDs in config
- Resolve bricks by ID from a registry; skip disabled/optional
- Enables Option B (WorldModelPredictionBrick) as optional step

### Phase 4: Per-Brick Model/Provider Override (Low–Medium Effort)
- Add `Model`, `Provider` to per-brick config
- Pass to `IProviderFactory` when executing brick
- Supports Option D

### Phase 5: Prompt Template Registry ✅ Implemented
- `IPromptTemplateRegistry`, `DefaultPromptTemplateRegistry` with built-in templates
- `promptTemplate` key in BrickOverrides references named template (e.g. `temporal-gameplay`)
- `promptTemplatesPath` in config loads additional templates from JSON file
- Placeholders: `{{goal}}`, `{{frameCount}}`, `{{gameStateSection}}`, `{{audioSection}}`, etc.

---

## File Changes Summary

| File | Changes |
|------|---------|
| `UniversalTesterConfig.cs` | Optional: `UnderstandingMode`, `UseGameStateFirst` |
| `UniversalTesterRuntimeConfig.cs` | Add `Pipeline`, `BrickConfig` (extends BrickRuntimeSpec), `Adapters` |
| `UniversalTesterAgent.cs` | Use pipeline config; resolve bricks from registry; pass brick config |
| `UnderstandingBrick.cs` | Branch on mode; use GameState in vision prompt when gameStateFirst |
| `IAdapterRegistry.cs` (new) | Interface + DefaultAdapterRegistry |
| `UniversalTesterRuntimeConfigLoader.cs` | Deserialize new schema; backward compat |
| `BrickPipelineSpec.cs` (new) | Optional, if separating pipeline from runtime config |
| `docs/universal-tester-spec.schema.json` (new) | JSON Schema for config validation |

---

## Backward Compatibility

- Existing JSON (only `prefer`, `bricks` with `BrickRuntimeSpec`) continues to work
- `Pipeline` defaults to current fixed order if absent
- `UnderstandingMode` defaults to `auto` (infer from GameState + frame count)

## Example Config

See `docs/universal-tester-agent-spec.example.json` for a full example with `understandingMode`, `brickOverrides`, and `pipeline`.
