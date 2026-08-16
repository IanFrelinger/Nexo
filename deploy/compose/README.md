# Compose stacks

All Docker Compose deployment stacks live here. Run them **from the repository root** so CWD-relative arguments (like `--env-file`) resolve as documented:

```bash
docker compose -f deploy/compose/docker-compose.portal.yml up -d --build
```

Build contexts inside these files point at the repository root (`../..`), so images build identically regardless of the invocation directory. Override files are passed as a second `-f` after their base file.

| Stack | Purpose | Docs / entry point |
|-------|---------|--------------------|
| `docker-compose.portal.yml` | Portal + API ("Try" lane) | `README.md` Quick Start |
| `docker-compose.agent-server.yml` | Self-hosted agent server | `docs/SelfHostedAgentServer.md` |
| `docker-compose.agent-server.local.yml` | Override: local agent-set mounts for the agent server | `docs/SelfHostedAgentServer.md` |
| `docker-compose.mesh-lab.yml` | Mesh virtual lab (peers + workers) | `docs/MeshVirtualLab.md` |
| `docker-compose.mesh-lab-tls.override.yml` | Override: TLS fronting (Caddy) for the mesh lab | `docs/MeshVirtualLab.md` |
| `docker-compose.mesh-lab-stress.override.yml` | Override: stress-test profile for the mesh lab | `docs/MeshVirtualLab.md` |
| `docker-compose.friend-mesh.yml` | Friend-mesh prefab | `docs/FriendMeshPrefab.md` |
| `docker-compose.game-director.yml` | Game Director studio app | `docs/GameDirectorStudio.md` |
| `docker-compose.ollama.yml` | Local Ollama model runtime | `docs/Configuration.md` |
| `docker-compose.ollama.gpu.override.yml` | Override: GPU acceleration for Ollama | `docs/Configuration.md` |
| `docker-compose.cloud-multi-tenant.yml` | Cloud multi-tenant deployment shape | `docs/DistributionModels.md` |
| `docker-compose.private-single-tenant.yml` | Private single-tenant deployment shape | `docs/DistributionModels.md` |
| `docker-compose.ephemeral.yml` | Ephemeral deployment shape | `docs/DistributionModels.md` |
| `docker-compose.provenance.yml` | Provenance graph demo | `scripts/demo-provenance-graph.sh` |
| `docker-compose.test.yml` | Cached test-runner image | `.docker/Dockerfile.test-caching` |
