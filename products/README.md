# Extractable Ashlar products

These trees are **applications** that consume the Ashlar framework. They live in this
monorepo until they are extracted to their own repositories. The one-way rule is
absolute:

```text
ashlar-cloud  →  ashlar-cluster  →  ashlar (src/)
ashlar-workstation  →  ashlar
ashlar-native  →  ashlar
```

The kernel under `src/` must never reference anything in `products/`. Products must
never reference `commercial/` from this scaffold (Fleet stays commercial until a
later extraction). Do not create the future GitHub repositories until extraction;
grow the trees here first (see [`docs/architecture/product-split.md`](../docs/architecture/product-split.md)).

| Tree | Future repo | Role |
|------|-------------|------|
| `ashlar-workstation/` | `ashlar-workstation` | Offline IDE daemon using `SecureWorkstation` (not the `AirGapped` profile) |
| `ashlar-cluster/` | `ashlar-cluster` | Cluster scheduler implementing `ITaskScheduler` |
| `ashlar-cloud/` | `ashlar-cloud` | Hosted control-plane stubs (orgs, billing, quotas); OIDC planned |
| `ashlar-native/` | `ashlar-native` | WASM / out-of-process native host |

Build and test this slice the same way `products-gate` does:

```bash
dotnet test products/Ashlar.Products.sln
dotnet test src/Ashlar.Tests.Contracts/Ashlar.Tests.Contracts.csproj \
  --filter FullyQualifiedName~DistributedContractTests
```

The open-core / commercial / cloud→kernel `ProjectReference` rules are enforced by
`dependency-boundary` (`scripts/verify-open-commercial-dependency-boundary.py`),
not by `products-gate`.

See [`docs/architecture/product-split.md`](../docs/architecture/product-split.md).
