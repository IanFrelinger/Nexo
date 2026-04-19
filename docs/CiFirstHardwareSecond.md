# CI first, hardware second

Use GitHub Actions to catch **restore, compose, and dev-container** failures before you spend cycles on **target hardware**. Hosted runners are not identical to your GPU, custom kernel, or air-gapped site, but they match the **container + CLI** path this repo documents in `README.md`.

## One-button smoke (recommended)

In GitHub: **Actions → Setup Smoke Suite → Run workflow** (pick your branch). That workflow runs **in parallel**:

1. **Dev container path** — same commands as `.devcontainer/post-create.sh`, then `Nexo.CLI` build and `--help`.
2. **Deploy / test compose syntax** — `docker compose … config` on the main compose files (portal, agent-server, ephemeral, test, ollama) so merge errors surface without starting full stacks.
3. **Native Ubuntu lane** — `scripts/setup/setup.sh check` + `restore`, then `dotnet build` on `Nexo.CLI` (matches a common Linux laptop path).

Workflow file: `.github/workflows/setup-smoke-suite.yml`.

To reproduce the **dev container** job locally (Docker on your workstation):

```bash
bash scripts/ci/devcontainer-smoke.sh
```

## Deeper gates (when the smoke suite is green)

| Workflow | Use when |
|----------|----------|
| `devcontainer-gate.yml` | You changed `.devcontainer/` or the setup-gate restore graph (same smoke as `scripts/ci/devcontainer-smoke.sh`). |
| `compose-gate.yml` | You changed `docker-compose.test.yml` / ephemeral lanes or need full test-container run + Postgres smoke. |
| `container-image-gate.yml` | You changed `.docker/Dockerfile.cli` or CLI dependencies. |
| `environment-setup-gate-v1.yml` | You need **macOS + Windows** native `setup.ps1` / `setup.sh`, not only Ubuntu. |
| `onboarding-quickstart-gate.yml` | You changed onboarding scripts or want taxonomy artifacts. |
| `full-platform-readiness-gate.yml` | You want setup → discovery → dry-run across **many** OS/container matrices (slowest; run before release or big infra changes). |

## What still belongs on target hardware

- GPU / accelerator drivers and device nodes inside or beside containers.
- Real **Ollama model pull** size and bandwidth, **NVMe** performance, and **custom rootfs** libraries.
- **Network exposure** (firewall, Tailscale, mTLS) and **air-gap** mirrors not modeled in CI.

After CI is green, run the **same** `docker compose` / images documented under **Deploy (operators)** in `README.md` on the device once, then iterate locally.
