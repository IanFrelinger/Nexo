# Architecture overview

High-level maps of how Nexo is structured. For day-to-day commands, see the repository root `README.md` and `CONTRIBUTING.md`.

| Document | Contents |
| -------- | -------- |
| [Trust and execution boundaries](TrustAndExecutionBoundaries.md) | Where trust is decided, how requests cross layers, and what runs locally vs. on peers. |
| [Testing model](TestingModel.md) | Relationship between xUnit tests, `UnitTestBase` / `ITestRunner`, and CI. |
| [.NET SDK and target frameworks](DotnetVersions.md) | Why `global.json` pins SDK 9.x while many libraries target `net8.0`. |
| [Runtime vs application repositories](runtime-vs-application.md) | Embeddable kernel vs product surfaces; `Nexo.Runtime.sln`, `Nexo.Runtime.Bundle`, packing scripts. |
| [Forge map adaptation](forge-map-adaptation.md) | `MapAdaptationPlanner`, dry-run pipeline, engine manifest JSON, and Forge persistence options. |
| [Forge map host integration](forge-map-host-integration.md) | Milestones M1–M6; terrain payload summaries; optional material **`IModel`** augmentation; tile cache; Unity/Godot package layouts. |
| [Aesthetic and engine adaptation](aesthetic-engine-adaptation.md) | Cross-engine `AestheticPack` fields, validation, Forge `apply-pack`, and shared Mapbox tile helpers. |
