# LinkedIn — distribution channels (copy/paste)

Short copy you can adapt. Highlights **multiple distribution surfaces** (NuGet, HTTP, CLI/containers, Compose, mesh) rather than a single install story.

## Short

Nexo ships as **NuGet embeds**, a **hosted HTTP API**, **CLI + container images**, **Compose stacks**, and **federated mesh peers**—same release train, different surfaces, each path covered by CI. If your roadmap says self-hosted AI with real operator choice, that is the shape we are building.

## Medium

We are hardening Nexo as a platform you can **actually deliver**: not just a library, but **distribution**—**NuGet** for .NET hosts and typed **HTTP clients**, **GHCR images** for headless automation, **Compose** for operators, and **mesh** for trusted federation. CI runs a **distribution matrix** so those channels stay green together, not “green on my laptop.”

## One-liner

One AI platform, **many distribution channels**—NuGet, API, CLI/containers, Compose, mesh—so customers deploy Nexo where their stack already lives.

## Reference (repo)

- **`docs/DistributionModels.md`** — channel table, golden pins, CI mapping.
- **`.github/workflows/distribution-matrix-gate.yml`** — parallel gates per channel.
