<#
.SYNOPSIS
Runs the autonomy first flight inside the repo's devcontainer image, talking to the
HOST Docker daemon through the mounted socket.

.DESCRIPTION
Two constraints shape this script. Windows Smart App Control blocks freshly built
unsigned DLLs on the host (0x800711C7), so the flight binary must build and run inside
Linux, where SAC does not apply — same reasoning as scripts/test-in-container.ps1, same
clone-from-read-only-mirror pattern, so only COMMITTED state flies. And the flight
needs a real container engine for its sandbox session, so /var/run/docker.sock is
mounted through: sessions started by the flight are SIBLING containers on the host
daemon (which is also why the flight's SandboxSpec carries no mounts — container-local
paths would be meaningless to the host daemon; see the known-limitation notes).

The docker CLI is a single static binary fetched into the container at run time; the
session image (alpine) is pulled by the HOST daemon on first use.

.PARAMETER Dry
Use the TestKit fake session runner instead of the live daemon (wiring proof only).

.PARAMETER Ref
Git ref to fly. Defaults to HEAD.

.EXAMPLE
powershell -NoProfile -File spikes/autonomy-first-flight/run-first-flight.ps1 -Dry
powershell -NoProfile -File spikes/autonomy-first-flight/run-first-flight.ps1
#>
param(
    [switch]$Dry,
    [string]$Ref = "HEAD"
)

$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel)
if (-not $repoRoot) { throw "Not inside a git repository." }
$sha = (git rev-parse $Ref)

$image = "mcr.microsoft.com/devcontainers/dotnet:9.0-bookworm"
$dryArg = if ($Dry) { "--dry" } else { "" }
$mode = if ($Dry) { "DRY" } else { "REAL (host daemon via docker.sock)" }

Write-Host "== autonomy first flight: $sha [$mode] =="

docker run --rm --user root `
    -v "${repoRoot}:/src-mirror:ro" `
    -v nexo-nuget-packages:/root/.nuget/packages `
    -v /var/run/docker.sock:/var/run/docker.sock `
    -e DOTNET_ROLL_FORWARD=LatestMajor `
    $image `
    bash -lc "set -e; if [ ! -x /usr/local/bin/docker ]; then curl -fsSL https://download.docker.com/linux/static/stable/x86_64/docker-27.5.1.tgz | tar -xz --strip 1 -C /usr/local/bin docker/docker; fi; git config --global safe.directory '*'; git clone -q /src-mirror /repo; cd /repo; git checkout -q $sha; dotnet run --project spikes/autonomy-first-flight/FirstFlight/FirstFlight.csproj -c Release -- $dryArg"

exit $LASTEXITCODE
