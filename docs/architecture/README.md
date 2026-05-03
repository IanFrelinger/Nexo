# Architecture overview

High-level maps of how Nexo is structured. For day-to-day commands, see the repository root `README.md` and `CONTRIBUTING.md`.

| Document | Contents |
| -------- | -------- |
| [Trust and execution boundaries](TrustAndExecutionBoundaries.md) | Where trust is decided, how requests cross layers, and what runs locally vs. on peers. |
| [Testing model](TestingModel.md) | Relationship between xUnit tests, `UnitTestBase` / `ITestRunner`, and CI. |
| [Aesthetic and engine adaptation](aesthetic-engine-adaptation.md) | Cross-engine `AestheticPack` fields, validation, Forge `apply-pack`, and shared Mapbox tile helpers. |
| [GitHub Actions trigger policy](../../.github/workflows/README.md) | Manual-first workflow policy (`workflow_dispatch`); tag-driven release automation unchanged. |
