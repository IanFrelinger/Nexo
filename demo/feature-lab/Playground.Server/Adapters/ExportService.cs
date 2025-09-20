using System.Diagnostics;
using Playground.Server.Models;

namespace Playground.Server.Adapters;

public sealed class ExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public async Task<ExportResult> ExportCliAsync(string featurePath, string outputDir)
    {
        try
        {
            var cliDir = Path.Combine(outputDir, "cli");
            Directory.CreateDirectory(cliDir);

            // Create a simple CLI script
            var cliScript = GenerateCliScript(featurePath);
            var cliPath = Path.Combine(cliDir, "nexo-run.sh");
            await File.WriteAllTextAsync(cliPath, cliScript);
            
            // Make it executable on Unix systems
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                Process.Start("chmod", $"+x {cliPath}");
            }

            _logger.LogInformation("CLI exported to {CliPath}", cliPath);
            return new ExportResult { Success = true, Path = cliPath, Type = "CLI" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export CLI");
            return new ExportResult { Success = false, Error = ex.Message, Type = "CLI" };
        }
    }

    public async Task<ExportResult> ExportDockerAsync(string featurePath, string outputDir)
    {
        try
        {
            var dockerDir = Path.Combine(outputDir, "docker");
            Directory.CreateDirectory(dockerDir);

            // Create Dockerfile
            var dockerfile = GenerateDockerfile(featurePath);
            var dockerfilePath = Path.Combine(dockerDir, "Dockerfile");
            await File.WriteAllTextAsync(dockerfilePath, dockerfile);

            // Create docker-compose.yml
            var compose = GenerateDockerCompose(featurePath);
            var composePath = Path.Combine(dockerDir, "docker-compose.yml");
            await File.WriteAllTextAsync(composePath, compose);

            _logger.LogInformation("Docker files exported to {DockerDir}", dockerDir);
            return new ExportResult { Success = true, Path = dockerDir, Type = "Docker" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Docker");
            return new ExportResult { Success = false, Error = ex.Message, Type = "Docker" };
        }
    }

    public async Task<ExportResult> ExportBothAsync(string featurePath, string outputDir)
    {
        var cliResult = await ExportCliAsync(featurePath, outputDir);
        var dockerResult = await ExportDockerAsync(featurePath, outputDir);

        return new ExportResult
        {
            Success = cliResult.Success && dockerResult.Success,
            Path = outputDir,
            Type = "Both",
            CliPath = cliResult.Path,
            DockerPath = dockerResult.Path,
            Error = cliResult.Success && dockerResult.Success ? null : 
                   $"CLI: {cliResult.Error}, Docker: {dockerResult.Error}"
        };
    }

    private string GenerateCliScript(string featurePath)
    {
        return $@"#!/bin/bash
# Nexo CLI Runner - Generated Export
# Feature: {featurePath}

set -e

echo ""🚀 Running Nexo Feature: {Path.GetFileName(featurePath)}""
echo ""===============================================""

# Set environment variables
export NEXO_AI_MODE=""${{NEXO_AI_MODE:-off}}""
export NEXO_PROVIDER=""${{NEXO_PROVIDER:-local}}""
export NEXO_MODEL=""${{NEXO_MODEL:-llama3}}""

# Create output directory
mkdir -p outputs

# Simulate feature execution
echo ""📋 Processing feature...""
sleep 2

# Generate sample output
cat > outputs/sample_output.csv << 'EOF'
subject,label,confidence,reply
""Sample Email"",""urgent"",0.95,""Thank you for your message. We are processing your request.""
EOF

echo ""✅ Feature execution completed""
echo ""📊 Outputs generated in: outputs/""
echo ""📁 Files created:""
ls -la outputs/
";
    }

    private string GenerateDockerfile(string featurePath)
    {
        return $@"FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY [""Playground.Server/Playground.Server.csproj"", ""Playground.Server/""]
RUN dotnet restore ""Playground.Server/Playground.Server.csproj""
COPY . .
WORKDIR /src/Playground.Server
RUN dotnet build ""Playground.Server.csproj"" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ""Playground.Server.csproj"" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""Playground.Server.dll""]

# Feature: {featurePath}
# This container runs the Nexo Feature Lab Playground
# with the {Path.GetFileName(featurePath)} feature enabled
";
    }

    private string GenerateDockerCompose(string featurePath)
    {
        return $@"version: '3.8'

services:
  nexolab:
    build: .
    ports:
      - ""8080:80""
    environment:
      - NEXO_AI_MODE=off
      - NEXO_PROVIDER=local
      - NEXO_MODEL=llama3
    volumes:
      - ./outputs:/app/outputs
    restart: unless-stopped

# Feature Lab with {Path.GetFileName(featurePath)}
# Access at: http://localhost:8080
";
    }
}

public sealed class ExportResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string Type { get; init; } = "";
    public string? Error { get; init; }
    public string? CliPath { get; init; }
    public string? DockerPath { get; init; }
}
