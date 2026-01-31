# Testing on Windows

## Simulating a Windows environment

You **cannot** run a real Windows environment (or Windows containers) on macOS or Linux. Options:

| Approach | Where | What you get |
|----------|--------|----------------|
| **CI (GitHub Actions)** | `windows-latest` runner | Real Windows VM; run `dotnet test` or the persistence script with `--env windows-8.0`. |
| **Docker (Windows host only)** | Windows PC or `windows-latest` in CI | Build/run the **Windows container** (`.docker/Dockerfile.test-caching-windows`); persistence script and CI both support this. |
| **Local Windows** | Your own PC or VM | Run the same scripts/tests natively on Windows. |
| **Windows VM** | Parallels, VMware, VirtualBox, Hyper-V | Full Windows guest; run tests or Docker Windows containers there. |
| **Docker “Windows” on Mac/Linux** | ❌ | Not possible; Windows containers require a Windows host. |

## Docker option for Windows

**Yes.** There is a Docker path for Windows; it only works on a **Windows host** (or `windows-latest` in CI).

- **Image:** `.docker/Dockerfile.test-caching-windows` (Nano Server + .NET SDK).
- **Locally (on a Windows machine):**  
  `.\scripts\test-persistence-multi-env.sh --env windows-8.0`  
  The script builds the Windows image and runs persistence tests inside the container using `cmd` (no bash in the image).
- **In CI:** The workflow **Persistence Tests (Multi-OS)** includes a job **Persistence - Windows (Docker)** that runs on `windows-latest`, builds the Windows image, and runs persistence tests inside it. So you get Windows coverage via Docker in CI without a local Windows box.

## Recommended: CI with a Windows job

Use GitHub Actions (or similar) so every push/PR runs your tests on a **Windows runner** (`windows-latest`). That gives you real Windows coverage without a local Windows machine.

This repo includes:

- **`.github/workflows/test-persistence-multi-os.yml`** – Runs persistence tests on **Ubuntu**, **macOS**, and **Windows** (native `dotnet test`), plus a **Windows (Docker)** job that runs the same tests inside the Windows container on `windows-latest`.

- **`.github/workflows/test-caching-multi-env.yml`** – Includes a **windows-8.0** matrix job that runs on `windows-latest` and uses Docker to build/run the Windows test image for caching/geo tests.

## Running the persistence script on Windows locally

On a Windows machine (or Windows VM):

```powershell
# Only Windows container platform
.\scripts\test-persistence-multi-env.sh --env windows-8.0

# Or use the Nexo CLI (builds and runs in Docker)
dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj -- test --platforms windows --project Nexo.Tests.Infrastructure --filter "FullyQualifiedName~InMemoryPersistenceTests"
```

On macOS/Linux, `--env windows-8.0` is skipped by the script (see the “Windows containers” message).

## Summary

- **Simulate Windows for tests:** Use CI with a `windows-latest` job (e.g. the new persistence multi-OS workflow).
- **Run Windows containers:** Use a Windows host (PC or VM) and run the script or CLI there.
- **No way:** Run Windows containers or a real Windows kernel on Mac/Linux.
