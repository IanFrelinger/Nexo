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

.PARAMETER SessionBuild
Enable the P3 leg: the candidate must compile INSIDE the attested session, so the
session image becomes the pinned SDK image (mcr.microsoft.com/dotnet/sdk:9.0) and the
certificate gains a session-build input.

.PARAMETER Ref
Git ref to fly. Defaults to HEAD.

.EXAMPLE
powershell -NoProfile -File spikes/autonomy-first-flight/run-first-flight.ps1 -Dry
powershell -NoProfile -File spikes/autonomy-first-flight/run-first-flight.ps1
powershell -NoProfile -File spikes/autonomy-first-flight/run-first-flight.ps1 -SessionBuild
#>
param(
    [switch]$Dry,
    [switch]$SessionBuild,
    [switch]$SessionExecute,
    [switch]$Proposed,
    [switch]$Live,
    [string]$Ref = "HEAD"
)

$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel)
if (-not $repoRoot) { throw "Not inside a git repository." }
$sha = (git rev-parse $Ref)

$image = "mcr.microsoft.com/devcontainers/dotnet:9.0-bookworm"
$dryArg = if ($Dry) { "--dry" } else { "" }
if ($SessionBuild) { $dryArg = "$dryArg --session-build".Trim() }
if ($SessionExecute) { $dryArg = "$dryArg --session-execute".Trim() }
if ($Proposed) { $dryArg = "$dryArg --proposed".Trim() }
if ($Live) { $dryArg = "$dryArg --live".Trim() }
$mode = if ($Dry) { "DRY" } else { "REAL (host daemon via docker.sock)" }
if ($SessionBuild) { $mode = "$mode + in-session build" }
if ($SessionExecute) { $mode = "$mode + in-session execution" }
if ($Proposed) { $mode = "$mode + MODEL-PROPOSED candidate" }
if ($Live) { $mode = "$mode + LIVE ollama proposal" }

# -Live: call the local model NOW, at flight time, and hand the loop its proposal.
# The call happens host-side (where ollama listens); the raw exchange is recorded as a
# committed-after-flight artifact (record/replay discipline), and the recording rides
# into the container as a read-only INPUT mount - the proposal is data the flight
# consumes, exactly as a proposer cluster would hand it over.
$liveMount = @()
if ($Live) {
    $promptPath = Join-Path $repoRoot "spikes/autonomy-first-flight/live-proposal-prompt.md"
    $prompt = [IO.File]::ReadAllText($promptPath)
    Write-Host "== live proposal: calling ollama codellama:7b =="
    $body = @{ model = "codellama:7b"; prompt = $prompt; stream = $false
               options = @{ temperature = 0.2; num_predict = 1600 } } | ConvertTo-Json -Depth 4
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $resp = Invoke-RestMethod -Uri "http://localhost:11434/api/generate" -Method Post `
        -ContentType "application/json" -Body $body -TimeoutSec 600
    $sw.Stop()
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $recDir = Join-Path $repoRoot "spikes/autonomy-first-flight/recordings"
    if (-not (Test-Path $recDir)) { New-Item -ItemType Directory -Path $recDir | Out-Null }
    $recording = @{
        provider   = "ollama"
        model      = "codellama:7b"
        proposedAt = (Get-Date).ToUniversalTime().ToString("o")
        durationSeconds = [math]::Round($sw.Elapsed.TotalSeconds, 1)
        signature  = "model:ollama:codellama-7b:live:$stamp"
        prompt     = $prompt
        response   = $resp.response
    } | ConvertTo-Json -Depth 3
    $recPath = Join-Path $recDir "live-$stamp.json"
    [IO.File]::WriteAllText($recPath, $recording, [Text.UTF8Encoding]::new($false))
    Write-Host "== live proposal recorded: $recPath ($($sw.Elapsed.TotalSeconds.ToString('F0'))s) =="
    $liveDir = Join-Path $env:TEMP "nexo-live-$stamp"
    New-Item -ItemType Directory -Path $liveDir | Out-Null
    Copy-Item $recPath (Join-Path $liveDir "recording.json")
    $liveMount = @("-v", "${liveDir}:/nexo-live:ro")
}

Write-Host "== autonomy first flight: $sha [$mode] =="

docker run --rm --user root `
    -v "${repoRoot}:/src-mirror:ro" `
    -v nexo-nuget-packages:/root/.nuget/packages `
    -v /var/run/docker.sock:/var/run/docker.sock `
    @liveMount `
    -e DOTNET_ROLL_FORWARD=LatestMajor `
    $image `
    bash -lc "set -e; if [ ! -x /usr/local/bin/docker ]; then curl -fsSL https://download.docker.com/linux/static/stable/x86_64/docker-27.5.1.tgz | tar -xz --strip 1 -C /usr/local/bin docker/docker; fi; git config --global safe.directory '*'; git clone -q /src-mirror /repo; cd /repo; git checkout -q $sha; dotnet run --project spikes/autonomy-first-flight/FirstFlight/FirstFlight.csproj -c Release -- $dryArg"

exit $LASTEXITCODE
