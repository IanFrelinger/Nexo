# deploy/

## `node.yml` — THE node

`deploy/node.yml` is the deployable Ashlar node: the file you copy to a machine you want in the
fleet. It pins a published image by digest, keeps the node's identity, published packages, cycle
history **and** gate store on one named volume, and restarts unless you stop it.

```bash
docker compose -f deploy/node.yml up -d
docker compose -f deploy/node.yml logs -f
```

Update the pin with `scripts/node-update.sh` rather than editing the digest by hand.

Everything under [`compose/`](compose/README.md) is a lab, a demo or a development stack — none of
those is a node.

## `k8s/`

Kubernetes manifests. Not the node path; see `node.yml` above.
