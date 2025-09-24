using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services.Export
{
    public class DockerExporter
    {
        private readonly ILogger _logger;

        public DockerExporter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExportResult> ExportDockerAsync(
            string outputPath,
            DockerExportOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting Docker to {OutputPath}", outputPath);

                var exportPath = Path.Combine(outputPath, "nexo-docker");
                Directory.CreateDirectory(exportPath);

                // Create Dockerfile
                await CreateDockerfileAsync(exportPath, options, cancellationToken);

                // Create docker-compose.yml
                await CreateDockerComposeAsync(exportPath, options, cancellationToken);

                // Create .dockerignore
                await CreateDockerIgnoreAsync(exportPath, cancellationToken);

                // Create startup scripts
                await CreateStartupScriptsAsync(exportPath, options, cancellationToken);

                // Create configuration
                await CreateDockerConfigurationAsync(exportPath, options, cancellationToken);

                var result = new ExportResult
                {
                    Success = true,
                    ExportPath = exportPath,
                    ArtifactType = "Docker",
                    Files = Directory.GetFiles(exportPath, "*", SearchOption.AllDirectories).ToList()
                };

                _logger.LogInformation("Docker export completed successfully. {FileCount} files created.", result.Files.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export Docker");
                return new ExportResult
                {
                    Success = false,
                    Message = $"Docker export failed: {ex.Message}",
                    Files = new List<string>()
                };
            }
        }

        private async Task CreateDockerfileAsync(string exportPath, DockerExportOptions options, CancellationToken cancellationToken)
        {
            var dockerfileContent = GenerateDockerfileContent(options);
            var dockerfilePath = Path.Combine(exportPath, "Dockerfile");
            await File.WriteAllTextAsync(dockerfilePath, dockerfileContent, cancellationToken);
        }

        private async Task CreateDockerComposeAsync(string exportPath, DockerExportOptions options, CancellationToken cancellationToken)
        {
            var composeContent = GenerateDockerComposeContent(options);
            var composePath = Path.Combine(exportPath, "docker-compose.yml");
            await File.WriteAllTextAsync(composePath, composeContent, cancellationToken);
        }

        private async Task CreateDockerIgnoreAsync(string exportPath, CancellationToken cancellationToken)
        {
            var ignoreContent = GenerateDockerIgnoreContent();
            var ignorePath = Path.Combine(exportPath, ".dockerignore");
            await File.WriteAllTextAsync(ignorePath, ignoreContent, cancellationToken);
        }

        private async Task CreateStartupScriptsAsync(string exportPath, DockerExportOptions options, CancellationToken cancellationToken)
        {
            var scriptsPath = Path.Combine(exportPath, "scripts");
            Directory.CreateDirectory(scriptsPath);

            // Create startup script
            var startupContent = GenerateStartupScript(options);
            var startupPath = Path.Combine(scriptsPath, "start.sh");
            await File.WriteAllTextAsync(startupPath, startupContent, cancellationToken);

            // Create health check script
            var healthContent = GenerateHealthCheckScript();
            var healthPath = Path.Combine(scriptsPath, "healthcheck.sh");
            await File.WriteAllTextAsync(healthPath, healthContent, cancellationToken);
        }

        private async Task CreateDockerConfigurationAsync(string exportPath, DockerExportOptions options, CancellationToken cancellationToken)
        {
            var configContent = GenerateDockerConfiguration(options);
            var configPath = Path.Combine(exportPath, "docker.config.json");
            await File.WriteAllTextAsync(configPath, configContent, cancellationToken);
        }

        private string GenerateDockerfileContent(DockerExportOptions options)
        {
            return $@"FROM {options.BaseImage}

# Set working directory
WORKDIR /app

# Install dependencies
RUN apt-get update && apt-get install -y \\
    curl \\
    && rm -rf /var/lib/apt/lists/*

# Copy application files
COPY . .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT={options.Environment}
ENV ASPNETCORE_URLS=http://+:80

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \\
    CMD curl -f http://localhost/health || exit 1

# Start application
ENTRYPOINT [""dotnet"", ""Nexo.dll""]";
        }

        private string GenerateDockerComposeContent(DockerExportOptions options)
        {
            return $@"version: '3.8'

services:
  nexo:
    build: .
    ports:
      - ""{options.Port}:80""
    environment:
      - ASPNETCORE_ENVIRONMENT={options.Environment}
    volumes:
      - ./data:/app/data
    restart: unless-stopped
    healthcheck:
      test: [""CMD"", ""curl"", ""-f"", ""http://localhost/health""]
      interval: 30s
      timeout: 10s
      retries: 3";
        }

        private string GenerateDockerIgnoreContent()
        {
            return @"# Ignore build artifacts
bin/
obj/
*.dll
*.exe

# Ignore IDE files
.vs/
.vscode/
*.user
*.suo

# Ignore logs
logs/
*.log

# Ignore temporary files
tmp/
temp/
*.tmp";
        }

        private string GenerateStartupScript(DockerExportOptions options)
        {
            return @"#!/bin/bash
set -e

echo ""Starting Nexo application...""

# Wait for dependencies
echo ""Waiting for dependencies...""
sleep 5

# Start the application
echo ""Starting application...""
exec dotnet Nexo.dll";
        }

        private string GenerateHealthCheckScript()
        {
            return @"#!/bin/bash
set -e

# Check if the application is responding
curl -f http://localhost/health || exit 1

echo ""Health check passed""";
        }

        private string GenerateDockerConfiguration(DockerExportOptions options)
        {
            return $@"{{
  ""version"": ""1.0.0"",
  ""environment"": ""{options.Environment}"",
  ""port"": {options.Port},
  ""baseImage"": ""{options.BaseImage}"",
  ""features"": {{
    ""healthCheck"": true,
    ""logging"": true,
    ""monitoring"": true
  }},
  ""volumes"": [
    ""/app/data""
  ],
  ""networks"": [
    ""nexo-network""
  ]
}}";
        }
    }
}
