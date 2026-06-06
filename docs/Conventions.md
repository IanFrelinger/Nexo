# Current code conventions

This document describes the conventions Nexo practices today. It is intentionally descriptive, not aspirational: this sprint does not refactor code, change exception behavior, remove interfaces, or migrate inheritance patterns.

## Error handling: values and exceptions both exist

Nexo is partway toward errors-as-values, but it is not uniformly there.

### Where errors-as-values is used

`Result<T>` appears in focused execution-routing paths:

- `src/Nexo.Core.Application/Execution/Routing/Result.cs`
- `src/Nexo.Infrastructure/Execution/Ollama/Result.cs`
- Routing and provider-adjacent code that models local/peer/cloud execution outcomes without using exceptions as the primary success/failure channel.

These areas are good examples of recoverable outcomes represented explicitly as values.

### Where exceptions are still used

Exceptions remain common and intentional in many parts of the codebase:

- Guard clauses such as null argument checks.
- Invalid state transitions, for example agent lifecycle methods that cannot execute from the current state.
- Framework and transport boundaries where exceptions map naturally to .NET, ASP.NET Core, gRPC, IO, or external process behavior.
- Validation, resource, persistence, and subprocess failures where existing APIs already throw.
- Domain exception types such as `DomainException` and specialized runtime/transport failures.

The rough split today is: **recoverable routing/provider outcomes sometimes use `Result<T>`; guard, lifecycle, IO, transport, framework interop, and many validation failures still use exceptions.**

## Interfaces and ports

Nexo is interface-heavy today. Interfaces are used for dependency inversion, package boundaries, extension seams, and testable orchestration ports.

Common examples include:

- Core abstractions such as agents, models, memory, tools, and provider factories.
- Application ports for observation, persistence, testing, mesh/networking, execution routing, and self-improvement.
- Orchestration ports for routing policies, build tools, game runners, asset generation, telemetry, and transport hooks.
- Runtime/transport surfaces such as gRPC channel factories and endpoint registries.
- Ingress-specific seams such as signature verification.

This is not a “no interfaces” codebase. The current architecture relies on interfaces to keep host, runtime, transport, app, and package seams explicit.

## Abstract classes and inheritance

Nexo also uses abstract classes where shared lifecycle or value-object behavior is centralized.

Examples include:

- Agent base classes that manage lifecycle state, health, timing, and execution hooks.
- Domain/value base types such as type-value objects and domain exceptions.
- Test abstractions and helper bases used by the existing test infrastructure.

The current codebase mixes composition with inheritance. The direction is still to keep boundaries explicit and traceable, but the implemented code is not purely composition-only.

## Generics

Generics are common and idiomatic throughout the repository:

- `Task<T>` and collection types on async ports and services.
- `Result<T>` in focused routing/provider paths.
- Generic stores, handlers, options, and typed client/runtime surfaces.
- Generic test and helper infrastructure.

Avoid presenting Nexo as a generics-free or intentionally flat-only codebase. The actual code uses .NET generics where they make contracts and data flow explicit.

## Aspiration vs. current state

The architectural aspiration is:

- Explicit, flat, traceable workflows.
- Composition over inheritance where it reduces coupling.
- Errors-as-values for recoverable operational outcomes.
- Clear auditability at trust, routing, and adaptation boundaries.

The current state is **partway there**:

- Some newer routing/provider paths use `Result<T>`.
- Many established paths still throw exceptions for guards, invalid state, IO, transport, and framework integration.
- Interfaces and abstract bases are both active parts of the design.
- Generics are widely used where they clarify typed contracts.

Treat further migration as future work. A follow-up issue should define where errors-as-values are valuable, which exception paths should remain, and which inheritance-heavy areas would benefit from composition. Do not start that migration as part of documentation or licensing work.
