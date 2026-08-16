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
    [switch]$Sweep,
    # -SweepLive: the campaign mode. One sweep of the standing loop with a LIVE ollama proposer
    # composed INSIDE the loop (so the loop's own repair channel runs), every sample objective
    # seeded, and a host-mounted campaign directory that keeps the objectives, every recorded
    # proposal (with the exact projected feedback the model saw) and the full log after the
    # container is gone. Hold admission stays on: certify everything, admit nothing.
    [switch]$SweepLive,
    [int]$MaxObjectives = 5,
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
if ($Sweep) { $dryArg = "--sweep" }
if ($SweepLive) { $dryArg = "--sweep" }
if ($Live) { $dryArg = "$dryArg --live".Trim() }
$mode = if ($Dry) { "DRY" } else { "REAL (host daemon via docker.sock)" }
if ($SweepLive) { $mode = "$mode + SWEEP with LIVE in-loop proposer (campaign)" }

# -SweepLive: campaign directory on the host, mounted into the runner at /campaign.
$campaignMount = @()
$campaignEnv = @()
$seedCmd = "mkdir -p .nexo/runtime-studio/objectives/pending; cp samples/autonomy-objectives/tag-scan-classifier.* .nexo/runtime-studio/objectives/pending/ 2>/dev/null || true;"
$campaignDir = $null
if ($SweepLive) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $campaignDir = Join-Path $repoRoot ".nexo/campaign/$stamp"
    New-Item -ItemType Directory -Path (Join-Path $campaignDir "objectives/pending") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $campaignDir "proposals") -Force | Out-Null
    Copy-Item (Join-Path $repoRoot "samples/autonomy-objectives/*.md") (Join-Path $campaignDir "objectives/pending/")
    Copy-Item (Join-Path $repoRoot "samples/autonomy-objectives/*.witness.json") (Join-Path $campaignDir "objectives/pending/")
    Remove-Item (Join-Path $campaignDir "objectives/pending/README.md") -ErrorAction SilentlyContinue
    $campaignMount = @("-v", "${campaignDir}:/campaign")
    $campaignEnv = @(
        "-e", "NEXO_SWEEP_PROPOSER=ollama",
        "-e", "NEXO_OLLAMA_BASE_URL=http://host.docker.internal:11434",
        "-e", "NEXO_OLLAMA_MODEL=codellama:7b",
        "-e", "NEXO_OBJECTIVES_ROOT=/campaign/objectives",
        "-e", "NEXO_CAMPAIGN_DIR=/campaign/proposals",
        "-e", "NEXO_SWEEP_MAX_OBJECTIVES=$MaxObjectives"
    )
    $seedCmd = ""  # objectives come from the mounted campaign store, not the clone
    Write-Host "== campaign directory: $campaignDir =="
}
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

$runnerCmd = "set -e; if [ ! -x /usr/local/bin/docker ]; then curl -fsSL https://download.docker.com/linux/static/stable/x86_64/docker-27.5.1.tgz | tar -xz --strip 1 -C /usr/local/bin docker/docker; fi; git config --global safe.directory '*'; git clone -q /src-mirror /repo; cd /repo; git checkout -q $sha; $seedCmd dotnet run --project spikes/autonomy-first-flight/FirstFlight/FirstFlight.csproj -c Release -- $dryArg"

if ($SweepLive) {
    # Campaign: keep the whole transcript on the host beside the objectives and proposals.
    # The tee runs INSIDE the container (PowerShell 5.1 turns native stderr into errors when
    # redirected), writing to the mounted campaign directory.
    $campaignCmd = "set -o pipefail; ( $runnerCmd ) 2>&1 | tee /campaign/sweep.log"
    docker run --rm --user root `
        -v "${repoRoot}:/src-mirror:ro" `
        -v nexo-nuget-packages:/root/.nuget/packages `
        -v /var/run/docker.sock:/var/run/docker.sock `
        @campaignMount `
        @campaignEnv `
        -e DOTNET_ROLL_FORWARD=LatestMajor `
        $image `
        bash -lc $campaignCmd
    $code = $LASTEXITCODE
    Write-Host "== campaign complete: exit $code — see $campaignDir =="
    exit $code
}

docker run --rm --user root `
    -v "${repoRoot}:/src-mirror:ro" `
    -v nexo-nuget-packages:/root/.nuget/packages `
    -v /var/run/docker.sock:/var/run/docker.sock `
    @liveMount `
    -e DOTNET_ROLL_FORWARD=LatestMajor `
    $image `
    bash -lc $runnerCmd

exit $LASTEXITCODE
