# Component Library

**Purpose:** Coverage table for the five North Star component type families required for runtime agent composition. See [SeedComponentLibraryAudit.md](SeedComponentLibraryAudit.md) for detailed analysis.

---

## Coverage Table

| Component Type | Status | Implementing Class |
|----------------|--------|-------------------|
| **Perception** | | |
| Code Analysis | exists | RoslynBrickStaticAnalyzer, OWASPScannerBrick, code-analysis (CapabilityComponentRegistry) |
| Text/NLP | partial | Implicit in analyzers |
| Data Parsing | partial | TrxTestResultParser, etc. |
| Vision (image input) | stub | vision-input (CapabilityComponentRegistry placeholder) |
| Audio (audio input) | stub | audio-input (CapabilityComponentRegistry placeholder) |
| **Action** | | |
| Code Generation | exists | RepoFsWriteTool, RepoFsSearchReplaceTool |
| File System | exists | RepoFsToolboxFactory |
| API Call | exists | RemoteBrick |
| Process Control | partial | Test runner spawns processes |
| UI Interaction | stub | ui-interaction (CapabilityComponentRegistry placeholder) |
| **Reasoning** | | |
| Planning | partial | Composition engine composes |
| Validation | exists | RoslynBrickStaticAnalyzer, DotNetRegressionTestRunner |
| Classification | partial | Violation classification |
| Comparison | partial | Implicit in analyzers |
| Summarization | partial | ChangelogGenerator |
| **Memory** | | |
| Short-term context | exists | IAgentMemory (toolbox) |
| Long-term query (RAG) | exists | IPatternStore, ITestFailureStore |
| Episodic recall | stub | episodic-memory (CapabilityComponentRegistry placeholder) |
| **Reporting** | | |
| Structured output | partial | BrickOutput, ToolResult |
| Human-readable summary | exists | ChangelogGenerator, DocumentationUpdater |
| Audit log entry | exists | IAdaptationAuditLog |
| Suggestion surface | stub | suggestion-surface (CapabilityComponentRegistry placeholder) |

---

## Gaps That Block High-Value Compositions

- **Vision:** UI testing, screenshot analysis. Stub in place; implement with SmolVLM2 or equivalent.
- **UI Interaction:** Web agent automation. Stub in place.
- **Short-term context:** Multi-step reasoning chains. IAgentMemory exists; composition engine integration may need enhancement.
- **Episodic memory:** Long-horizon task recall. Stub in place.

---

## Stub Components

Placeholder descriptors in `CapabilityComponentRegistry.SeedPlaceholderComponents()` ensure `nexo compose` does not fail with "no component found" for any family. Stubs use `ImplementationType = "TBD"` until real implementations exist.
