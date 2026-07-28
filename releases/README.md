# Release locks

`current.lock` is the immutable index-digest deployment contract.

Before promoting a new release, copy `current.lock` to `previous.lock`, generate
the new current lock, and verify `scripts/rollback-deployment.sh releases/previous.lock`
in the isolated rehearsal. Lock files are small, versioned evidence; never replace
them with tags such as `latest`.

| File | Purpose |
|------|---------|
| `current.lock` | Production pin (GHCR index digests) |
| `previous.lock` | Rollback target |
| `candidate.lock` | Local shipping-candidate only (`NEXO_ALLOW_LOCAL_IMAGES=1`) |

Promote a local candidate after GHCR auth (`write:packages`):

```bash
bash scripts/promote-shipping-candidate.sh nexo-dep-extract-agent:shipping-candidate
```

Local proof without publishing:

```bash
bash scripts/ship-proof-local.sh
```

Operator guide: `docs/production-readiness/ParserPipelineDeploymentShip.md`.
