# Seed Component Library Audit

**Purpose:** Map existing capability components against the five North Star component type families required for runtime agent composition. Document coverage and gaps.

**Reference:** [NorthStarGapAnalysis.md](NorthStarGapAnalysis.md) Layer 7.

---

## Five Component Type Families

| Family | Subtypes | Purpose |
|--------|----------|---------|
| **Perception** | Vision, Audio, Code Analysis, Data Parsing | Observe and ingest inputs |
| **Action** | UI Interaction, API Call, Code Generation, Process Control | Execute changes in the world |
| **Reasoning** | Comparison, Classification, Planning, Validation | Analyze and decide |
| **Memory** | Short-term, Long-term query, Episodic | Store and retrieve context |
| **Reporting** | Structured output, Human-readable, Audit log, Suggestion surface | Produce outputs for humans or systems |

---

## Current Coverage

### Perception

| Component | Type | Status | Notes |
|-----------|------|--------|-------|
| ObservationContextBrick | Code Analysis / Context | EXISTS | Assembles observation context from patterns, adaptations |
| RoslynBrickStaticAnalyzer | Code Analysis | EXISTS | Static analysis for brick violations |
| OWASPScannerBrick | Code Analysis / Security | EXISTS | Security scanning |
| code-analysis (CapabilityComponentRegistry) | Code Analysis | EXISTS | Composition engine seed |
| Vision | - | MISSING | No vision/image input component |
| Audio | - | MISSING | No audio input component |
| Data Parsing | - | PARTIAL | TrxTestResultParser, etc. exist but not as composition components |

### Action

| Component | Type | Status | Notes |
|-----------|------|--------|-------|
| RepoFsWriteTool, RepoFsSearchReplaceTool | Code Generation (file) | EXISTS | In RepoFsToolboxFactory |
| RemoteBrick | API Call (remote execution) | EXISTS | Proxies to remote brick catalog |
| UI Interaction | - | MISSING | No UI automation component |
| Process Control | - | PARTIAL | Test runner spawns processes; no general process control brick |

### Reasoning

| Component | Type | Status | Notes |
|-----------|------|--------|-------|
| RoslynBrickStaticAnalyzer | Validation | EXISTS | Validates brick structure |
| DotNetRegressionTestRunner | Validation | EXISTS | Regression validation |
| BehavioralAnalyzer | Analysis | EXISTS | Behavior analysis |
| test-discovery, test-execution, result-aggregation | Planning (test matrix) | EXISTS | Composition engine seeds |
| Comparison | - | PARTIAL | Implicit in analyzers |
| Classification | - | PARTIAL | Violation classification |
| Planning | - | PARTIAL | Composition engine composes; no explicit planning brick |

### Memory

| Component | Type | Status | Notes |
|-----------|------|--------|-------|
| IAgentMemory (toolbox) | Short-term | EXISTS | Per-agent event storage |
| IPatternStore, ITestFailureStore | Long-term query | EXISTS | Pattern and failure storage |
| Episodic | - | MISSING | No explicit episodic memory component for composition |

### Reporting

| Component | Type | Status | Notes |
|-----------|------|--------|-------|
| ChangelogGenerator | Human-readable | EXISTS | Changelog from adaptations |
| DocumentationUpdater | Human-readable | EXISTS | Brick docs |
| IAdaptationAuditLog | Audit log | EXISTS | Adaptation audit |
| Structured output | - | PARTIAL | BrickOutput, ToolResult |
| Suggestion surface | - | MISSING | No component that surfaces suggestions to user |

---

## BrickCategory Mapping

Current `BrickCategory` enum: Input, Output, Analysis, Transform, Generation, Validation, Security, Control.

| BrickCategory | Maps to Family |
|----------------|----------------|
| Input | Perception |
| Output | Reporting |
| Analysis | Reasoning |
| Transform | Action / Reasoning |
| Generation | Action |
| Validation | Reasoning |
| Security | Perception (OWASP) / Reasoning |
| Control | Action |

---

## Gaps Summary

| Family | Coverage | Missing |
|--------|----------|---------|
| Perception | ~50% | Vision, Audio, explicit Data Parsing component |
| Action | ~60% | UI Interaction, general Process Control |
| Reasoning | ~70% | Explicit Comparison, Classification, Planning bricks |
| Memory | ~60% | Episodic memory for composition |
| Reporting | ~65% | Suggestion surface component |

---

## Recommendations

1. **Placeholder descriptors:** Add `ComponentDescriptor` entries in `CapabilityComponentRegistry` for missing capability strings (e.g. `vision-input`, `audio-input`, `ui-interaction`, `episodic-memory`, `suggestion-surface`) with stub implementation types or "TBD" — enables composition engine to discover required capabilities even before implementations exist.

2. **Priority for novel composition:** Perception (Vision, Data Parsing) and Action (Code Generation) have the most coverage. Memory (Episodic) and Reporting (Suggestion surface) are the largest gaps for ambient intelligence scenarios.

3. **Integration:** Ensure `BrickRegistry` and `CapabilityComponentRegistry` stay aligned so bricks registered via SDK appear in composition queries.
