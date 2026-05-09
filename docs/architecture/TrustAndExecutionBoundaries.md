# Trust and execution boundaries

Nexo separates **policy and trust** from **transport and UI**. The following boundaries are the usual mental model when reading the codebase.

## Layers

1. **Domain (`Nexo.Core.Domain`)** — Entities, value objects, and domain rules. No knowledge of HTTP, gRPC, or hosting.
2. **Application (`Nexo.Core.Application`)** — Use cases, ports (interfaces), and orchestration contracts. Mediates between domain and the outside world without binding to a specific host.
3. **Infrastructure (`Nexo.Infrastructure`)** — Adapters: persistence, `ITestRunner` (reflection-based test discovery), external tools, and similar implementations of application ports.
4. **Hosting (`Nexo.Hosting`, `Nexo.API`)** — Composition root for processes: dependency injection, HTTP APIs, and service registration (including `ITestRunner` → `TestRunnerAdapter`).

## Trust and data flow

- **Portal and API** surface operator and developer actions; they should not bypass application use cases for privileged operations.
- **Background agents** and **mesh** features route work according to configured trust tiers and policies (see product docs under `docs/` for operator-facing detail).
- **Air-gapped and no-network CI** workflows validate that selected paths do not assume outbound network access; they complement but do not replace a full threat model review for your deployment.

For concrete configuration keys and environment variables, use `docs/Configuration.md` (repository root `docs/`).
