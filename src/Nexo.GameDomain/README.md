# Nexo.GameDomain

Deterministic gameplay bricks for Unity and other `netstandard2.0` hosts.

## What this is

Open-core library that reuses Nexo’s brick **strategy / execute / aggregate** shape without the AI host:

- `DomainBrick` atoms with `ImplementationType.Deterministic` only
- `GameplayBrickRegistry` + `DeterministicGameplayRunner`
- Fan-in `GameplayJoinStrategy` + `GameplayResultAggregator`

Depends only on **`Nexo.Brick.Contracts`** (no Hosting, Orchestration, or providers).

## Quick start

```csharp
using Nexo.GameDomain;
using Nexo.GameDomain.Bricks;
using Nexo.Core.Domain.Execution;

var registry = GameplayBrickRegistry.CreateDefault();
var runner = new DeterministicGameplayRunner(registry);
var context = GameplayExecutionContext.ForPlayer("player-1");

var input = new BrickInput();
input.Set("baseDamage", 100);
input.Set("critMultiplierPercent", 150);
input.Set("armor", 20);
input.Set("isCrit", true);

var output = await runner.ExecuteAsync("damage-resolver", input, context);
var finalDamage = output.Get<int>("finalDamage"); // 130
```

## Default bricks

| Id | Purpose |
|----|---------|
| `damage-resolver` | Crit + armor → final damage |
| `ability-gate` | Cooldown / resource gate |
| `score-aggregate` | Weighted score fold |

## Unity notes

- Target `netstandard2.0` (or consume the `net8.0` build from Editor tooling).
- Keep the player hot path on `DeterministicGameplayRunner`.
- Run Director Studio / agentic proposal loops in a sidecar; swap certified bricks into this registry.
