#!/usr/bin/env pwsh
# Build script for all platform targets

param(
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Test,
    [switch]$Verbose
)

Write-Host "🌍 Building all platform targets..." -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Define target frameworks
$targets = @("net8.0", "net6.0", "netstandard2.0")
$solutions = @(
    "solutions/Nexo.FeatureFactory.sln",
    "solutions/Nexo.Features.sln"
)

# Clean if requested
if ($Clean) {
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    foreach ($solution in $solutions) {
        Write-Host "  Cleaning $solution..." -ForegroundColor Gray
        dotnet clean $solution --configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Clean failed for $solution"
        }
    }
}

# Build for each target framework
foreach ($target in $targets) {
    Write-Host ""
    Write-Host "🎯 Building for $target..." -ForegroundColor Yellow
    
    foreach ($solution in $solutions) {
        Write-Host "  Building $solution..." -ForegroundColor Gray
        
        # Restore packages
        $restoreArgs = @("restore", $solution)
        if ($Verbose) { $restoreArgs += "--verbosity", "detailed" }
        
        dotnet $restoreArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Restore failed for $solution on $target"
            continue
        }
        
        # Build solution
        $buildArgs = @("build", $solution, "--configuration", $Configuration, "--framework", $target, "--no-restore")
        if ($Verbose) { $buildArgs += "--verbosity", "detailed" }
        
        dotnet $buildArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Build failed for $solution on $target"
            continue
        }
        
        # Run tests if requested
        if ($Test) {
            Write-Host "    Running tests..." -ForegroundColor Gray
            $testArgs = @("test", $solution, "--configuration", $Configuration, "--framework", $target, "--no-build", "--verbosity", "minimal")
            
            dotnet $testArgs
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Tests failed for $solution on $target"
            }
        }
    }
}

Write-Host ""
Write-Host "✅ All platform builds complete!" -ForegroundColor Green

# Display build summary
Write-Host ""
Write-Host "📊 Build Summary:" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration" -ForegroundColor White
Write-Host "  Target Frameworks: $($targets -join ', ')" -ForegroundColor White
Write-Host "  Solutions: $($solutions.Count)" -ForegroundColor White
Write-Host "  Tests: $(if ($Test) { 'Yes' } else { 'No' })" -ForegroundColor White
Write-Host ""
