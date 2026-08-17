#Requires -Version 5.1
<#
.SYNOPSIS
  Stop Docker Ollama from deploy/compose/docker-compose.ollama.yml; optionally free the Nexo API port.

.PARAMETER KeepOllama
  If set, only attempt to stop the API listener (no docker compose down).

.PARAMETER ApiPort
  Port Nexo.API was bound to (default: 8080). Used only with -KillApi.

.PARAMETER KillApi
  Stop the process listening on ApiPort (use if you started the API in another terminal).
#>
param(
    [switch] $KeepOllama,
    [int] $ApiPort = 8080,
    [switch] $KillApi
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$compose = Join-Path $RepoRoot "deploy/compose/docker-compose.ollama.yml"

if (-not $KeepOllama) {
    Set-Location $RepoRoot
    Write-Host "Stopping Ollama stack (deploy/compose/docker-compose.ollama.yml)..."
    docker compose -f $compose down 2>$null
    Write-Host "Done (named volume ollama-models is kept)."
}

if ($KillApi) {
    $conn = Get-NetTCPConnection -LocalPort $ApiPort -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($conn) {
        Write-Host "Stopping process on port $ApiPort (PID $($conn.OwningProcess))..."
        Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue
    } else {
        Write-Host "No listener on port $ApiPort."
    }
}
