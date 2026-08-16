---
id: door-lock-transition
title: Compute the next state of a three-state door lock for a given event
status: pending
source: Human
priority: 20
tags:
  - dogfood
  - state-machine
touch:
  pathPrefixes:
    - applications/Nexo.Samples.Dogfood/Locks/
  namespaces:
    - Nexo.Samples.Dogfood.Locks
  capabilities:
    - repo.fs.write
---

A door lock has three states and four triggers. (The input is named `trigger`, not `event` — `event` is a C# keyword and cannot be a variable name.) Provide a deterministic brick that, given the
current state and one trigger, reports the next state and whether the event was accepted.

The brick is class `DoorLockTransitionBrick` in namespace `Nexo.Samples.Dogfood.Locks`, with
`Id = "door-lock-transition"`.

Contract:

- Input `state` (string): the current state; one of `locked`, `unlocked`, `open`.
- Input `trigger` (string): the trigger; one of `unlock`, `lock`, `open`, `close`.
- Output `nextState` (string): the resulting state. NEVER null.
- Output `accepted` (bool): true only when the event is a valid transition from the state.

Transitions (all comparisons are case-sensitive, exact strings):

- `locked` + `unlock` → `unlocked`
- `unlocked` + `lock` → `locked`
- `unlocked` + `open` → `open`
- `open` + `close` → `unlocked`

Every other combination — including an unknown state or an unknown trigger — is rejected:
`accepted` is false and `nextState` is exactly the input `state`, unchanged (an unknown state
is echoed back as given).

Skeleton (fill in `ExecuteAsync`; do not add, remove, or reorder members):

```csharp
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Samples.Dogfood.Locks;

public sealed class DoorLockTransitionBrick : DomainBrick
{
    public DoorLockTransitionBrick()
    {
        Id = "door-lock-transition";
        Name = "Door Lock Transition";
        Description = "Computes the next state of a three-state door lock for a given event.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("state", "string", "current state"),
                new BrickInputDefinition("trigger", "string", "trigger")
            ],
            Outputs =
            [
                new BrickOutputDefinition("nextState", "string", "next state"),
                new BrickOutputDefinition("accepted", "bool", "accepted")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO
    }
}
```

Read inputs with `input.Get<string>("state", string.Empty) ?? string.Empty` and likewise for `trigger` (missing or null becomes the empty string).
Write outputs with `output.Set("nextState", value)` and `output.Set("accepted", value)` on a
`new BrickOutput()`, and return `Task.FromResult(output)`.

Deterministic only: no clock, no randomness, no I/O.
