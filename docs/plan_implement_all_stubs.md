# Plan: Fully Implement All Stubs

## Stub Inventory

| Location | Stub | Type |
|----------|------|------|
| [WorkflowExecutor.cs](src/Nexo.Core.Application/Workflows/WorkflowExecutor.cs) | ExecuteClusterNodeAsync | Node execution |
| [WorkflowExecutor.cs](src/Nexo.Core.Application/Workflows/WorkflowExecutor.cs) | ExecuteTransformNode | Node execution |
| [WorkflowExecutor.cs](src/Nexo.Core.Application/Workflows/WorkflowExecutor.cs) | ExecuteConditionalNode | Node execution |
| [WorkflowExecutor.cs](src/Nexo.Core.Application/Workflows/WorkflowExecutor.cs) | SerializeOutput (Xml, Csv, Markdown, Html, Pdf) | Output format |
| [LocalImageGenerator.cs](src/Nexo.Adapters.Assets/Images/LocalImageGenerator.cs) | GenerateOllamaAsync | Image generation |
| [LocalImageGenerator.cs](src/Nexo.Adapters.Assets/Images/LocalImageGenerator.cs) | GenerateLocalAIVariationsAsync | Image variations |
| [CodeGenerator.cs](src/Nexo.Infrastructure/Export/CodeGenerator.cs) | Fallback `_ => $"// Stub for {brick.Name}"` | Code generation |
| [StubLocalTransport](src/Nexo.Infrastructure/Mesh/StubLocalTransport.cs) | No-op transport | MVP placeholder |
| [StubCapabilityRequester](src/Nexo.Infrastructure/Mesh/StubCapabilityRequester.cs) | Returns null | MVP placeholder |

---

## Phase 1: WorkflowExecutor Output Serialization

**File:** `src/Nexo.Core.Application/Workflows/WorkflowExecutor.cs`

**Current:** `SerializeOutput` throws for Xml, Csv, Markdown, Html, Pdf.

**Implementation:**

- **XML:** Use `System.Xml.Linq` (XDocument, XElement) or `System.Text.Json` + manual XML build. For dictionary/list: emit `<root><item key="x">value</item></root>`.
- **CSV:** Use `CsvHelper` (in Directory.Packages.props) or manual: if `IEnumerable<Dictionary>`, write header from keys, then rows. Single object: flatten to key-value rows.
- **Markdown:** Convert to markdown table or list. Dictionary → `| Key | Value |`; list of dicts → table with columns from first row keys.
- **HTML:** Wrap in `<table>` or `<pre>`. Simple: `$"<pre>{JsonSerializer.Serialize(data)}</pre>"` or build table.
- **PDF:** Add `QuestPDF` or throw `NotSupportedException("PDF export not implemented; use Json, Xml, or Markdown")`.

---

## Phase 2: WorkflowExecutor TransformNode

**File:** `src/Nexo.Core.Application/Workflows/WorkflowExecutor.cs`

**Domain:** TransformNode has `Operation` (Map, Filter, Reduce, Sort, GroupBy, Merge) and `Expression` (string).

**Implementation:**

- **Map:** Extract property via Expression (e.g. `"value"`) or pass-through. Support `IEnumerable` + projection.
- **Filter:** Expression as `"key > 5"` or `"key == 'x'"`. Parse and evaluate against each item (reflection or IDictionary).
- **Reduce:** Expression = `"sum"`, `"count"`, `"first"`, `"last"`. Aggregate.
- **Sort:** Expression = property name. `OrderBy`/`OrderByDescending`.
- **GroupBy:** Expression = property name. `GroupBy`.
- **Merge:** Concatenate lists.

Use `System.Linq.Dynamic.Core` if available, or a minimal custom evaluator for `key`, `key op value`.

---

## Phase 3: WorkflowExecutor ConditionalNode

**File:** `src/Nexo.Core.Application/Workflows/WorkflowExecutor.cs`

**Implementation:**

- Parse `Condition` (e.g. `"data.count > 0"`, `"result == 'ok'"`).
- Evaluate against `inputs` using reflection/dictionary access.
- Return `NodeResult` with `Outputs["condition"] = true/false` and `Outputs["result"] = inputs` for downstream routing.

---

## Phase 4: WorkflowExecutor ClusterNode

**File:** `src/Nexo.Core.Application/Workflows/WorkflowExecutor.cs`

**Implementation:**

- Add `IClusterStore` port with `GetByIdAsync(string id)` returning `Cluster?`.
- Implement `IClusterStore` in Infrastructure (in-memory or config-based).
- In `ExecuteClusterNodeAsync`: resolve cluster, build sub-graph from Bricks+Connections, execute in topological order, map Parameters to brick inputs, aggregate outputs.

---

## Phase 5: LocalImageGenerator — Ollama

**File:** `src/Nexo.Adapters.Assets/Images/LocalImageGenerator.cs`

**Implementation:**

- Ollama image models (llava, etc.) return different response format. Check Ollama API: may use `api/generate` with base64 in response, or `api/show` for model info.
- Parse response, extract image bytes, write to temp file, return `GeneratedImage`.
- Handle non-image models with clear error.

---

## Phase 6: LocalImageGenerator — LocalAI Variations

**File:** `src/Nexo.Adapters.Assets/Images/LocalImageGenerator.cs`

**Implementation:**

- Use LocalAI `v1/images/variations` or `v1/images/edits` (OpenAI-compatible).
- POST multipart with image file, `n` for count.
- Parse response, return `IReadOnlyList<GeneratedImage>`.

---

## Phase 7: CodeGenerator Fallback

**File:** `src/Nexo.Infrastructure/Export/CodeGenerator.cs`

**Implementation:**

- For unknown `ExportTarget`, generate a generic stub (e.g. JSON block or C# comment block) instead of `// Stub for {brick.Name}`.
- Add cases for any additional targets in the enum.

---

## Phase 8: Mesh Stubs (Optional)

Keep `StubLocalTransport` and `StubCapabilityRequester` as-is; document as MVP placeholders. Full implementation would require real transport (e.g. file, gRPC) and capability registry.

---

## Implementation Order

1. Phase 1 — SerializeOutput (Xml, Csv, Markdown, Html; PDF = NotSupported)
2. Phase 7 — CodeGenerator fallback
3. Phase 2 — TransformNode
4. Phase 3 — ConditionalNode
5. Phase 4 — ClusterNode (add IClusterStore)
6. Phase 5 — Ollama image generation
7. Phase 6 — LocalAI variations

---

## Verification

- Add WorkflowExecutorSmokeTests for Transform, Conditional, output formats
- `dotnet build Nexo.sln` and `dotnet test` pass
