# ashlar-cloud

Hosted control-plane stubs: organizations, billing, and quotas (OIDC is planned,
not implemented in this scaffold). It may depend on `ashlar-cluster` (later) and
Ashlar framework packages **via NuGet after extraction**. In this monorepo it
must not `ProjectReference` `src/` or `commercial/`. It must never be
referenced by the kernel.

This tree is extractable to `github.com/IanFrelinger/ashlar-cloud`. The scaffold
intentionally has **no** `ProjectReference` to `src/` so the control plane
cannot accidentally invert the framework/product split. Cluster calls happen
over the execution-envelope protocol after extraction.
