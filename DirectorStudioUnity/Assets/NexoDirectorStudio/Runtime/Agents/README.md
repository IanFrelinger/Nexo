# Embedded Agents (Director + Autoplayer)

This folder contains embedded Unity agents that let you drive Director Studio end-to-end via natural-language prompts and auto-play the generated slice.

## Components

- `AgentDirector` (MonoBehaviour)
  - Input: freeform prompt (e.g., "Doom-style FPS. 15 min. Intense combat, key gates. seed=666.")
  - Orchestrates: Plan → Build → Interactions → Content
  - Launches a runtime scene and optionally attaches `AIAutoplayer`
  - Uses the same DI container as `DirectorStudioService`

- `AIAutoplayer` (MonoBehaviour)
  - Drives the player character
  - Policy: nearest enemy → power-up → goal → wander
  - Aims and fires, walks, and completes simple objectives

## Usage

1) Create an empty GameObject in your Unity scene
2) Add `AgentDirector` component
3) Paste your prompt and set options (genre hint, minutes, difficulty, seed)
4) Tick `Auto Launch` and (optionally) `Attach Autoplayer`
5) Press Play — the agents will generate the slice and auto-play it

### Example Prompt

```
Doom-style FPS. 15 minutes. Intense combat, key-locked doors, atmospheric lighting. seed=666.
```

## Options

- `prompt`: natural-language directive
- `genreHint`: "FPS" | "Platformer" | "RPG" (or leave blank to auto-detect)
- `targetMinutes`: session duration
- `difficulty`: 1..5
- `seed`: integer seed for deterministic builds
- `attachAutoplayer`: attach `AIAutoplayer` to the Player to auto-play

## Notes

- These agents sit entirely in `Assets/NexoDirectorStudio/**` per guardrails
- They do not modify Nexo core assemblies
- Offline adapters are consulted/stubbed according to Phase 8 behavior
- For CI/headless, keep using the existing headless/validation tests; agents are runtime/editor features
