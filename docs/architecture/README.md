# Architecture overview

High-level maps of how Nexo is structured. For day-to-day commands, see the repository root `README.md` and `CONTRIBUTING.md`.

| Document | Contents |
| -------- | -------- |
| [Trust and execution boundaries](TrustAndExecutionBoundaries.md) | Where trust is decided, how requests cross layers, and what runs locally vs. on peers. |
| [Testing model](TestingModel.md) | Relationship between xUnit tests, `UnitTestBase` / `ITestRunner`, and CI. |
| [Kernel phase matrix](KernelPhaseMatrix.md) | `NexoKernelRegistrar` phases, module flags, and `make kernel-gate` proof. |
| [.NET SDK and target frameworks](DotnetVersions.md) | Why `global.json` pins SDK 9.x while many libraries target `net8.0`. |
| [Runtime vs application layout](runtime-vs-application.md) | `src/` kernel vs `application/src/` hosts; `Nexo.Runtime.sln`, `Nexo.Runtime.Bundle`, NuGet metapackages, and packing scripts. |
| [Protocol integration: MCP + A2A](ProtocolIntegration-MCP-A2A.md) | MCP server bridge over `ITool` (allowlists, policy gate, stdio host) and the planned MCP client / A2A phases. |
| [Shipping and consumption (all audiences)](../DistributionModels.md) | How Nexo is packaged (NuGet, containers, HTTP); pinning; **distribution-matrix** CI jobs per channel. |
| [Forge map adaptation](forge-map-adaptation.md) | `MapAdaptationPlanner`, dry-run pipeline, engine manifest JSON, and Forge persistence options. |
| [Forge map host integration](forge-map-host-integration.md) | Milestones M1–M6; terrain payload summaries; optional material **`IModel`** augmentation; tile cache; Unity/Godot package layouts. |
| [Aesthetic and engine adaptation](aesthetic-engine-adaptation.md) | Cross-engine `AestheticPack` fields, validation, Forge `apply-pack`, and shared Mapbox tile helpers. |
| [SDK-style layout](SdkStructure.md) | Ports vs options vs builders; `Nexo.Hosting.Sdk` vs `Nexo.Sdk.Client`; folder conventions. |
| [SDK migration plan (remaining gaps)](SdkMigrationPlan.md) | **Execution status** at top; **[Plan: close remaining gaps](#plan-close-remaining-gaps-post-migration)** (D1–D6: docs, sweep, consumers, optional `Sdk/Options`, hosting polish, CI clarity). |
| **`Nexo.Framework.Sdk`** | Optional megaproject in `src/Nexo.Framework.Sdk/` — `AddNexoFramework` combines HTTP client + `AddNexo`. |
| [GitHub Actions trigger policy](../../.github/workflows/README.md) | Manual-first workflow policy (`workflow_dispatch`); tag-driven release automation unchanged. |
