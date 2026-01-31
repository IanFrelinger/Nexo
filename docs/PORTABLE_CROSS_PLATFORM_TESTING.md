# Portable Cross-Platform Testing (Any Host → Any Target)

This doc describes how to run tests **for any target platform from any host** in a portable way, and what is actually possible (including iOS and macOS).

## Host × Target Matrix

| Target ↓ / Host → | **Linux** | **macOS** | **Windows** |
|-------------------|-----------|-----------|--------------|
| **Linux** (Ubuntu, Alpine, Debian, etc.) | ✅ Docker | ✅ Docker | ✅ Docker |
| **Windows** | ✅ QEMU/KVM or CI | ✅ VM (Parallels/VMware) or CI | ✅ Native or Docker |
| **Android** | ✅ Docker (KVM) | ✅ Docker (nested virt) | ✅ Docker (nested virt) |
| **macOS** | ❌ Requires Mac | ✅ Native | ❌ Requires Mac |
| **iOS** | ❌ Requires Mac | ✅ Simulator / device | ❌ Requires Mac |

### Why some cells are limited

- **iOS** – Apple’s iOS Simulator runs only on **macOS**. There is no official or legal way to run it on Linux or Windows. Options from non-Mac: use **CI with macos-latest** or a **cloud testing service** (e.g. BrowserStack, LambdaTest).
- **macOS** – macOS is only licensed to run on Apple hardware. You cannot legally run macOS in a VM on non-Apple hardware. So “test macOS” from Linux/Windows means: use **CI with macos-latest** or a real Mac.
- **Windows** – From Linux you can run Windows in **QEMU/KVM** (e.g. dockur/windows) or use a **Windows runner in CI**. From Mac you can use a **Windows VM** (Parallels/VMware) or CI. From Windows you use native or Windows containers.
- **Android** – Portable: **Docker** images (e.g. budtmo/docker-android) can run the Android emulator on Linux, macOS, or Windows (with nested virtualization where needed).
- **Linux** – Portable: **Docker** gives you many distros (Ubuntu, Alpine, Debian, etc.) on any host.

So “test any platform on any platform” works for **Linux, Windows, and Android** from any host (using Docker and/or VM/CI). **iOS and macOS** can only be tested on a **Mac** (or via CI/cloud using Mac runners).

## Portable entry point: run what’s possible on this host

The idea is **one command** you run from any host; it:

1. Detects the current host (Linux, macOS, Windows).
2. Runs tests for every target that is **possible** on this host (Docker, VM, native, or simulator).
3. Skips targets that are **not possible** and prints why (e.g. “iOS requires macOS or CI macos-latest”).

No need to “change the host” manually: you run the same script on a Mac, a Linux box, or a Windows PC, and it does the right set of targets for that host.

### What runs where

| Host | Targets run locally | Targets that need CI or another host |
|------|----------------------|--------------------------------------|
| **Linux** | Linux (Docker), Android (Docker), optionally Windows (QEMU) | Windows (or use CI), iOS, macOS |
| **macOS** | Linux (Docker), Android (Docker), macOS (native), iOS (simulator), optionally Windows (VM) | — (all can run or use CI for Windows) |
| **Windows** | Linux (Docker), Android (Docker), Windows (native/Docker) | iOS, macOS |

So:

- **Fully portable (any host → same targets)** for: **Linux variants, Android, and Windows** (with Windows via CI or VM when not on Windows).
- **Portable with one constraint** for: **iOS and macOS** – they run only when the host is **Mac** (or you use CI/cloud with Mac runners).

## How to use it in this repo

1. **Portable script (recommended)**  
   From repo root, on any host:
   ```bash
   `dotnet run --project src/Nexo.CLI -- test portable [--list] [--scope persistence|smoke|all]` (or `nexo test portable` if the tool is installed). See [SCRIPTS_REPLACED_BY_CLI.md](SCRIPTS_REPLACED_BY_CLI.md).
   ```
   Or: `make test-portable [SCOPE=persistence|smoke|all]`
   - `--list` – Print which targets will run on this host and which will be skipped (and why).
   - Without `--list` – Run tests for all runnable targets (Docker Linux, Docker Android, native Windows on Windows, native macOS/iOS on Mac, etc.).
   - Same script works on Linux, macOS, and Windows (e.g. in Git Bash or WSL).
   - **Scope:** `persistence` (default) = persistence tests on all runnable platforms; `smoke` = smoke tests on this host only; `all` = persistence on all runnable + smoke on host.

2. **CI for full matrix**  
   Use the **Cross-Platform Tests** (and related) workflows so that every push or manual run executes on **ubuntu-latest**, **macos-latest**, and **windows-latest**. That gives you Linux, macOS, and Windows without switching your own machine. For **iOS**, run your iOS tests in a job that uses **macos-latest** (simulator or device).

3. **Optional: Windows VM from Linux**  
   If you’re on Linux and want a local Windows target, use a QEMU/KVM-based image (e.g. dockur/windows). The portable script can skip or run this depending on an option. See [VMS_IN_DOCKER.md](VMS_IN_DOCKER.md).

## Summary

- **Portable** = one script/entry point, same on every host; it runs what’s possible and skips the rest with a clear message.
- **Linux, Windows, Android** – Can be tested from any host (Docker + optional VM/CI for Windows).
- **iOS and macOS** – Can only be tested on a **Mac** (or via CI/cloud using Mac). The script will run them when on Mac and report “requires macOS” when not.

So you get “test for any platform, on any platform” in a portable way, with the only hard limit being **iOS and macOS**, which require a Mac or a Mac runner in CI.
