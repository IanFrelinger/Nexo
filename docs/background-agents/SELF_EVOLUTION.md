# Self-Evolution: Configuring Agents for Self-Extending, Self-Testing, and Self-Documenting

This document describes how to configure embedded background agents so the framework can **continuously evolve** after deployment and at runtime: self-extending (code changes), self-testing, and self-documenting.

## What You Can Do Today (Config-Only)

With the current codebase you can configure agents that **dog-food** the framework:

| Capability      | Role        | What runs                         | Config / Host requirement                          |
|-----------------|-------------|------------------------------------|----------------------------------------------------|
| **Self-testing**   | `tester`    | Framework test pipeline (`nexo test`) | Optional `Parameters.Filter`. Host registers `ITestRunRunner`. |
| **Self-analyzing** | `optimizer` | Code analysis (`nexo analyze`)     | `Parameters.Path` or `Parameters.AnalysisPath`. Host registers `ICodeAnalysisRunner`. |
| **Self-extending** | `extender`  | LLM + tools (write, search/replace) | `Parameters.RepoRoot` or `Parameters.Path`. Host registers `ISelfExtendRunner`. Policy: path allowlist, max write size. |

- **Self-testing**: Use [examples/dogfood-tester.json](examples/dogfood-tester.json). The agent runs the app's tests on a schedule and results are logged.
- **Self-analyzing**: Use [examples/dogfood-optimizer.json](examples/dogfood-optimizer.json). The agent runs the app's analysis on a path on a schedule; violations and summary are logged.
- **Self-extending**: Use [examples/dogfood-extender.json](examples/dogfood-extender.json). The agent runs one ThinkAsync cycle: the LLM (IModel) returns tool_calls; only policy-approved calls (PathAllowlist, MaxWriteSize) are executed. Writes are restricted to `src/` and `tests/`.

You can run **all three** in the same host; the CLI registers all three runners.

## What's Not Yet Config-Only (Self-Documenting)

1. **Self-extending** is now supported via the **extender** role: the registry calls **ISelfExtendRunner**, which runs a **ToolCallingAgent** (LLM) with a toolbox (repo.fs.write, repo.fs.search_replace) and path/max-size policy. See [DOGFOOD_EXTENDER.md](DOGFOOD_EXTENDER.md).

2. **Self-documenting**
   - RAG and knowledge-base indexing already support **reading** the codebase.
   - **Writing** documentation (e.g. to `docs/generated/`) would need tools (e.g. `write_doc_file`, `update_readme`) and policy (e.g. only under `docs/generated/` or with approval). The same ThinkAsync + toolbox pattern used by the extender can be reused; add a **documenter** role and an **IDocumentationRunner** (or extend the extender toolbox with doc-writing tools and path policy) when ready.

## Summary

- **Today**: You can configure embedded agents for **self-testing** (tester), **self-analyzing** (optimizer), and **self-extending** (extender). The extender runs an LLM-backed tool-calling agent with repo.fs.write and repo.fs.search_replace; path allowlist and max write size policies restrict where and how large writes can be.
- **Next steps**: Add a **documenter** role (or doc-writing tools + policy) for self-documenting; optionally add more tools (e.g. read_file) and stricter policies (e.g. human-in-the-loop) as needed.
