## Nexo Agent Foundry for Unity (Offline)

This demo adds an offline Agent Workbench and runtime tools for a **Doom-style FPS game** in Unity.

### 🎮 **Game Features**
- **FPS Movement**: WASD + mouse look with jump
- **Weapon System**: Shotgun with hitscan mechanics, recoil, and reload
- **Enemy AI**: Imp enemies with NavMesh pathfinding, patrol, chase, and attack
- **Puzzle System**: Collect keys to unlock doors
- **Procedural Generation**: Doom-style rooms and corridors
- **HUD System**: Health, ammo, crosshair, and message display
- **Audio Feedback**: Footsteps, weapon sounds, enemy audio

### 📋 **Requirements**
- Unity 2021.3+ (LTS recommended)
- Packages are provided under `Packages/` for UPM (local)
- No cloud dependencies - fully offline

### 🎯 **Menu Path**
- **Nexo → Agent Workbench**

### 🚀 **Quick Start**
1. **Open Unity project** (root of this repo as a Unity project workspace)
2. **Open menu** `Nexo → Agent Workbench`
3. **Generate Assets**: Click "Generate" to create all game components
4. **Validate**: Click "Validate" to run validation agents
5. **Play**: Open scene `Assets/AgentFoundryDemo/Scenes/E1M1_Blockout.unity`

### 🎮 **Interactive Gameplay**

#### **Controls**
- **WASD**: Move forward/backward/strafe
- **Mouse**: Look around
- **Space**: Jump
- **Left Click**: Fire weapon
- **R**: Reload weapon
- **E**: Interact with doors/keys

#### **Gameplay Loop**
1. **Explore** the procedurally generated rooms
2. **Collect** the blue key in the first room
3. **Fight** enemy imps using the shotgun
4. **Unlock** the blue door to progress
5. **Survive** and complete the level

#### **Objectives**
- Collect the blue key
- Kill all enemy imps
- Unlock the blue door
- Survive with health remaining

### 🤖 **Validation Agents**

#### **Playbot** (Input System)
- Tests WASD movement
- Validates mouse look
- Checks weapon firing
- Verifies interaction inputs

#### **UIValidator** (UIToolkit)
- Checks HUD element visibility
- Validates contrast ratios (≥4.5)
- Tests message display system
- Verifies crosshair positioning

#### **PerfGuard** (Performance)
- Monitors frame time (target: 60 FPS)
- Tracks memory allocations
- Checks NavMesh update frequency
- Validates audio source limits

#### **NavGuard** (Navigation)
- Bakes NavMesh automatically
- Tests enemy pathfinding
- Validates reachability
- Checks patrol point connectivity

#### **CodeGate** (Policies)
- Enforces coding standards
- Bans dangerous APIs (Process.Start)
- Requires XML documentation
- Validates license compliance

### 🏗️ **Bootstrap a Fresh Unity Project**

Import local Nexo packages:
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


