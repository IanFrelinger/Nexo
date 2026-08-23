# Start the local self-hosted Ashlar agent server (full-stack lane) on Windows.
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
. "$PSScriptRoot\_AshlarEnv.ps1"

$root = Get-AshlarRepoRoot
Set-Location $root

$defaults = @{
    COMPOSE_PROJECT_NAME                              = "ashlar-fullstack-agent"
    ASHLAR_REPO_ROOT                                    = ($root.Path -replace '\\', '/')
    ASHLAR_AGENT_SERVER_HTTP_PORT                       = "8088"
    ASHLAR_OLLAMA_HOST_PORT                             = "11434"
    ASHLAR_API_HOST                                     = "127.0.0.1"
    OLLAMA_BASE_URL                                   = "http://host.docker.internal:11434"
    OLLAMA_MODEL                                      = "codellama:7b"
    ASHLAR_BACKGROUND_AGENTS_CONFIG                     = "/agents/agent_set.fullstack.local.json"
    Ashlar__RegisterBackgroundAgentHostedService        = "false"
    Ashlar__NodeCapabilityRuntime__Ollama__BaseUrl      = "http://host.docker.internal:11434"
    Ashlar__Meai__OllamaBaseUrl                         = "http://host.docker.internal:11434"
    Ashlar__Meai__OllamaModel                           = "codellama:7b"
}

$envPath = Ensure-AshlarDotEnvFile -RepoRoot $root.Path -Defaults $defaults
$map = Read-AshlarDotEnv -Path $envPath

if ($ApiPort -le 0) { $ApiPort = Get-AshlarEnvInt -Map $map -Key "ASHLAR_AGENT_SERVER_HTTP_PORT" -Default 8088 }
if ($OllamaHostPort -le 0) { $OllamaHostPort = Get-AshlarEnvInt -Map $map -Key "ASHLAR_OLLAMA_HOST_PORT" -Default 11434 }
if (-not $ApiHost) { $ApiHost = if ($map["ASHLAR_API_HOST"]) { $map["ASHLAR_API_HOST"] } else { "127.0.0.1" } }
if (-not $OllamaModel) { $OllamaModel = if ($map["OLLAMA_MODEL"]) { $map["OLLAMA_MODEL"] } else { "codellama:7b" } }

if ($UseBundledOllama) {
    $OllamaBaseUrl = "http://ollama:11434"
} elseif (-not $OllamaBaseUrl) {
    $OllamaBaseUrl = "http://host.docker.internal:$OllamaHostPort"
}

# Persist effective config so compose / smoke / IDE stay aligned
Set-AshlarDotEnvValue -Path $envPath -Key "ASHLAR_API_HOST" -Value $ApiHost
Set-AshlarDotEnvValue -Path $envPath -Key "ASHLAR_AGENT_SERVER_HTTP_PORT" -Value "$ApiPort"
Set-AshlarDotEnvValue -Path $envPath -Key "ASHLAR_OLLAMA_HOST_PORT" -Value "$OllamaHostPort"
Set-AshlarDotEnvValue -Path $envPath -Key "OLLAMA_BASE_URL" -Value $OllamaBaseUrl
Set-AshlarDotEnvValue -Path $envPath -Key "OLLAMA_MODEL" -Value $OllamaModel
Set-AshlarDotEnvValue -Path $envPath -Key "Ashlar__NodeCapabilityRuntime__Ollama__BaseUrl" -Value $OllamaBaseUrl
Set-AshlarDotEnvValue -Path $envPath -Key "Ashlar__Meai__OllamaBaseUrl" -Value $OllamaBaseUrl
Set-AshlarDotEnvValue -Path $envPath -Key "Ashlar__Meai__OllamaModel" -Value $OllamaModel
Set-AshlarDotEnvValue -Path $envPath -Key "COMPOSE_PROJECT_NAME" -Value "ashlar-fullstack-agent"
Set-AshlarDotEnvValue -Path $envPath -Key "ASHLAR_REPO_ROOT" -Value ($root.Path -replace '\\', '/')
Set-AshlarDotEnvValue -Path $envPath -Key "ASHLAR_BACKGROUND_AGENTS_CONFIG" -Value "/agents/agent_set.fullstack.local.json"
Set-AshlarDotEnvValue -Path $envPath -Key "Ashlar__RegisterBackgroundAgentHostedService" -Value "false"

Write-Host "Config: api=$ApiHost`:$ApiPort  ollamaHostPort=$OllamaHostPort  model=$OllamaModel"
Write-Host "        OLLAMA_BASE_URL=$OllamaBaseUrl"
Write-Host "        .env=$envPath"

$compose = @(
    "-f", "deploy/compose/docker-compose.agent-server.yml",
    "-f", "deploy/compose/docker-compose.agent-server.local.yml"
)

$upArgs = @("up", "-d")
if (-not $NoBuild) { $upArgs += "--build" }
$upArgs += @("ashlar-api", "--no-deps")
if ($UseBundledOllama) {
    # Bring ollama + api; drop --no-deps
    $upArgs = @("up", "-d")
    if (-not $NoBuild) { $upArgs += "--build" }
}

docker compose @compose @upArgs

$baseUrl = Get-AshlarApiBaseUrl -HostName $ApiHost -Port $ApiPort
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
            Write-Host "IDE:     set ashlar.apiHost=$ApiHost ashlar.apiPort=$ApiPort (or ashlar.baseUrl=$baseUrl)"
        }
        exit 0
    } catch {
        Start-Sleep 5
    }
}

Write-Host "Timed out waiting for $baseUrl/health. Logs:"
docker compose @compose logs --tail 80 ashlar-api
exit 1
