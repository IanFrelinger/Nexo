# Start the local self-hosted Nexo agent server (full-stack lane) on Windows.
# Prerequisites: Docker Desktop; Ollama on host OR bundled compose ollama service.
#
# Examples:
#   .\scripts\Start-FullstackAgentServer.ps1
#   .\scripts\Start-FullstackAgentServer.ps1 -ApiPort 8090 -OllamaHostPort 11434
#   .\scripts\Start-FullstackAgentServer.ps1 -ApiPort 8088 -OllamaModel llama3.1:latest
#   .\scripts\Start-FullstackAgentServer.ps1 -ApiHost 127.0.0.1 -ApiPort 9088 -UseBundledOllama
#
# Ports and URLs are written into repo-root .env (Compose + smoke + IDE use the same values).

param(
    [string]$ApiHost = "127.0.0.1",
    [int]$ApiPort = 0,
    [int]$OllamaHostPort = 0,
    [string]$OllamaModel = "",
    [string]$OllamaBaseUrl = "",
    [switch]$UseBundledOllama,
    [switch]$NoBuild,
    [switch]$SkipSmokeHint
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\_NexoEnv.ps1"

$root = Get-NexoRepoRoot
Set-Location $root

$defaults = @{
    COMPOSE_PROJECT_NAME                              = "nexo-fullstack-agent"
    NEXO_REPO_ROOT                                    = ($root.Path -replace '\\', '/')
    NEXO_AGENT_SERVER_HTTP_PORT                       = "8088"
    NEXO_OLLAMA_HOST_PORT                             = "11434"
    NEXO_API_HOST                                     = "127.0.0.1"
    OLLAMA_BASE_URL                                   = "http://host.docker.internal:11434"
    OLLAMA_MODEL                                      = "codellama:7b"
    NEXO_BACKGROUND_AGENTS_CONFIG                     = "/agents/agent_set.fullstack.local.json"
    Nexo__RegisterBackgroundAgentHostedService        = "false"
    Nexo__NodeCapabilityRuntime__Ollama__BaseUrl      = "http://host.docker.internal:11434"
    Nexo__Meai__OllamaBaseUrl                         = "http://host.docker.internal:11434"
    Nexo__Meai__OllamaModel                           = "codellama:7b"
}

$envPath = Ensure-NexoDotEnvFile -RepoRoot $root.Path -Defaults $defaults
$map = Read-NexoDotEnv -Path $envPath

if ($ApiPort -le 0) { $ApiPort = Get-NexoEnvInt -Map $map -Key "NEXO_AGENT_SERVER_HTTP_PORT" -Default 8088 }
if ($OllamaHostPort -le 0) { $OllamaHostPort = Get-NexoEnvInt -Map $map -Key "NEXO_OLLAMA_HOST_PORT" -Default 11434 }
if (-not $ApiHost) { $ApiHost = if ($map["NEXO_API_HOST"]) { $map["NEXO_API_HOST"] } else { "127.0.0.1" } }
if (-not $OllamaModel) { $OllamaModel = if ($map["OLLAMA_MODEL"]) { $map["OLLAMA_MODEL"] } else { "codellama:7b" } }

if ($UseBundledOllama) {
    $OllamaBaseUrl = "http://ollama:11434"
} elseif (-not $OllamaBaseUrl) {
    $OllamaBaseUrl = "http://host.docker.internal:$OllamaHostPort"
}

# Persist effective config so compose / smoke / IDE stay aligned
Set-NexoDotEnvValue -Path $envPath -Key "NEXO_API_HOST" -Value $ApiHost
Set-NexoDotEnvValue -Path $envPath -Key "NEXO_AGENT_SERVER_HTTP_PORT" -Value "$ApiPort"
Set-NexoDotEnvValue -Path $envPath -Key "NEXO_OLLAMA_HOST_PORT" -Value "$OllamaHostPort"
Set-NexoDotEnvValue -Path $envPath -Key "OLLAMA_BASE_URL" -Value $OllamaBaseUrl
Set-NexoDotEnvValue -Path $envPath -Key "OLLAMA_MODEL" -Value $OllamaModel
Set-NexoDotEnvValue -Path $envPath -Key "Nexo__NodeCapabilityRuntime__Ollama__BaseUrl" -Value $OllamaBaseUrl
Set-NexoDotEnvValue -Path $envPath -Key "Nexo__Meai__OllamaBaseUrl" -Value $OllamaBaseUrl
Set-NexoDotEnvValue -Path $envPath -Key "Nexo__Meai__OllamaModel" -Value $OllamaModel
Set-NexoDotEnvValue -Path $envPath -Key "COMPOSE_PROJECT_NAME" -Value "nexo-fullstack-agent"
Set-NexoDotEnvValue -Path $envPath -Key "NEXO_REPO_ROOT" -Value ($root.Path -replace '\\', '/')
Set-NexoDotEnvValue -Path $envPath -Key "NEXO_BACKGROUND_AGENTS_CONFIG" -Value "/agents/agent_set.fullstack.local.json"
Set-NexoDotEnvValue -Path $envPath -Key "Nexo__RegisterBackgroundAgentHostedService" -Value "false"

Write-Host "Config: api=$ApiHost`:$ApiPort  ollamaHostPort=$OllamaHostPort  model=$OllamaModel"
Write-Host "        OLLAMA_BASE_URL=$OllamaBaseUrl"
Write-Host "        .env=$envPath"

$compose = @(
    "-f", "deploy/compose/docker-compose.agent-server.yml",
    "-f", "deploy/compose/docker-compose.agent-server.local.yml"
)

$upArgs = @("up", "-d")
if (-not $NoBuild) { $upArgs += "--build" }
$upArgs += @("nexo-api", "--no-deps")
if ($UseBundledOllama) {
    # Bring ollama + api; drop --no-deps
    $upArgs = @("up", "-d")
    if (-not $NoBuild) { $upArgs += "--build" }
}

docker compose @compose @upArgs

$baseUrl = Get-NexoApiBaseUrl -HostName $ApiHost -Port $ApiPort
$deadline = (Get-Date).AddMinutes(5)
while ((Get-Date) -lt $deadline) {
    try {
        $health = Invoke-RestMethod "$baseUrl/health" -TimeoutSec 3
        Write-Host "Healthy: $($health | ConvertTo-Json -Compress)"
        Write-Host "Portal:  $baseUrl/"
        Write-Host "IDE API: $baseUrl/api/ide/health"
        Write-Host "Director: POST $baseUrl/api/director/run"
        Write-Host "Agent set: apps/runtime-studio/config/agent_set.fullstack.local.json"
        if (-not $SkipSmokeHint) {
            Write-Host "Smoke:   .\scripts\Smoke-IdeApi.ps1 -BaseUrl $baseUrl"
            Write-Host "IDE:     set nexo.apiHost=$ApiHost nexo.apiPort=$ApiPort (or nexo.baseUrl=$baseUrl)"
        }
        exit 0
    } catch {
        Start-Sleep 5
    }
}

Write-Host "Timed out waiting for $baseUrl/health. Logs:"
docker compose @compose logs --tail 80 nexo-api
exit 1
