# Release-readiness lab

End-to-end scenarios run against a **fresh image built from `master`** (not the released
tag), covering what the unit/integration suites can't: the real image booting, multi-node
federation, live-model autonomy, deployment ops, and onboarding honesty. Container-first —
matches the deployed node model.

These complement, they don't replace, the automated suites:

- `scripts/e2e-loop.sh` — 128 governance/trust/gates/packaging/export scenarios
- `scripts/run-cert-gate.sh` + `make kernel-gate` — the required checks
- the mesh unit/integration tests in `Ashlar.Tests.CLI`

## Prerequisites (one-time)

```bash
# 1. build the release-candidate image from the current tree
docker build -f .docker/Dockerfile.cli -t ashlar-cli:lab .

# 2. a lab network + a live model sidecar (for the autonomy scenarios)
docker network create ashlar-lab
docker run -d --name ashlar-lab-ollama --network ashlar-lab ollama/ollama:latest
docker exec ashlar-lab-ollama ollama pull qwen2.5-coder:1.5b

# 3. an on-network probe host with curl/jq/openssl (the image has none) —
#    any container on ashlar-lab works; the dev container is convenient:
docker network connect ashlar-lab <your-dev-or-probe-container>
```

The scripts default `PROBE=elated_satoshi`; set it to whatever probe container you connected.

## Run

```bash
bash scripts/lab/release-image-lab.sh        # image sanity, identity, trust, arming, A0/A1, F1/F4, deploy, onboarding
bash scripts/lab/release-federation-lab.sh   # live multi-node: serve, trusted-held vs untrusted-refused, discovery, identity persistence
```

Each prints `PASS / WEAK / FAIL` per scenario and a scorecard; a `*-results.tsv` is written
to the CWD. A trap tears down the transient `labnode-*` / `fnode-*` containers and `labvol-*`
volumes on exit.

## Notes for a truthful read

- **Verify a FAIL before believing it.** Some scenarios are sensitive to shell quoting, a
  `tail -1` catching a later idempotent tick, or (on Windows) `docker cp` path translation.
  When a scenario fails, reproduce the underlying command directly before treating it as a
  product defect — the authoritative check for "was it held" is the node's `gates` queue,
  not a log line.
- **Multicast discovery is best-effort.** It works across a user-defined Docker bridge but
  may not cross Docker Desktop's bridge to a physical LAN; `ASHLAR_MESH_PEERS` (any routable
  or tailnet address) is the works-everywhere baseline.
- **A fresh node is unsigned by design** until `ashlar keys init` (presence-activated
  signing), so its heartbeat `keyFingerprint` is null until then.
