#!/usr/bin/env pwsh
# Build script for Nexo Feature Factory development

param(
    [string]$Configuration = "Release",
    [string]$Framework = "",
    [switch]$Clean,
    [switch]$Test,
    [switch]$Verbose
)

Write-Host "🚀 Building Nexo Feature Factory..." -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Clean if requested
if ($Clean) {
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean solutions/Nexo.FeatureFactory.sln --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Clean failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

# Restore packages
Write-Host "📦 Restoring packages..." -ForegroundColor Yellow
$restoreArgs = @("restore", "solutions/Nexo.FeatureFactory.sln")
if ($Verbose) { $restoreArgs += "--verbosity", "detailed" }

dotnet $restoreArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Restore failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Build solution
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
$buildArgs = @("build", "solutions/Nexo.FeatureFactory.sln", "--configuration", $Configuration, "--no-restore")
if ($Framework) { $buildArgs += "--framework", $Framework }
if ($Verbose) { $buildArgs += "--verbosity", "detailed" }

dotnet $buildArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Run tests if requested
if ($Test) {
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow
    $testArgs = @("test", "solutions/Nexo.FeatureFactory.sln", "--configuration", $Configuration, "--no-build", "--verbosity", "normal")
    if ($Framework) { $testArgs += "--framework", $Framework }
    
    dotnet $testArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

Write-Host "✅ Feature Factory build complete!" -ForegroundColor Green

# Display build summary
Write-Host ""
Write-Host "📊 Build Summary:" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration" -ForegroundColor White
if ($Framework) { Write-Host "  Framework: $Framework" -ForegroundColor White }
Write-Host "  Solution: Nexo.FeatureFactory.sln" -ForegroundColor White
Write-Host "  Tests: $(if ($Test) { 'Yes' } else { 'No' })" -ForegroundColor White
Write-Host ""
