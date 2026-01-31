# Cross-Platform Test Server (Mac, Windows, Linux from One Place)

You can run tests on **Mac, Windows, and Linux without changing hosts** by using a **single control point** that dispatches work to each OS. You don’t run everything on one physical machine; the “server” is the **orchestrator** (CI or a small lab) that runs jobs on each platform.

## Option 1: CI as the Test Server (recommended)

**GitHub Actions** (or Azure DevOps, GitLab CI, etc.) is your cross-platform test server:

- One push or one manual trigger runs workflows.
- Each workflow can run **multiple jobs** on different runners: `ubuntu-latest`, `macos-latest`, `windows-latest`.
- You stay on your current host (Mac or PC); the **CI platform** runs the actual tests on each OS.

No extra machines or VMs on your side. You get Mac + PC + Linux coverage from one place (the repo / Actions tab).

### How to use it

1. **Push or open a PR**  
   Path-specific workflows (e.g. persistence, caching) run on the right OSes automatically.

2. **Manual run**  
   In GitHub: **Actions** → choose a workflow (e.g. **Cross-Platform Tests** or **Persistence Tests (Multi-OS)**) → **Run workflow**.  
   One click runs the selected tests on all configured platforms.

3. **From the command line (no host change)**  
   From any host with `gh` and permissions:
   ```bash
   gh workflow run "Cross-Platform Tests" --ref main
   ```
   Then check the run in the Actions tab. Same workflow, same platforms; you didn’t switch machines.

### What this repo has

- **Persistence Tests (Multi-OS)** – Runs persistence tests on Ubuntu, macOS, and Windows (native + Windows Docker).
- **Test Caching Multi-Environment** – Runs caching/geo tests on several environments (including Windows when the runner is `windows-latest`).
- **Cross-Platform Tests** (umbrella) – Single workflow you can trigger to run build + test on **Ubuntu, macOS, and Windows** in one go. Scope: `smoke` (default), `persistence`, or `full`. See [.github/workflows/cross-platform-tests.yml](../.github/workflows/cross-platform-tests.yml).

So the “cross-platform test server” is: **trigger one of these workflows**; they run on Mac, PC, and Linux for you.

---

## Option 2: Self-Hosted Runners (your own “server”)

If you want the same model but **your own hardware** (e.g. on-prem or a lab):

1. Add **self-hosted runners** to GitHub (or your CI):
   - One runner on a **Windows** machine (or VM).
   - One runner on a **macOS** machine (or VM, where allowed).
   - One runner on a **Linux** machine (or VM).

2. Use **labels** so workflows run on the right OS (e.g. `runs-on: [self-hosted, windows]`).

3. Keep the **same workflows**; only the runner type changes (hosted → self-hosted). One repo, one pipeline, Mac + PC + Linux still handled from one place without you “changing the host” to run each OS—the runners are the “server” that handles each platform.

---

## Option 3: Single Host + Docker (Linux-only coverage)

On **one Linux host** you can cover **many Linux variants** with Docker (e.g. `dotnet run --project src/Nexo.CLI -- test multi-env --suite persistence --all` for ubuntu, alpine, debian, android). See [SCRIPTS_REPLACED_BY_CLI.md](SCRIPTS_REPLACED_BY_CLI.md). That’s still “one server,” but it only runs **Linux** (and Linux-based containers). It does **not** run real macOS or Windows; for those you need Option 1 or 2.

---

## Summary

| Goal | Approach |
|------|----------|
| Run tests on Mac, PC, and Linux without switching your own machine | **CI (Option 1)** – trigger a workflow; it runs on all three OSes. |
| Same, but on your own machines | **Self-hosted runners (Option 2)** – register Mac, PC, Linux as runners; same workflows. |
| One Linux host testing many Linux flavors | **Docker + scripts (Option 3)** – e.g. `test-persistence-multi-env.sh`; no Mac/Windows. |

The cross-platform test “server” is either **CI** (hosted or self-hosted) or a **single Linux host + Docker** for Linux-only. For Mac and PC without changing the host you use daily, use CI (or self-hosted runners) and trigger the cross-platform workflow.
