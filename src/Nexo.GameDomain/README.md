# Nexo.GameDomain

Deterministic gameplay bricks for Unity and other `netstandard2.0` hosts.

## What this is

Open-core library that reuses Nexo’s brick **strategy / execute / aggregate** shape without the AI host:

- Typed, synchronous rules for the authoritative simulation hot path
- Fixed-tick combat simulation, ordered events, replay, and state hashing
- `DomainBrick` atoms with `ImplementationType.Deterministic` only
- `GameplayBrickRegistry` + `DeterministicGameplayRunner`
- Fan-in `GameplayJoinStrategy` + `GameplayResultAggregator`

Depends only on **`Nexo.Brick.Contracts`** (no Hosting, Orchestration, or providers).

## Authoritative hot path

```csharp
using Nexo.GameDomain.Contracts;
using Nexo.GameDomain.Rules.Combat;

var command = new DamageCommand(
    source: new EntityId(1),
    target: new EntityId(2),
    baseDamage: 100,
    criticalMultiplierPercent: 150,
    isCritical: true);
var result = DamageRules.Resolve(command, new DamageContext(currentHealth: 200, armor: 20));
var finalDamage = result.AppliedDamage; // 130
```

`DamageResolverBrick` delegates to this same typed rule. Use bricks for Nexo
authoring/certification and `DamageRules` inside the live server simulation.

## Default bricks

| Id | Purpose |
|----|---------|
| `damage-resolver` | Crit + armor → final damage |
| `ability-gate` | Cooldown / resource gate |
| `score-aggregate` | Weighted score fold |

## Weapon rules

Typed, allocation-light weapon mechanics live under `Rules/Weapons`:

- `WeaponCoreDefinition` — immutable magazine / fire-mode / spread / recoil data
- `WeaponStateMachine` — tick-driven fire, reload, cooldown, burst
- `WeaponBallistics` — seeded spread/recoil sampling and range/region damage

Unity hosts should keep a thin adapter (see BR `WeaponKernel`) and must not pull
Fusion/VFX types into this package.

## Unity notes

- Target `netstandard2.0` (or consume the `net8.0` build from Editor tooling).
- Keep hot paths on typed synchronous rules; do not use `BrickInput` dictionaries per frame.
- Keep Unity/Fusion types in adapter assemblies outside this package.
- Hash authoritative rules state for replay checks; do not hash Unity physics contacts.
- Run Director Studio / agentic proposal loops in a sidecar; swap certified bricks into this registry.
