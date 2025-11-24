# Nexo API Reference

This document summarizes the primary public APIs exposed by the Nexo Clean Architecture implementation. It focuses on the Application layer ports (interfaces) and the configuration services that external adapters are expected to implement.

## Application Ports

### `Nexo.Core.Application.Analysis.Ports.IAnalysisService`
- **Purpose:** Runs static analysis and policy validation against a given path.
- **Method:** `Task<AnalysisResult> AnalyzeAsync(DirectoryInfo path, CancellationToken token = default)`
- **Return:** `AnalysisResult` containing violations, totals, and severity metadata.

### `Nexo.Core.Application.Validation.Ports.IValidationService`
- **Purpose:** Executes validation/test suites.
- **Method:** `Task<ValidationResult> ValidateAsync(string? filter, CancellationToken token = default)`
- **Return:** `ValidationResult` with counts, pass/fail flags, and per-test details.

### `Nexo.Core.Application.Agent.Ports.IAgentExecutor`
- **Purpose:** Runs registered agents with optional input files.
- **Method:** `Task<AgentExecutionResult> ExecuteAsync(string agentName, FileInfo? inputFile, CancellationToken token = default)`
- **Return:** `AgentExecutionResult` tracking success state, duration, and output payload.

### `Nexo.Core.Application.Agent.Ports.IAgentRegistry`
- **Purpose:** Provides metadata about available agents.
- **Methods:**
  - `Task<IReadOnlyList<AgentMetadata>> GetAgentsAsync(...)`
  - `Task<AgentMetadata?> GetAgentAsync(string agentName, ...)`
  - `Task<IReadOnlyList<AgentMetadata>> DiscoverAgentsAsync(...)`

### `Nexo.Core.Application.Common.Ports.ICacheStrategy`
- **Purpose:** Abstracts caching implementations (Decorator pattern).
- **Methods:** `GetAsync`, `SetAsync`, `RemoveAsync`, `ClearAsync`.

### `Nexo.Core.Application.Common.Ports.IMetricsCollector`
- **Purpose:** Collects execution metrics and counters.
- **Methods:** `RecordExecutionTime`, `IncrementCounter`, `GetSnapshotAsync`.

### `Nexo.Core.Application.Configuration.Ports.IConfigurationService`
- **Purpose:** Loads and saves CLI configuration (analysis, validation, logging).
- **Methods:** `LoadAsync`, `SaveAsync`, `GetDefault`.

## Domain Exceptions & Error Codes

All domain-level failures use specialized exceptions with error codes and suggestions.

| Exception | Typical Error Codes | Notes |
|-----------|--------------------|-------|
| `AnalysisException` | `ANALYSIS_100x` | Path issues, unauthorized access, rule failures. |
| `ValidationException` | `VALIDATION_200x` | Missing tests, TRX parsing issues, timeouts. |
| `AgentExecutionException` | `AGENT_300x` | Missing agents, timeouts, invalid inputs. |
| `ConfigurationException` | `CONFIG_400x` | Config file not found, invalid format/values. |

`docs/TROUBLESHOOTING_GUIDE.md` provides concrete suggestions per error code.

## Extension Points

- **Analysis Rules:** Implement `Nexo.Infrastructure.Analysis.Rules.IAnalysisRule` and register via DI to extend rule coverage.
- **Validation Parsers:** Implement `Nexo.Infrastructure.Validation.Parsers.ITestResultParser` to support additional test result formats.
- **Agents:** Implement `Nexo.Abstractions.IAgent` and register in DI (or discovered via `IAgentRegistry`) to add new agents.

## ADRs

Architecture decisions and rationale are documented in `docs/adr/`. See `ADR-001-clean-architecture.md` for the decision to adopt Clean Architecture + MediatR + FluentValidation.

