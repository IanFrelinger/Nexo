# Gap Analysis: Current State vs North Star

**North Star:** Every capability must be used by Nexo on itself. Each block has a dogfood gate that must pass before moving on.

**Reference:** [DogfoodValidation.md](DogfoodValidation.md)

---

## Executive Summary

| Area | Status | Gap Severity |
|------|--------|---------------|
| Dogfood gates (Blocks 1–9 + Phase F) | All implemented in tests; `make dogfood-all` passes | Low |
| CLI dogfood parity | `nexo dogfood` exposes `block1`–`block9`, `closedloop`, `phasef`, `all` | Resolved |
| Observe → Improve integration | Observe and improve are separate; patterns don't drive analysis | Medium |
| Test failure → adaptation trigger | Self-improvement loop exists; test failure ingestion unclear | Low |
| Trust in improve flow | Trust phases 1–4 implemented; improve may not use sanitization | Low |
| Documentation & discoverability | Changelog, dogfood, improve flows under-documented | Low |

---

## 1. Dogfood Gates (Blocks 1–9 + Phase F)

### Current State
- All 9 blocks and Phase F (closed loop, changelog, test failure store) have passing tests.
- `make dogfood-all` runs Phase C (blocks 1–6), Phase D+E (blocks 7–9), closed loop, and Phase F.
- CI supports `scope=dogfood` in Cross-Platform Tests workflow.
- `nexo dogfood` exposes `block1`–`block9`, `closedloop`, `phasef`, and `all`.

### Gap
- **No functional parity gap.** CLI and test runner both cover all dogfood gates.
- **Discoverability gap:** users still need clearer docs on when to use `nexo dogfood ...` vs `make dogfood-*`.

### Recommendation
Keep CLI parity marked as resolved; prioritize documentation examples that map each `nexo dogfood` command to the equivalent `make` and test-filter paths.

---

## 2. Observe → Analyze → Adapt Loop

### Current State
- **Observe:** `nexo observe` watches file system and processes, detects patterns (e.g. `repeated-edits`), stores in `nexo-patterns.db`.
- **Improve:** `nexo improve` runs `IBrickStaticAnalyzer` on a path (default: Block 1 Observation path), then adapts from violations.
- **Self-context:** `nexo self-context` assembles adaptations, executions, and patterns for "what did I change in 24h?"
- **Full pipeline E2E:** `observe` → `analyze bricks` → `adapt` → `improve` → `self-context` run sequentially in tests.

### Gap
- **Improve does not use observation patterns.** It analyzes a fixed path (`FindBlock1ObservationPath`) or user-specified `--path`. Observation patterns (e.g. repeated-edits on a file) do not drive which files are prioritized or selected for analysis.
- **No single "observe → improve" command.** Users must run `observe` and `improve` separately. The doc says "Dogfood: observe → analyze → adapt" but the improve flow is effectively "analyze → adapt."

### Recommendation
- Option A: Add `nexo improve --from-observation` to query recent patterns and prioritize analysis on frequently edited paths.
- Option B: Document that the intended flow is sequential (`observe` then `improve`) and that patterns feed self-context, not the improve path selection.
- Option C: Add a `nexo loop` or `nexo improve --continuous` that runs observe + improve in one process (e.g. observe for N minutes, then run improve on observed paths).

---

## 3. Self-Improvement Loop (Test Failures → Adaptation)

### Current State
- `nexo improve --self` runs `ISelfImprovementLoop.RunOnceAsync()`.
- Loop reads from `ITestFailureStore`, processes failures, applies fixes, validates, promotes.
- `DogfoodPhaseFTests` validates `TestFailureStore_RecordAndQuery_ReturnsStoredFailures`.

### Gap
- **Test failure ingestion:** It is unclear how test failures are written to `ITestFailureStore` in production. The test seeds the store directly. A test runner integration (e.g. xUnit/dotnet test result listener) that records failures into the store may be missing or not documented.
- **Background scheduling:** The self-improvement loop is run on-demand via `nexo improve --self`. A scheduled background agent that runs this loop periodically is not clearly documented.

### Recommendation
- Document or implement the path from test execution (e.g. `nexo test local`, CI) to `ITestFailureStore`.
- If a background agent exists for self-improvement, document it in README/GettingStarted.

---

## 4. Trust & Information Architecture in Improve Flow

### Current State
- Trust phases 1–4 implemented: classification, sanitization, access boundary, audit dashboard.
- `SanitizingProviderFactory` wraps `IProviderFactory` when Trust is enabled via hosting registration (`AddNexo`).
- `ImproveCommand` builds a lightweight service collection and registers `ProviderFactory` directly.
- Default `FixGenerator` paths are rule-based; cloud LLM usage in improve is optional and configuration-dependent.

### Gap
- **Trust wiring gap risk:** `ImproveCommand` does not inherit hosting-level Trust registration by default.
- If future improve paths send prompts to cloud providers, they must explicitly include Trust sanitization registration in the CLI-local DI graph.

### Recommendation
- Document the current improve wiring (local DI, default rule-based fix generation).
- Add a focused test that validates Trust sanitization is active when improve is configured to use cloud-backed fix generation.

---

## 5. Composition & Mesh (Blocks 7–9)

### Current State
- Block 7: `ICompositionEngine.ComposeAsync("test Nexo CLI")` returns a pipeline.
- Block 8: Parallel test matrix + composed test runner.
- Block 9: Instance mesh advertise/discover; local IPC between two instances.

### Gap
- **Production use:** These are validated in tests. It is unclear if `nexo compose` and `nexo mesh` are used in real workflows (e.g. CI, local dev) or only for validation.
- **Discoverability:** README lists `nexo compose` and `nexo mesh` but does not explain when to use them or how they fit the North Star.

### Recommendation
- Add a "Dogfood workflows" section to README: e.g. "Run `nexo compose test nexo-cli` to compose a test agent for Nexo CLI."
- Consider a `nexo dogfood full` that runs observe → improve → self-context → changelog as a single workflow.

---

## 6. Documentation & Discoverability

### Current State
- README lists CLI commands including `nexo changelog`, `nexo dogfood`, `nexo improve`.
- DogfoodValidation.md documents gates and validation commands.
- GettingStarted.md does not mention dogfood, changelog, or the improve flow.

### Gap
- New users may not discover `nexo changelog --since 7d` or `make dogfood-all`.
- The North Star principle ("every capability used by Nexo on itself") is not prominent in README.

### Recommendation
- Add a "North Star & Dogfood" subsection to README with links to DogfoodValidation.md and `make dogfood-all`.
- Add "Changelog from promoted changes" to GettingStarted: `nexo changelog --since 7d`.

---

## 7. Summary: Priority Order

| Priority | Gap | Effort | Impact |
|----------|-----|--------|--------|
| 1 | Observe → improve integration (pattern-driven analysis) | High | Medium – completes "observe → analyze → adapt" |
| 2 | Trust in improve flow (sanitization when LLM used) | Low | High – security/compliance |
| 3 | Test failure ingestion path documentation | Low | Medium – enables self-improvement in production |
| 4 | Documentation (North Star, changelog, dogfood in README/GettingStarted) | Low | Medium – discoverability |

---

## Appendix: What’s Working Well

- All dogfood gates pass; CI supports dogfood scope.
- Observe, adapt, improve, self-context, changelog, compose, mesh are implemented and usable.
- Trust & Information Architecture phases 1–4 are implemented.
- Full pipeline E2E runs observe → analyze → adapt → improve → self-context.
- `nexo improve --self` runs the self-improvement loop from test failures.
- Cross-platform testing (Docker, portable, multi-env) is in place.
