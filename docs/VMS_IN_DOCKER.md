# Open-Source VMs That Run Inside Docker

You can run full VMs (including Windows or other OSes) **inside** Docker containers using QEMU/KVM in a container. These are open-source options that work on Linux (and some on macOS with limitations).

## Popular options

| Project | What it does | License | Notes |
|--------|----------------|--------|-------|
| **[dockur/windows](https://github.com/dockur/windows)** | Windows inside a Docker container (QEMU) | MIT | Very popular (~50k stars). Runs Windows in a container on Linux. RDP access. |
| **[qemux/qemu](https://hub.docker.com/r/qemux/qemu)** | QEMU in Docker with web UI | — | Supports many disk formats (.iso, .img, .qcow2, .vhd, .vmdk). KVM acceleration. |
| **[container-vm](https://github.com/wy-z/container-vm)** (wy-z) | QEMU/KVM VMs in containers | Open source | Supports Windows, OpenWRT, others. Python entry point. |
| **[docker-qemu-vm](https://github.com/lnattrass/docker-qemu-vm)** | QEMU VM in a container | — | Aimed at Kubernetes; TAP/bridge networking. |
| **[dockerqemu](https://github.com/dturvene/dockerqemu)** | QEMU guest in Debian container | — | Stable QEMU deps; virtiofsd for shared files. |

## How it works

- A **Docker image** runs QEMU (and optionally KVM) inside the container.
- You provide or build a **VM disk image** (e.g. Windows ISO → installed disk, or a Linux image).
- The container **emulates** (or with KVM on Linux, virtualizes) the guest OS.
- You get a full VM (e.g. Windows) that you can use for testing, RDP, or running Windows-only tools.

## Platform notes

- **Linux:** Best support. KVM gives near-native performance. dockur/windows and the others are typically used on Linux.
- **macOS:** QEMU can run in Docker on Mac, but **no KVM** (KVM is Linux-only). So you get emulation only, which is slower. Possible for light use or CI that runs on Linux.
- **Windows host:** For “Windows in Docker” you’d use **Windows containers** (native) instead of QEMU. The projects above are for “run a VM (e.g. Windows) inside Docker **on Linux**.”

## Using this for Nexo tests

If you want a **Windows environment** for tests without a real Windows machine:

1. **CI on Linux** – Use a Linux runner and run dockur/windows (or similar) in a job: start the container, wait for Windows to boot, then run your tests inside the VM (e.g. via RDP/SSH or a script inside the guest). Heavy and slower than native Windows runners.
2. **Local Linux** – Run the same setup on your Linux box to get a Windows VM in Docker for ad‑hoc testing.
3. **Simpler alternative** – Use GitHub Actions (or similar) with **windows-latest** and optionally the **Windows Docker** job we added. No VM-in-Docker needed; you get a real Windows runner.

For **Linux-on-Linux** (e.g. different distros), standard **Docker images** (Alpine, Debian, Ubuntu, etc.) are usually enough; you don’t need a full VM.

## Quick links

- dockur/windows: https://github.com/dockur/windows  
- qemux/qemu (Docker Hub): https://hub.docker.com/r/qemux/qemu  
- container-vm: https://github.com/wy-z/container-vm  

## Summary

**Yes.** Open-source VMs can run in Docker using **QEMU** (and KVM on Linux). **dockur/windows** and **qemux/qemu** are two common options; **container-vm** is another. They’re most practical on **Linux** (KVM). On Mac you can run them with QEMU only (slower). For CI, using a **Windows runner** (e.g. `windows-latest`) is usually simpler than running a Windows VM inside Docker.
