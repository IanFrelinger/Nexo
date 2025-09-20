using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Service for exporting CLI and Docker artifacts
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;

        public ExportService(ILogger<ExportService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExportResult> ExportCliAsync(
            string outputPath,
            CliExportOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting CLI to {OutputPath}", outputPath);

                var exportPath = Path.Combine(outputPath, "nexo-cli");
                Directory.CreateDirectory(exportPath);

                // Create CLI executable
                await CreateCliExecutableAsync(exportPath, options, cancellationToken);

                // Create dependencies directory
                var dependenciesPath = Path.Combine(exportPath, "dependencies");
                Directory.CreateDirectory(dependenciesPath);
                await CreateDependenciesAsync(dependenciesPath, options, cancellationToken);

                // Create sample recipes
                var recipesPath = Path.Combine(exportPath, "recipes");
                Directory.CreateDirectory(recipesPath);
                await CreateSampleRecipesAsync(recipesPath, cancellationToken);

                // Create configuration files
                await CreateConfigurationFilesAsync(exportPath, options, cancellationToken);

                // Create README
                await CreateReadmeAsync(exportPath, cancellationToken);

                var result = new ExportResult
                {
                    Success = true,
                    ExportPath = exportPath,
                    ArtifactType = "CLI",
                    Files = Directory.GetFiles(exportPath, "*", SearchOption.AllDirectories).ToList()
                };

                _logger.LogInformation("Successfully exported CLI to {ExportPath} with {FileCount} files", 
                    exportPath, result.Files.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export CLI to {OutputPath}", outputPath);
                return new ExportResult
                {
                    Success = false,
                    ExportPath = outputPath,
                    ArtifactType = "CLI",
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ExportResult> ExportDockerAsync(
            string outputPath,
            DockerExportOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting Docker image to {OutputPath}", outputPath);

                var exportPath = Path.Combine(outputPath, "nexo-docker");
                Directory.CreateDirectory(exportPath);

                // Create Dockerfile
                await CreateDockerfileAsync(exportPath, options, cancellationToken);

                // Create docker-compose.yml
                await CreateDockerComposeAsync(exportPath, options, cancellationToken);

                // Create application files
                var appPath = Path.Combine(exportPath, "app");
                Directory.CreateDirectory(appPath);
                await CreateDockerApplicationAsync(appPath, options, cancellationToken);

                // Create sample recipes
                var recipesPath = Path.Combine(exportPath, "recipes");
                Directory.CreateDirectory(recipesPath);
                await CreateSampleRecipesAsync(recipesPath, cancellationToken);

                // Create build script
                await CreateBuildScriptAsync(exportPath, cancellationToken);

                var result = new ExportResult
                {
                    Success = true,
                    ExportPath = exportPath,
                    ArtifactType = "Docker",
                    Files = Directory.GetFiles(exportPath, "*", SearchOption.AllDirectories).ToList()
                };

                _logger.LogInformation("Successfully exported Docker to {ExportPath} with {FileCount} files", 
                    exportPath, result.Files.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export Docker to {OutputPath}", outputPath);
                return new ExportResult
                {
                    Success = false,
                    ExportPath = outputPath,
                    ArtifactType = "Docker",
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ExportResult> ExportPortableAsync(
            string outputPath,
            PortableExportOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting portable package to {OutputPath}", outputPath);

                var exportPath = Path.Combine(outputPath, "nexo-portable");
                Directory.CreateDirectory(exportPath);

                // Create portable executable
                await CreatePortableExecutableAsync(exportPath, options, cancellationToken);

                // Create configuration
                await CreatePortableConfigurationAsync(exportPath, options, cancellationToken);

                // Create sample recipes
                var recipesPath = Path.Combine(exportPath, "recipes");
                Directory.CreateDirectory(recipesPath);
                await CreateSampleRecipesAsync(recipesPath, cancellationToken);

                var result = new ExportResult
                {
                    Success = true,
                    ExportPath = exportPath,
                    ArtifactType = "Portable",
                    Files = Directory.GetFiles(exportPath, "*", SearchOption.AllDirectories).ToList()
                };

                _logger.LogInformation("Successfully exported portable package to {ExportPath} with {FileCount} files", 
                    exportPath, result.Files.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export portable package to {OutputPath}", outputPath);
                return new ExportResult
                {
                    Success = false,
                    ExportPath = outputPath,
                    ArtifactType = "Portable",
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> ValidateExportAsync(
            string exportPath,
            string artifactType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(exportPath))
                {
                    _logger.LogWarning("Export path does not exist: {ExportPath}", exportPath);
                    return false;
                }

                var requiredFiles = GetRequiredFiles(artifactType);
                var missingFiles = new List<string>();

                foreach (var requiredFile in requiredFiles)
                {
                    var filePath = Path.Combine(exportPath, requiredFile);
                    if (!File.Exists(filePath))
                    {
                        missingFiles.Add(requiredFile);
                    }
                }

                if (missingFiles.Any())
                {
                    _logger.LogWarning("Missing required files in export {ExportPath}: {MissingFiles}", 
                        exportPath, string.Join(", ", missingFiles));
                    return false;
                }

                _logger.LogInformation("Export validation passed for {ArtifactType} at {ExportPath}", 
                    artifactType, exportPath);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate export at {ExportPath}", exportPath);
                return false;
            }
        }

        private async Task CreateCliExecutableAsync(string exportPath, CliExportOptions options, CancellationToken cancellationToken)
        {
            var executablePath = Path.Combine(exportPath, options.ExecutableName);
            var executableContent = GenerateCliExecutable(options);
            await File.WriteAllTextAsync(executablePath, executableContent, cancellationToken);
        }

        private async Task CreateDependenciesAsync(string dependenciesPath, CliExportOptions options, CancellationToken cancellationToken)
        {
            var dependenciesContent = GenerateDependenciesList(options);
            await File.WriteAllTextAsync(Path.Combine(dependenciesPath, "dependencies.txt"), dependenciesContent, cancellationToken);
        }

        private async Task CreateSampleRecipesAsync(string recipesPath, CancellationToken cancellationToken)
        {
            var sampleRecipes = new[]
            {
                ("triage.yaml", GenerateTriageRecipe()),
                ("summarize.yaml", GenerateSummarizeRecipe()),
                ("translate.yaml", GenerateTranslateRecipe())
            };

            foreach (var (fileName, content) in sampleRecipes)
            {
                await File.WriteAllTextAsync(Path.Combine(recipesPath, fileName), content, cancellationToken);
            }
        }

        private async Task CreateConfigurationFilesAsync(string exportPath, CliExportOptions options, CancellationToken cancellationToken)
        {
            var configContent = GenerateConfiguration(options);
            await File.WriteAllTextAsync(Path.Combine(exportPath, "nexo.config.json"), configContent, cancellationToken);
        }

        private async Task CreateReadmeAsync(string exportPath, CancellationToken cancellationToken)
        {
            var readmeContent = GenerateReadme();
            await File.WriteAllTextAsync(Path.Combine(exportPath, "README.md"), readmeContent, cancellationToken);
        }

        private async Task CreateDockerfileAsync(string exportPath, DockerExportOptions options, CancellationToken cancellationToken)
        {
            var dockerfileContent = GenerateDockerfile(options);
            await File.WriteAllTextAsync(Path.Combine(exportPath, "Dockerfile"), dockerfileContent, cancellationToken);
        }

        private async Task CreateDockerComposeAsync(string exportPath, DockerExportOptions options, CancellationToken cancellationToken)
        {
            var composeContent = GenerateDockerCompose(options);
            await File.WriteAllTextAsync(Path.Combine(exportPath, "docker-compose.yml"), composeContent, cancellationToken);
        }

        private async Task CreateDockerApplicationAsync(string appPath, DockerExportOptions options, CancellationToken cancellationToken)
        {
            var appContent = GenerateDockerApplication(options);
            await File.WriteAllTextAsync(Path.Combine(appPath, "Program.cs"), appContent, cancellationToken);
        }

        private async Task CreateBuildScriptAsync(string exportPath, CancellationToken cancellationToken)
        {
            var buildScriptContent = GenerateBuildScript();
            await File.WriteAllTextAsync(Path.Combine(exportPath, "build.sh"), buildScriptContent, cancellationToken);
        }

        private async Task CreatePortableExecutableAsync(string exportPath, PortableExportOptions options, CancellationToken cancellationToken)
        {
            var executablePath = Path.Combine(exportPath, options.ExecutableName);
            var executableContent = GeneratePortableExecutable(options);
            await File.WriteAllTextAsync(executablePath, executableContent, cancellationToken);
        }

        private async Task CreatePortableConfigurationAsync(string exportPath, PortableExportOptions options, CancellationToken cancellationToken)
        {
            var configContent = GeneratePortableConfiguration(options);
            await File.WriteAllTextAsync(Path.Combine(exportPath, "nexo.config.json"), configContent, cancellationToken);
        }

        private List<string> GetRequiredFiles(string artifactType)
        {
            return artifactType switch
            {
                "CLI" => new List<string> { "nexo-cli", "dependencies/dependencies.txt", "recipes", "nexo.config.json", "README.md" },
                "Docker" => new List<string> { "Dockerfile", "docker-compose.yml", "app/Program.cs", "recipes", "build.sh" },
                "Portable" => new List<string> { "nexo-portable", "nexo.config.json", "recipes" },
                _ => new List<string>()
            };
        }

        private string GenerateCliExecutable(CliExportOptions options)
        {
            return $@"#!/bin/bash
# Nexo CLI Executable
# Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

echo ""Nexo CLI v{options.Version}""
echo ""Executing with options: {string.Join(" ", options.Arguments)}""

# Add your CLI logic here
echo ""CLI execution started""
echo ""Processing completed""
echo ""CLI execution finished""
";
        }

        private string GenerateDependenciesList(CliExportOptions options)
        {
            return @"Microsoft.Extensions.DependencyInjection
Microsoft.Extensions.Logging
Microsoft.Extensions.Configuration
System.Text.Json
Newtonsoft.Json
";
        }

        private string GenerateTriageRecipe()
        {
            return @"name: triage
description: Email triage and classification
blocks:
  - name: email-parser
    type: input
  - name: classifier
    type: ai
    model: gpt-4
  - name: labeler
    type: output
";
        }

        private string GenerateSummarizeRecipe()
        {
            return @"name: summarize
description: Document summarization
blocks:
  - name: document-loader
    type: input
  - name: summarizer
    type: ai
    model: gpt-4
  - name: formatter
    type: output
";
        }

        private string GenerateTranslateRecipe()
        {
            return @"name: translate
description: Text translation
blocks:
  - name: text-input
    type: input
  - name: translator
    type: ai
    model: gpt-4
  - name: text-output
    type: output
";
        }

        private string GenerateConfiguration(CliExportOptions options)
        {
            return @"{
  ""version"": ""1.0.0"",
  ""ai_mode"": ""off"",
  ""providers"": {
    ""local"": {
      ""enabled"": true,
      ""model"": ""llama3""
    },
    ""openai"": {
      ""enabled"": false,
      ""model"": ""gpt-4""
    }
  },
  ""export_options"": {
    ""include_dependencies"": true,
    ""include_recipes"": true
  }
}";
        }

        private string GenerateReadme()
        {
            return @"# Nexo CLI

A powerful command-line interface for the Nexo platform.

## Usage

```bash
./nexo-cli --help
./nexo-cli run recipe.yaml
./nexo-cli generate --type feature --name MyFeature
```

## Configuration

Edit `nexo.config.json` to configure AI modes and providers.

## Recipes

Sample recipes are included in the `recipes/` directory.
";
        }

        private string GenerateDockerfile(DockerExportOptions options)
        {
            return $@"FROM mcr.microsoft.com/dotnet/aspnet:{options.DotNetVersion}
WORKDIR /app
COPY app/ .
COPY recipes/ ./recipes/
RUN dotnet restore
RUN dotnet build -c Release
EXPOSE {options.Port}
ENTRYPOINT [""dotnet"", ""Nexo.Docker.dll""]
";
        }

        private string GenerateDockerCompose(DockerExportOptions options)
        {
            return $@"version: '3.8'
services:
  nexo:
    build: .
    ports:
      - ""{options.Port}:{options.Port}""
    environment:
      - NEXO_AI_MODE=off
    volumes:
      - ./recipes:/app/recipes
";
        }

        private string GenerateDockerApplication(DockerExportOptions options)
        {
            return @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
";
        }

        private string GenerateBuildScript()
        {
            return @"#!/bin/bash
echo ""Building Nexo Docker image...""
docker build -t nexo:latest .
echo ""Build complete!""
echo ""Run with: docker-compose up""
";
        }

        private string GeneratePortableExecutable(PortableExportOptions options)
        {
            return $@"#!/bin/bash
# Nexo Portable Executable
# Version: {options.Version}

echo ""Nexo Portable v{options.Version}""
echo ""Running in portable mode...""

# Portable execution logic
echo ""Portable execution started""
echo ""Processing completed""
echo ""Portable execution finished""
";
        }

        private string GeneratePortableConfiguration(PortableExportOptions options)
        {
            return @"{
  ""version"": ""1.0.0"",
  ""mode"": ""portable"",
  ""ai_mode"": ""off"",
  ""offline"": true
}";
        }
    }

    /// <summary>
    /// CLI export options
    /// </summary>
    public class CliExportOptions
    {
        public string ExecutableName { get; set; } = "nexo-cli";
        public string Version { get; set; } = "1.0.0";
        public List<string> Arguments { get; set; } = new();
        public bool IncludeDependencies { get; set; } = true;
        public bool IncludeRecipes { get; set; } = true;
    }

    /// <summary>
    /// Docker export options
    /// </summary>
    public class DockerExportOptions
    {
        public string DotNetVersion { get; set; } = "8.0";
        public int Port { get; set; } = 8080;
        public string ImageName { get; set; } = "nexo";
        public string Tag { get; set; } = "latest";
    }

    /// <summary>
    /// Portable export options
    /// </summary>
    public class PortableExportOptions
    {
        public string ExecutableName { get; set; } = "nexo-portable";
        public string Version { get; set; } = "1.0.0";
        public bool Offline { get; set; } = true;
    }

    /// <summary>
    /// Export result
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string ExportPath { get; set; } = string.Empty;
        public string ArtifactType { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Interface for export service
    /// </summary>
    public interface IExportService
    {
        Task<ExportResult> ExportCliAsync(
            string outputPath,
            CliExportOptions options,
            CancellationToken cancellationToken = default);

        Task<ExportResult> ExportDockerAsync(
            string outputPath,
            DockerExportOptions options,
            CancellationToken cancellationToken = default);

        Task<ExportResult> ExportPortableAsync(
            string outputPath,
            PortableExportOptions options,
            CancellationToken cancellationToken = default);

        Task<bool> ValidateExportAsync(
            string exportPath,
            string artifactType,
            CancellationToken cancellationToken = default);
    }
}
