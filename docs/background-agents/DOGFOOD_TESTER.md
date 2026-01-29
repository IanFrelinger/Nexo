# Dog-food: Self-testing background agent

Background agents with **Role: `tester`** run the framework's own test pipeline on a schedule. The host (CLI) supplies `ITestRunRunner` (implemented by `TestRunRunnerAdapter`), which invokes `RunTestsCommand` / `ITestRunner`—the same pipeline used by `nexo test`.

## Configuration

- **Role**: `tester`
- **Parameters** (optional):
  - **Filter**: test category or name filter (same semantics as `nexo test --filter`). Omit to run all tests.

## Example

See [examples/dogfood-tester.json](examples/dogfood-tester.json): a single agent that runs all tests every 30 minutes. To run a subset of tests, add `"Parameters": { "Filter": "CategoryName" }`.

## Enabling in the host

When using the Nexo CLI, `ITestRunRunner` is registered automatically and tester agents are executed when the registry runs. For a custom host, register an implementation of `ITestRunRunner` that calls your test pipeline and ensure the registry is built with that implementation (e.g. via `AddBackgroundAgents` and the same pattern as `ICodeAnalysisRunner`).

## Relation to self-evolution

Together with the **optimizer** role (code analysis), the **tester** role supports **self-testing** in a post-deployment or runtime loop: the framework can periodically run its own tests and log results. Full autonomous evolution (self-extending code, self-documenting) still requires additional tools and the LLM-driven execution path; see [SELF_EVOLUTION.md](SELF_EVOLUTION.md).
