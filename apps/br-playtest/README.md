# Nexo BR Playtest Agent

Local-only Nexo `ToolCallingAgent` host for the Unity BR Weapon Lab.

## Capabilities

The agent receives an allowlisted toolbox only:

- `game.launch` — launch the configured Weapon Lab `.app`
- `game.status` — structured health/position/weapon/camera state
- `game.keyboard` — semantic W/A/S/D/Space/R/E/Mouse1 input
- `game.look` — deterministic first-person yaw/pitch
- `game.screenshot` — camera-native PNG capture
- `game.screen_capture` — optional macOS desktop capture fallback
- `game.nearby` — renderer/clipping diagnostics
- `playtest.report` — JSON and Markdown evidence
- `game.stop` — terminate the allowlisted game process

There is no generic shell, arbitrary process, repository write, web-search, or
network-export tool. Unity listens only on `127.0.0.1`.

## Pipeline

```bash
bash apps/br-playtest/scripts/run_pipeline.sh
```

This configures/builds the Weapon Lab when Unity is closed, launches it, runs the
agent sequence, writes screenshots and reports under:

```text
.nexo/playtest/br-weapon-lab/
```

Reuse an existing build:

```bash
bash apps/br-playtest/scripts/run_pipeline.sh --skip-build
```

Run one agent cycle directly:

```bash
bash apps/br-playtest/scripts/run_once.sh
```

Keep the local agent hosted on a 30-minute interval:

```bash
bash apps/br-playtest/scripts/run_daemon.sh --interval-minutes 30
```

The standard agent-set source is
`config/agent_set.br-playtest.local.json`. The current host uses a deterministic
local planner so it works without Ollama; an Ollama planner can replace the
`IModel` without changing tools or policy boundaries.

The `playtest` role is also registered with Nexo's normal
`IBackgroundAgentRegistry` through `IPlaytestRunRunner`, and
`UnityWeaponLabGameRunner` implements the orchestration-layer `IGameRunner`
contract. Game Director Studio exposes:

- `br_run_playtest`
- `br_get_playtest_report`

through its MCP registry when the playtest runner is installed.

## Unity bridge

`WeaponLabPlaytestBridge` is opt-in and activated by:

- `NEXO_BR_PLAYTEST_PORT`, or
- `-br-playtest-port <port>`

Optional hardening:

- `NEXO_BR_PLAYTEST_TOKEN`
- `NEXO_BR_PLAYTEST_ARTIFACT_ROOT`

OS-level `screencapture` may require macOS Screen Recording permission. Normal
input and game screenshots use the Unity IPC bridge and require neither
Accessibility nor Screen Recording access.

## True OS virtual-player mode

Run the windowed build with real macOS keyboard/mouse events and desktop capture:

```bash
bash apps/br-playtest/scripts/run_virtual_player.sh
```

This mode is intentionally opt-in and uses only allowlisted keys
`W/A/S/D/E/R/Space`, mouse movement/left-click, and `screencapture`. It still
uses localhost game telemetry to verify that physical input changed gameplay.

One-time macOS permissions may be required:

1. **System Settings → Privacy & Security → Accessibility** — allow Cursor and/or
   the `Nexo.BRPlaytestAgent`/`dotnet` process.
2. **Screen Recording** — allow the process responsible for `screencapture`.

The normal hosted daemon continues using deterministic semantic input because it
is reliable and does not require OS permissions. The OS virtual player is a
separate first-pass/manual certification lane.

## Weapon content agents

Runtime weapon mechanics live in the typed `BR.Game.Weapons` kernel. Agents
create drafts under:

```text
NexoBattleRoyale/.nexo/br-director/proposals/<proposal-id>/
  proposal.json
  descriptor.weapon.json
```

The Unity **Battle Royale → BR Director Studio** window approves/rejects drafts.
Approval copies the descriptor into the live descriptor directory, validates it
against the kernel constructor, and materializes a `WeaponDefinitionAsset`.
Generated assets and runtime C# are not agent-owned.

Run the local Ollama content author:

```bash
bash apps/br-playtest/scripts/run_weapon_content_agent.sh \
  "Create a three-round burst rifle for mid-range combat"
```

The script sandboxes writes to the descriptor directory via
`NEXO_SANDBOX_ROOT` and `NEXO_PATH_ALLOWLIST_EXTRA`.
