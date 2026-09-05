# ashlar-cluster

Self-hostable / managed cluster engine. Implements framework ports
(`ITaskScheduler`) so edge nodes and the cloud control plane share one
execution-envelope protocol.

This tree is extractable to `github.com/IanFrelinger/ashlar-cluster`. It must
depend only on Ashlar framework packages — never on `ashlar-cloud` or
`ashlar-workstation`.

Commercial Fleet / MeshDirector under `commercial/` is **not** moved in this
increment. When Fleet extracts, it becomes a consumer of this cluster engine
or stays a separate commercial overlay.
