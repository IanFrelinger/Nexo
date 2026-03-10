# Unity Sidecar Demo (Ubuntu-Testable)

This demo scaffolds a Unity-facing generation loop **outside kernel core** by reusing existing Nexo CLI orchestration.

## What it does

`tools/Nexo.UnitySidecarDemo` provides three commands:

- `generate` – takes a gameplay prompt, calls `nexo chat`, and writes Unity-style generated scripts
- `validate` – compiles generated scripts in Ubuntu using Unity stubs
- `run-demo` – runs `generate` + `validate` + `nexo dogfood block1`

Output root defaults to:

- `tools/unity-demo-output/Assets/GeneratedSystems/`

Generated files include:

- `Contracts/IGeneratedGameplaySystem.cs`
- `Contracts/SystemContext.cs`
- `<GeneratedClass>.cs`
- `generation_manifest.json`

## Why this fits Ubuntu

Ubuntu cannot run Unity Editor hot-reload, but it can validate the sidecar loop:

1. prompt handling
2. orchestration invocation (`nexo chat`)
3. code generation output
4. compile viability (via stubs + `dotnet build`)
5. nexo self-check (`dogfood block1`)

## Commands

```bash
# Full sidecar smoke demo
bash scripts/unity-sidecar-demo.sh run-demo --prompt "add a dash ability"

# Generate only
bash scripts/unity-sidecar-demo.sh generate --prompt "add a health pickup system"

# Validate generated scripts compile
bash scripts/unity-sidecar-demo.sh validate
```

## Suggested next step (Unity machine)

In Unity Editor, point your runtime loader at:

- `Assets/GeneratedSystems/`

Then call sidecar generation from an in-game prompt UI and trigger AssetDatabase refresh.
