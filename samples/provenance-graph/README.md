# Provenance graph sample bundle

Input fixture for the read-only certification provenance projection in [`applications/Ashlar.Provenance.Graph/`](../../applications/Ashlar.Provenance.Graph/README.md). It is one signed physical-atom certified bundle whose certificate carries the `ashlar.provenance.v1` extension claim, i.e. the smallest input from which the projector can derive `Artifact -CERTIFIED_BY-> Certificate -ISSUED_UNDER-> PolicyVersion` and `Artifact -PRODUCED_BY-> Agent` and answer `ArtifactsUnderPolicy`.

## `demo.bundle.json`

`PhysicalAtomBundleLoader` picks up every `*.bundle.json` under a directory, so this file name matters. Contents:

| Field | Value | Meaning |
|-------|-------|---------|
| `certificate.bindingScope` | `Design` | Phase 1 design-scope certificate (`schemaVersion` 1, `maturity` Prototype). |
| `assetBytesBase64` | `ashlar-provenance-demo-asset-v1` (UTF-8) | The bound asset; SHA-256 = `8f168a714d1b9833b60055ba3d3b0da110198c5672a1ec73e4baf52c126a02e6` = `certificate.assetHash` = the graph `Artifact` id. |
| `certificate.extensions["ashlar.provenance.v1"]` | base64 JSON | Signed graph claims: `artifactKind: atom`, `policyName: SelfProducedBrickCertificationPolicy`, `policyVersion: 1.0.0`, `producerAgentId: ashlar-demo-agent`, `producerAgentKind: self`, `dependsOnArtifactIds: []`, `issuedAt: 2026-07-13T12:00:00+00:00`. |
| `issuerPublicKeyBase64` / `certificate.issuerSignature` | Ed25519 | Sample issuer key (documentation and CI only, not production PKI). |

Because the claims live inside the signed certificate, editing any of them invalidates the signature and the projector rejects the batch; that is the point.

## How it is used

**Unit test (CI, no Docker):** `applications/Ashlar.Provenance.Graph.Tests/Unit/ProvenanceSourceAdapterTests.cs` copies this directory into its output (`Content Include="../../samples/provenance-graph/**/*"` in the test csproj), projects it through the in-memory store, and asserts `AcceptedCount == 1`, no rejections, and that `ArtifactsUnderPolicy("SelfProducedBrickCertificationPolicy", "1.0.0")` returns exactly the artifact id above. This is the `unit-tests` job of `.github/workflows/provenance-graph-gate.yml`. Reproduce from the repository root:

```bash
dotnet test applications/Ashlar.Provenance.Graph.Tests/Ashlar.Provenance.Graph.Tests.csproj --filter "Category!=Integration"
```

**Neo4j demo (Docker):** `tools/Ashlar.Provenance.Demo` reads `samples/provenance-graph/` (path resolved from the repo root passed as its first argument, or found by walking up to `Ashlar.sln`), projects it into a Neo4j instance and runs the `ArtifactsUnderPolicy` query. One command, from the repository root, with Docker running:

```bash
bash scripts/demo-provenance-graph.sh
```

That starts Neo4j via `deploy/compose/docker-compose.provenance.yml` (bolt on `localhost:7687`, user `neo4j`, password `provenance-graph`; override with `NEO4J_URI` / `NEO4J_USERNAME` / `NEO4J_PASSWORD`), waits for the port, then runs `dotnet run --project tools/Ashlar.Provenance.Demo/Ashlar.Provenance.Demo.csproj -- "$ROOT"`. If Neo4j is already up, `bash scripts/project-provenance-graph.sh` skips the compose step. Expected tail of the output:

```
=== ArtifactsUnderPolicy Demo ===
Policy:     SelfProducedBrickCertificationPolicy@1.0.0
Chain head: <chain head hash of the projected batch>
Artifacts (1):
  - 8f168a714d1b9833b60055ba3d3b0da110198c5672a1ec73e4baf52c126a02e6
```

Exit code `0`; `1` if no `*.bundle.json` was found; `2` if the projector rejected anything.

## Adding bundles

Drop further `*.bundle.json` files here to grow the graph. Each must be signed by an issuer key the loader can verify, and any `dependsOnArtifactIds` / `priorCertificateHash` it claims must resolve inside the same batch, otherwise the whole batch is rejected before any write (see "Trust and edge authority" in the application README).
