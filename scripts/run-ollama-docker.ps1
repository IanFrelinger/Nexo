#Requires -Version 5.1
<#
.SYNOPSIS
  Start Ollama in Docker (Docker Desktop) and pull a model.

.PARAMETER Model
  Ollama model tag to pull (default: llama3.1:latest).

.PARAMETER Port
  Host port mapped to Ollama (default: 11434).
#>
param(
    [string] $Model = "llama3.1:latest",
    [int] $Port = 11434
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$compose = Join-Path $RepoRoot "deploy/compose/docker-compose.ollama.yml"

$env:NEXO_OLLAMA_HOST_PORT = "$Port"
Set-Location $RepoRoot

Write-Host "Starting Ollama container (port $Port)..."
docker compose -f $compose up -d

Write-Host "Pulling model '$Model' (first run can take several minutes)..."
docker compose -f $compose exec ollama ollama pull $Model

Write-Host @"

Ollama is listening at http://127.0.0.1:$Port

Or start Nexo.API with env + health wait (all platforms):
  powershell -File scripts/start-nexo-api-dev.ps1 -Pull
  bash scripts/start-nexo-api-dev.sh --pull

Manual env for Nexo:
  `$env:OLLAMA_BASE_URL = 'http://127.0.0.1:$Port'
  `$env:OLLAMA_MODEL = '$Model'
  `$env:Nexo__NodeCapabilityRuntime__Ollama__BaseUrl = 'http://127.0.0.1:$Port'

Stop Ollama: docker compose -f deploy/compose/docker-compose.ollama.yml down
"@
