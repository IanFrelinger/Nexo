# Smoke-test the Nexo IDE bridge (/api/ide/* + /api/workloads/*).
# Usage:
#   .\scripts\Smoke-IdeApi.ps1
#   .\scripts\Smoke-IdeApi.ps1 -BaseUrl http://127.0.0.1:8090
#   .\scripts\Smoke-IdeApi.ps1 -ApiPort 8090
# Reads NEXO_API_HOST / NEXO_AGENT_SERVER_HTTP_PORT from repo .env when -BaseUrl omitted.
param(
    [string]$BaseUrl = "",
    [string]$ApiHost = "",
    [int]$ApiPort = 0
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\_NexoEnv.ps1"

$root = Get-NexoRepoRoot
$envPath = Join-Path $root.Path ".env"
$map = Read-NexoDotEnv -Path $envPath

if (-not $BaseUrl) {
    if (-not $ApiHost) {
        $ApiHost = if ($map["NEXO_API_HOST"]) { $map["NEXO_API_HOST"] } else { "127.0.0.1" }
    }
    if ($ApiPort -le 0) {
        $ApiPort = Get-NexoEnvInt -Map $map -Key "NEXO_AGENT_SERVER_HTTP_PORT" -Default 8088
    }
    $BaseUrl = Get-NexoApiBaseUrl -HostName $ApiHost -Port $ApiPort
}

$BaseUrl = $BaseUrl.TrimEnd("/")
$failed = 0

function Assert-Ok([string]$Name, [scriptblock]$Block) {
    try {
        $result = & $Block
        Write-Host "PASS  $Name"
        return $result
    } catch {
        Write-Host "FAIL  $Name - $($_.Exception.Message)"
        $script:failed++
        return $null
    }
}

Write-Host "Nexo IDE smoke against $BaseUrl"

$health = Assert-Ok "GET /api/ide/health" {
    $r = Invoke-WebRequest "$BaseUrl/api/ide/health" -TimeoutSec 10 -UseBasicParsing
    if ($r.Content -match "<!doctype html>" -or $r.Content -match "<html") {
        throw "SPA fallback returned HTML (IDE routes not deployed)"
    }
    $obj = $r.Content | ConvertFrom-Json
    if (-not $obj.status) { throw "status missing" }
    Write-Host "      status=$($obj.status) ollama=$($obj.ollamaAvailable) stream=$($obj.features.chatStream)"
    $obj
}

Assert-Ok "GET /api/ide/models" {
    $m = Invoke-RestMethod "$BaseUrl/api/ide/models" -TimeoutSec 30
    if (-not $m.models) { throw "models array missing" }
    Write-Host "      count=$($m.models.Count) default=$($m.defaultModelId)"
    $m
} | Out-Null

Assert-Ok "GET /api/ide/agents" {
    $a = Invoke-RestMethod "$BaseUrl/api/ide/agents" -TimeoutSec 15
    if ($null -eq $a.agents) { throw "agents array missing" }
    Write-Host "      count=$($a.agents.Count)"
    $a
} | Out-Null

Assert-Ok "POST /api/ide/session" {
    $body = @{ workspaceRoot = "C:/tmp/nexo-ide-smoke"; ideName = "smoke" } | ConvertTo-Json
    Invoke-RestMethod "$BaseUrl/api/ide/session" -Method Post -ContentType "application/json" -Body $body -TimeoutSec 10
} | Out-Null

Assert-Ok "GET /api/ide/runs" {
    $r = Invoke-RestMethod "$BaseUrl/api/ide/runs?take=5" -TimeoutSec 10
    if ($null -eq $r.runs) { throw "runs array missing" }
    Write-Host "      count=$($r.runs.Count)"
    $r
} | Out-Null

Assert-Ok "GET /api/workloads/provider" {
    $p = Invoke-RestMethod "$BaseUrl/api/workloads/provider" -TimeoutSec 10
    Write-Host "      provider=$($p.provider) available=$($p.available)"
    $p
} | Out-Null

Assert-Ok "GET /api/workloads" {
    $w = Invoke-RestMethod "$BaseUrl/api/workloads" -TimeoutSec 10
    if ($null -eq $w.workloads) { throw "workloads array missing" }
    Write-Host "      count=$($w.workloads.Count)"
    $w
} | Out-Null

if ($health -and $health.ollamaAvailable) {
    Assert-Ok "POST /api/ide/chat (short)" {
        $body = @{
            prompt = "Reply with exactly: pong"
            mode = "ask"
            contexts = @()
        } | ConvertTo-Json -Depth 4
        $res = Invoke-RestMethod "$BaseUrl/api/ide/chat" -Method Post -ContentType "application/json" -Body $body -TimeoutSec 120
        if (-not $res.runId) { throw "runId missing" }
        Write-Host "      runId=$($res.runId) msgLen=$($res.message.Length)"
        $res
    } | Out-Null
} else {
    Write-Host "SKIP  POST /api/ide/chat (ollama unavailable)"
}

if ($failed -gt 0) {
    Write-Host ""
    Write-Host "$failed check(s) failed."
    exit 1
}

Write-Host ""
Write-Host "All smoke checks passed."
exit 0
