# ADR-001: Adopt Clean Architecture with MediatR & Ports/Adapters

## Status

Accepted – November 24, 2025

## Context

The original CLI used ad-hoc command handlers and tightly coupled implementations. The project required:
- Clear separation between presentation (CLI), application, domain, and infrastructure.
- Testability via unit, integration, and E2E layers.
- Support for MediatR-based CQRS, FluentValidation, and dependency injection.

## Decision

Adopt Clean Architecture with the following conventions:
- **Presentation:** `Nexo.CLI` (System.CommandLine, DI bootstrap)
- **Application:** `Nexo.Core.Application` (MediatR commands/queries, validation, behaviors)
- **Domain:** `Nexo.Core.Domain` (value objects, exceptions, error codes)
- **Infrastructure:** `Nexo.Infrastructure` (adapters for analysis, validation, agents, configuration, caching, metrics)
- Depend on abstractions only: Presentation → Application → Domain; Infrastructure implements ports.
- Use MediatR for request/response flow; apply FluentValidation via pipeline behaviors.

## Consequences

**Positive**
- High testability (unit, integration, E2E) due to clear boundaries.
- Extensibility via ports/adapters (e.g., new analysis rules, test parsers, agent implementations).
- Improved developer experience with centralized configuration, error codes, and documentation.

**Negative**
- Increased upfront complexity (multiple projects/layers).
- Requires consistent DI wiring and documentation to onboard new contributors.

