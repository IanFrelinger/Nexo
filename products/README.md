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
later extraction).

| Tree | Future repo | Role |
|------|-------------|------|
| `ashlar-workstation/` | `ashlar-workstation` | Air-gapped IDE daemon using `SecureWorkstation` |
| `ashlar-cluster/` | `ashlar-cluster` | Cluster scheduler implementing `ITaskScheduler` |
| `ashlar-cloud/` | `ashlar-cloud` | Hosted control plane (orgs, billing, quotas) |
| `ashlar-native/` | `ashlar-native` | WASM / out-of-process native host |

Build and test this slice:

```bash
dotnet test products/Ashlar.Products.sln
```

See [`docs/architecture/product-split.md`](../docs/architecture/product-split.md).
