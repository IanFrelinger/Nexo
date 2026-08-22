# Runtime Spec Profiles

This folder contains ready-to-run routing/config profiles for:

- task-size model matching (`small`, `medium`, `large`)
- adaptive personal software runtime context (`runtime-manifest`)
- creative-director workflow behavior (`self-extend` workflow spec)

## 1) Orchestration runtime specs (model routing)

Use with `ashlar orchestrate --runtime-spec ...`.

- `small_task.orchestration.runtime-spec.json`
- `medium_task.orchestration.runtime-spec.json`
- `large_task.orchestration.runtime-spec.json`

Example:

- `dotnet run --project application/src/Ashlar.CLI -- orchestrate "analyze this test failure" --runtime-spec docs/runtime/specs/small_task.orchestration.runtime-spec.json --format-json`

## 2) Adaptive runtime manifests (personal software context)

Use with `ashlar runtime execute --runtime-manifest ...`.

- `small_task.runtime-manifest.json`
- `medium_task.runtime-manifest.json`
- `large_task.runtime-manifest.json`
- `creative_director.runtime-manifest.json`

Example:

- `dotnet run --project application/src/Ashlar.CLI -- runtime execute --goal "scaffold a personal planning app extension" --runtime-manifest docs/runtime/specs/creative_director.runtime-manifest.json --provider ollama --run-tests`

## 3) Self-extend workflow runtime specs (pipeline behavior)

Use with `ashlar self-extend run --runtime-spec ...`.

- `creative_director.self-extend.runtime-spec.json`

Example:

- `dotnet run --project application/src/Ashlar.CLI -- self-extend run --goal "design and scaffold a polished dashboard feature" --runtime-spec docs/runtime/specs/creative_director.self-extend.runtime-spec.json --provider ollama --run-tests`

## Notes on model choice

`OrchestrationRuntimeSpec` routes by provider and preference. Specific OSS model IDs are selected by provider-level environment variables (for example, `OLLAMA_MODEL` and `OLLAMA_VISION_MODEL`).
