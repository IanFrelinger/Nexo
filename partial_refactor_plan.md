# Partial Refactor Plan (Nexo)
- Mechanical split only; no public API changes.
- {Type}.Core.cs: type decl + fields + ctors + constants + type-level attributes.
- {Type}.Orchestrator.cs: coordinating entry points that delegate to slices.
- Other slices by concern: Validation, Parsing, Execution, Mapping, IO, Diagnostics, Utilities.
- Repeat full type header on every partial (access + modifiers + partial + bases + constraints).
- Tests: keep type-level test attributes only in Core.
