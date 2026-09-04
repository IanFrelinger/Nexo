# ashlar-cloud

Hosted control plane: organizations, OIDC, billing, and quotas. It may depend on
`ashlar-cluster` (later) and Ashlar framework packages. It must never be
referenced by the kernel.

This tree is extractable to `github.com/IanFrelinger/ashlar-cloud`. The scaffold
intentionally has **no** `ProjectReference` to `src/` so the control plane
cannot accidentally invert the framework/product split. Cluster calls happen
over the execution-envelope protocol after extraction.
