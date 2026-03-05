# Gap Analysis: Current State vs North Star

**North Star:** Every capability must be used by Nexo on itself. Each block has a dogfood gate that must pass before moving on.

**Reference:** [DogfoodValidation.md](DogfoodValidation.md)

---

## Executive Summary

| Area | Status | Gap Severity |
|------|--------|---------------|
| Dogfood gates (Blocks 1–9 + Phase F) | All implemented in tests; `make dogfood-all` passes | Low |
| CLI dogfood parity | Only `nexo dogfood block1` exposed; blocks 2–9 test-only | Medium |
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

### Gap
- **CLI parity:** `nexo dogfood` only exposes `block1`. Blocks 2–9, closed loop, and Phase F are validated only via `dotnet test` / `make dogfood-*`.
- **North Star expectation:** Users should be able to run each gate from the CLI for quick validation without invoking the test runner.

### Recommendation
Add `nexo dogfood block2` through `nexo dogfood block9`, `nexo dogfood closedloop`, and `nexo dogfood phasef` subcommands that invoke the same logic as the tests (or delegate to test runner with a filter).

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
- `SanitizingProviderFactory` wraps `IProviderFactory` when Trust is enabled.
- `ImproveCommand` uses `ProviderFactory` (and fix generation may call LLM). Trust/sanitization is applied when the host is configured with `NEXO_TRUST_ENABLED` and `AddNexo` Trust options.

### Gap
- **ImproveCommand builds its own `ServiceProvider`** with `AddCodeAnalyzers`, `AddAdaptationInfrastructure`, etc. It does not use `AddNexo()` from Hosting. Whether `SanitizingProviderFactory` is registered in this light-weight service setup is unclear.
- If fix generation uses LLM and Trust is not wired in the improve flow, prompts could reach cloud without sanitization when running `nexo improve`.

### Recommendation
- Audit `ImproveCommand` service registration: ensure `SanitizingProviderFactory` is used when Trust is enabled.
- Add a dogfood test that runs improve with Trust enabled and asserts sanitization/audit behavior.

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
| 1 | CLI dogfood parity (blocks 2–9, closedloop, phasef) | Medium | High – aligns CLI with North Star gates |
| 2 | Observe → improve integration (pattern-driven analysis) | High | Medium – completes "observe → analyze → adapt" |
| 3 | Trust in improve flow (sanitization when LLM used) | Low | High – security/compliance |
| 4 | Test failure ingestion path documentation | Low | Medium – enables self-improvement in production |
| 5 | Documentation (North Star, changelog, dogfood in README/GettingStarted) | Low | Medium – discoverability |

---

## Appendix: What’s Working Well

- All dogfood gates pass; CI supports dogfood scope.
- Observe, adapt, improve, self-context, changelog, compose, mesh are implemented and usable.
- Trust & Information Architecture phases 1–4 are implemented.
- Full pipeline E2E runs observe → analyze → adapt → improve → self-context.
- `nexo improve --self` runs the self-improvement loop from test failures.
- Cross-platform testing (Docker, portable, multi-env) is in place.
