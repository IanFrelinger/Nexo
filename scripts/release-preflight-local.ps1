#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)][string] $Version
)

$ErrorActionPreference = "Stop"
$Version = $Version.TrimStart("v", "V")
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

if ($Version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$') {
    throw "Invalid SemVer: $Version"
}
$canonical = (Get-Content (Join-Path $Root "VERSION") -Raw).Trim()
if ($Version -ne $canonical) {
    throw "Version $Version does not match root VERSION ($canonical). Update VERSION before release preflight."
}
if ($env:ASHLAR_RELEASE_AUDIT -eq "1" -and $env:ASHLAR_RELEASE_PREFLIGHT_TRIGGER_GATE -eq "1") {
    throw "Release audit mode never dispatches external workflows."
}

Write-Host "== 1/2 Pack graph vs MSBuild (Ashlar.Hosting) =="
$py = @("python3", "python") | ForEach-Object { Get-Command $_ -ErrorAction SilentlyContinue } | Select-Object -First 1
if (-not $py) { throw "Python required on PATH (python3 or python)" }
& $py.Source (Join-Path $Root "scripts/verify-pack-ashlar-hosting-graph-alignment.py")

Write-Host "== 2/2 NuGet consumer sample =="
$env:ASHLAR_SDK_PACKAGE_VERSION = $Version
& (Join-Path $Root "scripts/verify-stable-sdk-host-sample-packages.ps1") -Version $Version

if ($env:ASHLAR_RELEASE_PREFLIGHT_TRIGGER_GATE -eq "1") {
    $ref = if ($env:ASHLAR_RELEASE_PREFLIGHT_REF) { $env:ASHLAR_RELEASE_PREFLIGHT_REF } else { "master" }
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($gh) {
        Write-Host "== Optional: gh workflow run Runtime Release Gate on $ref =="
        gh workflow run "Runtime Release Gate" --ref $ref
    } else {
        Write-Warning "ASHLAR_RELEASE_PREFLIGHT_TRIGGER_GATE=1 but gh not found; skip."
    }
}

Write-Host ""
Write-Host "Preflight OK for version $Version."
Write-Host "Next: git tag v$Version && git push origin v$Version"
Write-Host "      dotnet run --project application/src/Ashlar.CLI -- release preflight $Version"
