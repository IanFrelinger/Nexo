#!/usr/bin/env pwsh
# Comprehensive build script for Nexo

param(
    [ValidateSet("FeatureFactory", "AllPlatforms", "Main", "Features", "Tests", "Generation")]
    [string]$Target = "FeatureFactory",
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Test,
    [switch]$Verbose,
    [switch]$Help
)

if ($Help) {
    Write-Host "🚀 Nexo Build Script" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\build-nexo.ps1 [-Target <target>] [-Configuration <config>] [options]" -ForegroundColor White
    Write-Host ""
    Write-Host "Targets:" -ForegroundColor Cyan
    Write-Host "  FeatureFactory  - Build Feature Factory solution (default)" -ForegroundColor White
    Write-Host "  AllPlatforms    - Build all platform targets" -ForegroundColor White
    Write-Host "  Main            - Build main solution" -ForegroundColor White
    Write-Host "  Features        - Build features solution" -ForegroundColor White
    Write-Host "  Tests           - Build and run all tests" -ForegroundColor White
    Write-Host "  Generation      - Test platform code generation" -ForegroundColor White
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "  -Configuration  - Build configuration (Debug|Release, default: Release)" -ForegroundColor White
    Write-Host "  -Clean          - Clean before building" -ForegroundColor White
    Write-Host "  -Test           - Run tests after building" -ForegroundColor White
    Write-Host "  -Verbose        - Verbose output" -ForegroundColor White
    Write-Host "  -Help           - Show this help" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\build-nexo.ps1                                    # Build Feature Factory" -ForegroundColor White
    Write-Host "  .\build-nexo.ps1 -Target AllPlatforms -Test        # Build all platforms with tests" -ForegroundColor White
    Write-Host "  .\build-nexo.ps1 -Target Generation -Clean         # Test generation with cleanup" -ForegroundColor White
    Write-Host "  .\build-nexo.ps1 -Target Tests -Configuration Debug # Build and test in Debug mode" -ForegroundColor White
    exit 0
}

Write-Host "🚀 Nexo Build System" -ForegroundColor Green
Write-Host "Target: $Target | Configuration: $Configuration" -ForegroundColor Cyan
Write-Host ""

# Set error action preference
$ErrorActionPreference = "Stop"

$startTime = Get-Date

try {
    switch ($Target) {
        "FeatureFactory" {
            Write-Host "🎯 Building Feature Factory..." -ForegroundColor Yellow
            $args = @("-Configuration", $Configuration)
            if ($Clean) { $args += "-Clean" }
            if ($Test) { $args += "-Test" }
            if ($Verbose) { $args += "-Verbose" }
            
            & "$PSScriptRoot/build-feature-factory.ps1" @args
        }
        
        "AllPlatforms" {
            Write-Host "🌍 Building all platforms..." -ForegroundColor Yellow
            $args = @("-Configuration", $Configuration)
            if ($Clean) { $args += "-Clean" }
            if ($Test) { $args += "-Test" }
            if ($Verbose) { $args += "-Verbose" }
            
            & "$PSScriptRoot/build-all-platforms.ps1" @args
        }
        
        "Main" {
            Write-Host "🏗️ Building main solution..." -ForegroundColor Yellow
            $buildArgs = @("build", "solutions/Nexo.Main.sln", "--configuration", $Configuration)
            if ($Clean) { $buildArgs = @("clean", "solutions/Nexo.Main.sln", "--configuration", $Configuration) + $buildArgs }
            if ($Verbose) { $buildArgs += "--verbosity", "detailed" }
            
            if ($Clean) {
                dotnet clean solutions/Nexo.Main.sln --configuration $Configuration
                dotnet build solutions/Nexo.Main.sln --configuration $Configuration --no-restore
            } else {
                dotnet build solutions/Nexo.Main.sln --configuration $Configuration
            }
            
            if ($Test) {
                Write-Host "🧪 Running tests..." -ForegroundColor Yellow
                dotnet test solutions/Nexo.Main.sln --configuration $Configuration --no-build
            }
        }
        
        "Features" {
            Write-Host "🔧 Building features solution..." -ForegroundColor Yellow
            $buildArgs = @("build", "solutions/Nexo.Features.sln", "--configuration", $Configuration)
            if ($Clean) { $buildArgs = @("clean", "solutions/Nexo.Features.sln", "--configuration", $Configuration) + $buildArgs }
            if ($Verbose) { $buildArgs += "--verbosity", "detailed" }
            
            if ($Clean) {
                dotnet clean solutions/Nexo.Features.sln --configuration $Configuration
                dotnet build solutions/Nexo.Features.sln --configuration $Configuration --no-restore
            } else {
                dotnet build solutions/Nexo.Features.sln --configuration $Configuration
            }
        }
        
        "Tests" {
            Write-Host "🧪 Building and running all tests..." -ForegroundColor Yellow
            
            # Build test solution
            $buildArgs = @("build", "solutions/Nexo.Tests.sln", "--configuration", $Configuration)
            if ($Clean) { $buildArgs = @("clean", "solutions/Nexo.Tests.sln", "--configuration", $Configuration) + $buildArgs }
            if ($Verbose) { $buildArgs += "--verbosity", "detailed" }
            
            if ($Clean) {
                dotnet clean solutions/Nexo.Tests.sln --configuration $Configuration
                dotnet build solutions/Nexo.Tests.sln --configuration $Configuration --no-restore
            } else {
                dotnet build solutions/Nexo.Tests.sln --configuration $Configuration
            }
            
            # Run tests
            Write-Host "🧪 Running tests..." -ForegroundColor Yellow
            $testArgs = @("test", "solutions/Nexo.Tests.sln", "--configuration", $Configuration, "--no-build", "--verbosity", "normal")
            if ($Verbose) { $testArgs += "--verbosity", "detailed" }
            
            dotnet $testArgs
        }
        
        "Generation" {
            Write-Host "🎨 Testing platform code generation..." -ForegroundColor Yellow
            $args = @("-Configuration", $Configuration)
            if ($Clean) { $args += "-Clean" }
            if ($Verbose) { $args += "-Verbose" }
            
            & "$PSScriptRoot/test-platform-generation.ps1" @args
        }
    }
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Host ""
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
    Write-Host "⏱️ Duration: $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Cyan
    
} catch {
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Host ""
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host "⏱️ Duration: $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Cyan
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    exit 1
}
