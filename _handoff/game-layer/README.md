# Game layer (extracted from the Ashlar kernel)

48 files lifted out of the kernel: the Playtest tree, the GameDomain tree (recognition
patterns, domain agents, asset agents), `TileMapRenderTool`, and 8 test files.

**This tree does not compile yet.** There is no project file, nothing here has been built,
and the namespaces still claim to be part of the kernel. Everything below is what remains
to be done, in order.

---

## 1. It has no `.csproj`

The extraction moved source files, not a project. You need one — multi-targeting to match
the kernel (`net8.0;net10.0`; executables roll forward, libraries multi-target).

## 2. The namespaces still say `Ashlar.Orchestration.*`

The moved code kept the namespaces it had inside the kernel:

| Path | Namespace |
|---|---|
| `src/GameLayer/Playtest/` | `Ashlar.Orchestration.Playtest*` |
| `src/GameLayer/Agents/Playtest/` | `Ashlar.Orchestration.Agents.Playtest` |
| `src/GameLayer/GameDomain/` | `Ashlar.Orchestration.GameDomain*` |
| `src/GameLayer/Tools/` | `Ashlar.Tools.Dev` |

Declaring those from a different assembly is legal C# but misleading — it reads as if the
types ship with the kernel. Decide deliberately: keep them (least churn, keeps the tests
compiling unchanged) or rename to your own root (honest, but touches every file and every
test's `using` lines).

## 3. What it needs from the kernel

These are the seams the extraction was built around. The game layer implements them; the
kernel defines them and knows nothing about the implementations.

| Kernel type | Assembly | Used for |
|---|---|---|
| `IDomainAgentProvider`, `IAgentCreationContext` | `Ashlar.Orchestration` | `PlaytestAgentProvider`, `GameDomainAgentProvider`, `GameAssetAgentProvider` |
| `IDomainPatternProvider` | `Ashlar.Orchestration` | `GameDomainPatternProvider` |
| `BaseAgent`, `BaseDomainAgent`, `LoggerAdapter<T>` | `Ashlar.Orchestration` | every agent here |
| `BaseAssetAgent`, `Generated*Asset`, `GenerationPrompt` | `Ashlar.Orchestration` | the three asset agents |
| `IImageGenerator`, `IAudioGenerator`, `IModel3DGenerator`, `IAssetStorage` | `Ashlar.Orchestration` | asset ports — **these stayed in the kernel on purpose** |
| `AgentSpawnSpec` | `Ashlar.Orchestration` | all providers |
| `IModel`, `ITool`, `IToolSource` | `Ashlar.Abstractions` | models and tools |

**Nothing is published to nuget.org and there are no git tags**, so there is no package to
restore yet. Until Wave 6 lands, use a `ProjectReference` into a sibling Ashlar checkout, or
a local folder feed packed from one. `consumer-template/` in the Ashlar repo exists for this,
and `scripts/verify-external-product-shape.sh` builds such a feed.

## 4. How an application turns it on

```csharp
services.AddGameDomain();        // patterns + domain agents + asset agents
services.AddPlaytestAgents();    // playtest agents
services.AddSingleton<IToolSource, GameToolSource>();   // repo.tile_map.render
```

`AddGameDomain()` is a convenience over `AddGameDomainPatterns()`,
`AddGameDomainAgents()` and `AddGameAssetAgents()`, which can be taken individually — an
application may want the asset agents without the combat/economy ones, or the recognition
patterns without any agents.

The agents resolve their own collaborators at spawn time, so the application must also
register whatever they need: `IGameRunner` and `ITelemetryStore` for playtest, and an
`IImageGenerator` / `IAudioGenerator` / `IModel3DGenerator` plus `IAssetStorage` for assets.
Missing ones throw when an agent is spawned, not at registration — unchanged from before
the extraction.

## 5. Behaviour to be aware of

- **Without this package installed the kernel does not fail — it degrades.** Combat,
  economy, gameplay, playtest and asset domains become ordinary unrecognised domains and
  return a `GenericAgent`. Domain recognition simply matches fewer domains, which lowers RAG
  similarity scores rather than erroring.
- **The kernel still recognises the AI domain.** `DomainRecognizer` kept the
  general-purpose AI vocabulary (agent, neural, learning, decision); this package adds the
  game half (npc, pathfinding, steering, non-player, character, navigation) under the same
  `"AI"` key, and the two lists are merged. But `AIAgent` lives here, because its prompt
  reads "specializing in game AI".
- **Two domains are claimed but unbuildable, carried over verbatim.**
  `PlaytestAgentProvider` claims `"telemetry"` with no case to build it, and
  `GameAssetAgentProvider` claims `"shader"` and `"animation"` likewise. Both throw
  `ArgumentException`. This is exactly what `AgentFactory` did before the extraction and was
  preserved rather than repaired so that no behaviour change hid inside a mechanical move.
  The asset one is covered by a test; the playtest one is pinned by a test that documents it
  as an oddity. Fixing them is a reasonable first change to make here.

## 6. Known gap in this tree

`GameToolSource.cs` was written after the extraction, not carried over from anywhere. Phase A
of the refactor removed `tools.Register(new TileMapRenderTool())` from
`RepoFsToolboxFactory` on the stated grounds that the game package would supply the tool
through `IToolSource` — but no implementation of that interface existed anywhere in the
repository, so `repo.tile_map.render` was reachable by nobody in between. `GameToolSource` is
that missing implementation. **It has never been compiled**, because this tree has no project
file. Build it first.

## 7. Naming

`GameLayer` is a placeholder. Pick the real name, then:

    grep -rl GameLayer . | xargs sed -i 's/GameLayer/YourName/g'
    find . -depth -iname '*GameLayer*' -exec bash -c 'mv "$0" "${0//GameLayer/YourName}"' {} \;

Note the lesson from the Ashlar rename itself: a `sed` sweep cannot tell an *instance* of a
name from a *description* of one. If you write docs about this rename, exclude them from the
sweep, or they will be rewritten into nonsense.

## 8. Making it a repository

1. Pick the name (§7).
2. Add the project file (§1) and decide the namespace question (§2).
3. Wire the kernel reference (§3) and build. Fix what falls out — `GameToolSource` first.
4. `git init && git add -A && git commit -m "chore: extract game layer from Ashlar kernel"`
