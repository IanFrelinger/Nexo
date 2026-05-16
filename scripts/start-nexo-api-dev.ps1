#Requires -Version 5.1
<#
.SYNOPSIS
  Cross-platform dev helper (Windows): Docker Ollama + wait for readiness + run Nexo.API with correct env.

.DESCRIPTION
  Starts docker-compose.ollama.yml (unless -SkipOllama), waits for Ollama HTTP, optionally pulls a model,
  sets OLLAMA_* and NCR Ollama URL for the host, clears NEXO_BACKGROUND_AGENTS_CONFIG for a simple portal run,
  then dotnet run Nexo.API.

  Linux/macOS: use scripts/start-nexo-api-dev.sh (same flags).

.PARAMETER Model
  Ollama model tag (default: llama3.1:latest). Use -Pull on first run or after changing model.

.PARAMETER OllamaPort
  Host port published for Ollama (default: 11434).

.PARAMETER ApiPort
  Kestrel URL for Nexo.API (default: 8080).

.PARAMETER SkipOllama
  Do not start Docker Compose; assume Ollama is already reachable on OllamaPort.

.PARAMETER Pull
  Run `ollama pull` inside the container after Ollama is up.

.PARAMETER ComposeProjectName
  If set, exported as COMPOSE_PROJECT_NAME for this compose invocation (avoids volume/network clashes).

.PARAMETER WaitSeconds
  Max seconds to wait for Ollama /api/tags (default: 120).

.PARAMETER ListenLan
  Bind Nexo.API on 0.0.0.0 so other devices on your Wi‑Fi/LAN can open the portal (http://<this-pc-ip>:port).
  Ollama stays on 127.0.0.1 from the API process (Docker published port). You may need a Windows Firewall allow rule for ApiPort.
  Sets Nexo__Security__ExposureProfile=Lan unless you already set it (use Tailnet when using Tailscale — see docs/TailscaleAndNexo.md).
#>
param(
    [string] $Model = "llama3.1:latest",
    [int] $OllamaPort = 11434,
    [int] $ApiPort = 8080,
    [switch] $SkipOllama,
    [switch] $Pull,
    [string] $ComposeProjectName = "",
    [int] $WaitSeconds = 120,
    [switch] $ListenLan
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$compose = Join-Path $RepoRoot "docker-compose.ollama.yml"
$ollamaUrl = "http://127.0.0.1:$OllamaPort"

Set-Location $RepoRoot

if ($ComposeProjectName) {
    $env:COMPOSE_PROJECT_NAME = $ComposeProjectName
}

if (-not $SkipOllama) {
    $env:NEXO_OLLAMA_HOST_PORT = "$OllamaPort"
    Write-Host "Starting Ollama (Docker) on host port $OllamaPort..."
    docker compose -f $compose up -d

    $deadline = (Get-Date).AddSeconds($WaitSeconds)
    $ready = $false
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-RestMethod -Uri "$ollamaUrl/api/tags" -TimeoutSec 2 | Out-Null
            $ready = $true
            break
        } catch {
            Start-Sleep -Seconds 1
        }
    }
    if (-not $ready) {
        throw "Ollama did not become ready at $ollamaUrl within $WaitSeconds s. Check Docker Desktop and: docker compose -f docker-compose.ollama.yml logs ollama"
    }
    Write-Host "Ollama is ready."

    if ($Pull) {
        Write-Host "Pulling model '$Model'..."
        docker compose -f $compose exec ollama ollama pull $Model
    }
} else {
    Write-Host "SkipOllama: using existing Ollama at $ollamaUrl"
}

$env:OLLAMA_BASE_URL = $ollamaUrl
$env:OLLAMA_MODEL = $Model
$env:Nexo__NodeCapabilityRuntime__Ollama__BaseUrl = $ollamaUrl
if ($ListenLan) {
    $env:ASPNETCORE_URLS = "http://0.0.0.0:$ApiPort"
} else {
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$ApiPort"
}
Remove-Item Env:NEXO_BACKGROUND_AGENTS_CONFIG -ErrorAction SilentlyContinue

if (-not $env:Nexo__Security__ExposureProfile) {
    if ($ListenLan) {
        $env:Nexo__Security__ExposureProfile = "Lan"
    } else {
        $env:Nexo__Security__ExposureProfile = "Localhost"
    }
}

Write-Host ""
Write-Host "Nexo.API (this PC) → http://127.0.0.1:$ApiPort  |  Ollama → $ollamaUrl  |  Model → $Model"
if ($ListenLan) {
    Write-Host ""
    Write-Host "LAN / phone: same Wi‑Fi as this PC, open one of:"
    Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.PrefixOrigin -ne 'WellKnown' -and $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } |
        Select-Object -ExpandProperty IPAddress -Unique |
        ForEach-Object { Write-Host "  http://${_}:$ApiPort" }
    Write-Host ""
    Write-Host "If the phone cannot connect, allow inbound TCP $ApiPort in Windows Defender Firewall."
    Write-Host "Anyone on your LAN can reach the portal — do not use -ListenLan on untrusted networks."
}
Write-Host ""
Write-Host "Stop Ollama: docker compose -f docker-compose.ollama.yml down"
Write-Host "Or: powershell -File scripts/stop-nexo-api-dev.ps1"
Write-Host ""
dotnet run --project (Join-Path $RepoRoot "application/src/Nexo.API/Nexo.API.csproj")
