## Nexo Agent Foundry for Unity (Offline)

This demo adds an offline Agent Workbench and runtime tools for a Doom-style slice in Unity.

Requirements:
- Unity 2021.3+ (LTS recommended)
- Packages are provided under `Packages/` for UPM (local)

Menu Path:
- Nexo → Agent Workbench

Steps:
1) Open Unity project (root of this repo as a Unity project workspace).
2) Open menu Nexo → Agent Workbench.
3) Paste a short story, click Generate. Scripts appear under `Assets/AgentFoundryDemo/` and compile.
4) Click Validate to run offline validators (stubs included; full checks in tests).
5) Open scene `Assets/AgentFoundryDemo/Scenes/E1M1_Blockout.unity` (scaffold placeholder created on first gen).

Bootstrap a fresh Unity project and import local Nexo packages:
```bash
chmod +x scripts/unity-bootstrap.sh
UNITY_PATH="/Applications/Unity/Hub/Editor/2021.3.41f1/Unity.app/Contents/MacOS/Unity" \
scripts/unity-bootstrap.sh ~/Projects/NexoUnityDemo "$UNITY_PATH"
```
Then open `~/Projects/NexoUnityDemo` in Unity → Nexo → Agent Workbench.

Headless CI (Unity Test Framework):
```bash
/Applications/Unity/Hub/Editor/2021.3.41f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -runTests \
  -testResults ./TestResults.xml \
  -testPlatform PlayMode
```

Notes:
- Offline by default: set environment `NEXO_AI_MODE=off`.
- No cloud calls or third-party licenses required.


