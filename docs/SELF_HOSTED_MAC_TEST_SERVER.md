# Self-Hosted Mac-Focused Test Server

You can run a **Mac-focused test server** on your own Mac (physical or cloud) so that macOS and iOS tests run on your hardware instead of (or in addition to) GitHub-hosted `macos-latest`. Two main approaches:

1. **GitHub Actions self-hosted runner** – Your Mac runs as a CI runner; workflows dispatch Mac/iOS jobs to it.
2. **Standalone test server** – A Mac that runs tests on a schedule or on demand (cron, script, or agent) without GitHub Actions.

---

## Option 1: GitHub Actions Self-Hosted Runner (recommended)

Your Mac becomes a **runner** that executes Mac/iOS jobs from your workflows. One machine handles all Mac-focused testing.

### 1. Prepare the Mac

- **OS:** macOS 11 (Big Sur) or later (x64 or ARM64).
- **Install:**
  - **Xcode** (from App Store or developer.apple.com) – required for iOS Simulator and macOS builds.
  - **.NET SDK** (e.g. 8.0) – for Nexo and `dotnet test`.
  - **Command Line Tools** (if not using full Xcode): `xcode-select --install`.
  - **Docker Desktop** (optional) – if you want the same Mac to run Linux containers (e.g. Ubuntu, Alpine) for cross-platform tests.

### 2. Register the runner

1. On GitHub: **Settings** → **Actions** → **Runners** (repo or org).
2. **New self-hosted runner**.
3. Choose **macOS** and your architecture (x64 or ARM64).
4. On the Mac, run the download and config commands shown (they look like this; use the exact ones from GitHub):

```bash
# Create a folder for the runner
mkdir -p ~/actions-runner && cd ~/actions-runner

# Download (use the URL and version from GitHub’s instructions)
curl -o actions-runner-osx-arm64-2.311.0.tar.gz -L https://github.com/actions/runner/releases/download/v2.311.0/actions-runner-osx-arm64-2.311.0.tar.gz
tar xzf ./actions-runner-osx-arm64-2.311.0.tar.gz

# Configure (token and repo/org URL from GitHub)
./config.sh --url https://github.com/YOUR_ORG_OR_USER/YOUR_REPO --token YOUR_TOKEN

# Run as a service (recommended) or in foreground
./run.sh
```

5. Use a **label** for Mac-focused jobs, e.g. `self-hosted`, `macos`, `mac`, or `x64`/`ARM64`. You can add custom labels in `config.sh` (e.g. `--labels macos,ios`).

### 3. Use the runner in workflows

Point Mac/iOS jobs at your runner with `runs-on`:

```yaml
jobs:
  mac-tests:
    name: Mac & iOS Tests
    runs-on: [self-hosted, macos]   # or [self-hosted, macos, ARM64] etc.
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet test src/Nexo.Tests.Infrastructure/... --filter "FullyQualifiedName~InMemoryPersistenceTests"
      # iOS simulator steps if needed (e.g. xcrun simctl, xcodebuild)
```

- **Repo-level runner:** only this repo’s workflows can use it.
- **Org-level runner:** any repo in the org can use it (good for a shared “Mac test server”).

### 4. Keep it running

- **Foreground:** `./run.sh` in a terminal or tmux/screen.
- **Service (recommended):** GitHub’s instructions include installing the runner as a LaunchDaemon so it starts on boot and restarts if it exits.

### 5. Security and networking

- The Mac must reach GitHub (outbound HTTPS).
- Jobs run in the runner’s user context; avoid storing long-lived secrets on the machine if possible, and use GitHub’s secret injection.
- For a dedicated test server, consider a clean user account and minimal extra software.

---

## Option 2: Standalone Mac Test Server (no GitHub runner)

Use a Mac as a **test server** that runs your test suite on a schedule or on demand, without registering it as a GitHub Actions runner.

1. **Same prep:** Install Xcode, .NET SDK, and (optional) Docker on the Mac.
2. **Clone the repo** (or pull periodically).
3. **Run tests** via the portable script or `dotnet test`:
   - On demand: `dotnet run --project src/Nexo.CLI -- test portable --scope persistence` (or `nexo test portable --scope persistence`). Runs all targets possible on Mac, including iOS/macOS. See [SCRIPTS_REPLACED_BY_CLI.md](SCRIPTS_REPLACED_BY_CLI.md).
   - Scheduled: add a cron job or launchd plist that runs the same command (e.g. nightly).
4. **Results:** Redirect output to logs or artifacts (e.g. `test-results/`) and optionally publish them (e.g. to S3, internal dashboard, or a simple HTTP endpoint).

This gives you a “Mac-focused test server” that always runs on your Mac; it does not integrate with GitHub Actions unless you also add Option 1.

---

## Mac in the cloud (no physical Mac)

If you don’t have a Mac to dedicate:

- **MacStadium,** **AWS EC2 Mac,** **Scaleway Mac** (and similar) rent macOS instances. Set one up, then use it as in Option 1 (self-hosted runner) or Option 2 (standalone server).
- **GitHub-hosted `macos-latest`** – No setup; use `runs-on: macos-latest` in workflows. Not “self-hosted,” but no Mac to maintain.

---

## Summary

| Approach | What you get |
|----------|----------------|
| **Self-hosted runner (Option 1)** | Your Mac runs Mac/iOS (and optionally Linux-in-Docker) jobs from GitHub Actions. One Mac = your own Mac-focused test server. |
| **Standalone server (Option 2)** | Same Mac runs tests on a schedule or on demand via scripts; no GitHub runner. |
| **Mac in the cloud** | Same as Option 1 or 2, but the “Mac” is a rented instance. |
| **GitHub-hosted macos-latest** | Mac tests run on GitHub’s Macs; no self-hosted server. |

For a **self-hosted Mac-focused test server**, use **Option 1**: register the Mac as a GitHub Actions self-hosted runner, give it a label like `macos`, and use `runs-on: [self-hosted, macos]` (or your label) for Mac/iOS jobs in your workflows.
