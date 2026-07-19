# Host bridge and artifact sinks

ADR for promoting BR Weapon Lab playtest infrastructure into open-core
layers so other Nexo hosts can reuse the same seams.

## Status

Accepted — implementation phased (host bridge → evidence/sink →
deterministic planner → GameDomain weapon rules).

## Context

The BR Weapon Lab work produced reusable patterns that lived only in
`tools/Nexo.BRPlaytestAgent` and duplicated TCP/NDJSON logic in
`Nexo.BackgroundAgents.HostRunners.UnityWeaponLabGameRunner`:

- Localhost NDJSON agent↔host bridge (`{id,token,cmd,args}` → `{ok,data|error}`)
- Evidence packages under `.nexo/playtest/...` (screenshots, videos, reports)
- Unattended Google Drive upload via service account
- Deterministic no-LLM `IModel` that emits tool-call sequences
- Combat/weapon rules that belong in `Nexo.GameDomain` rather than Unity

## Decision

| Concern | Open-core home | Stays out of kernel |
|---------|----------------|---------------------|
| Bridge protocol + client port | `Nexo.Abstractions` + `Nexo.HostBridge` | Unity command handlers / Fusion |
| Endpoint / token / artifact-root VOs | `Nexo.Core.Domain` | Product-specific env defaults |
| Application ports (`IArtifactSink`, evidence writer) | `Nexo.Core.Application` | BR verdict checklists |
| Drive / filesystem sink adapters | `Nexo.Infrastructure` | Credential files |
| Deterministic tool-sequence `IModel` | `Nexo.BackgroundAgents` | BR scenario step lists |
| Weapon fire / spread / reload rules | `Nexo.GameDomain` | Viewmodels, capture, netcode |

### Host bridge

- Wire format remains NDJSON over TCP (localhost by default).
- Product command vocabularies (`status`, `approach_pickup`, …) stay
  host-specific; the transport does not interpret them.
- `IHostBridgeClient` is the shared port; TCP implementation lives in
  `Nexo.HostBridge` so HostRunners and tools avoid a heavy Infrastructure
  dependency for framing alone.

### Evidence and uploads

- Domain models describe evidence bundles and uploaded artifacts.
- `IArtifactSink` is provider-agnostic; Google Drive is one adapter.
- Upload configuration remains environment-driven for unattended runs.

### Non-goals

- AVFoundation / GPU capture plugins
- macOS Accessibility virtual-player automation
- Fusion feedback / HUD / prefab scaffolders
- Moving commercial Game Director MCP into open core

## Consequences

- BR playtest agent and `UnityWeaponLabGameRunner` share one client.
- Future hosts (CLI daemons, other engines) implement the same NDJSON
  server contract or reuse the client against an existing host.
- Provider SDKs (Google APIs) stay in Infrastructure; Domain never
  references them.
- Weapon rule extraction into GameDomain is a later phase and must not
  pull Unity types into `Nexo.GameDomain`.

## Phases

1. **Host bridge** — done (`Nexo.HostBridge`, shared client)
2. **Evidence + artifact sink** — done (`IArtifactSink`, Drive/local adapters)
3. **Deterministic tool-sequence model** — done (`DeterministicToolSequenceModel`)
4. **GameDomain weapons** — done (`Rules/Weapons` + Unity `WeaponKernel` facade)
