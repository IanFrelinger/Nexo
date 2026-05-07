# Architecture overview

High-level maps of how Nexo is structured. For day-to-day commands, see the repository root `README.md` and `CONTRIBUTING.md`.

| Document | Contents |
| -------- | -------- |
| [Trust and execution boundaries](TrustAndExecutionBoundaries.md) | Where trust is decided, how requests cross layers, and what runs locally vs. on peers. |
| [Testing model](TestingModel.md) | Relationship between xUnit tests, `UnitTestBase` / `ITestRunner`, and CI. |
| [.NET SDK and target frameworks](DotnetVersions.md) | Why `global.json` pins SDK 9.x while many libraries target `net8.0`. |
| [Forge map adaptation](forge-map-adaptation.md) | `MapAdaptationPlanner`, dry-run pipeline, engine manifest JSON, and Forge persistence options. |
| [Forge map host integration](forge-map-host-integration.md) | Milestones M1–M4: manifest, tiles, hints, LOD pyramid (`GET /map/tile-pyramid`), sample host project. |
