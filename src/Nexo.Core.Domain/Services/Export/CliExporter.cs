using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Export;

namespace Nexo.Core.Domain.Services.Export
{
    public class CliExporter
    {
        private readonly ILogger _logger;

        public CliExporter(ILogger logger)
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

                _logger.LogInformation("CLI export completed successfully. {FileCount} files created.", result.Files.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export CLI");
                return new ExportResult
                {
                    Success = false,
                    Message = $"CLI export failed: {ex.Message}",
                    Files = new List<string>()
                };
            }
        }

        private async Task CreateCliExecutableAsync(string exportPath, CliExportOptions options, CancellationToken cancellationToken)
        {
            var executableName = options.Platform switch
            {
                "windows" => "nexo.exe",
                "linux" => "nexo",
                "macos" => "nexo",
                _ => "nexo"
            };

            var executablePath = Path.Combine(exportPath, executableName);
            
            // Create a simple executable script
            var executableContent = GenerateExecutableScript(options);
            await File.WriteAllTextAsync(executablePath, executableContent, cancellationToken);
            
            // TODO: Fix platform-specific file permissions
            // if (options.Platform != "windows")
            // {
            //     // Make executable on Unix systems
            //     if (!OperatingSystem.IsWindows())
            //     {
            //         await Task.Run(() => File.SetUnixFileMode(executablePath, UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite), cancellationToken);
            //     }
            // }
        }

        private async Task CreateDependenciesAsync(string dependenciesPath, CliExportOptions options, CancellationToken cancellationToken)
        {
            var dependencies = GetDependencies(options);
            
            foreach (var dependency in dependencies)
            {
                var dependencyPath = Path.Combine(dependenciesPath, dependency.Name);
                await File.WriteAllTextAsync(dependencyPath, dependency.Content, cancellationToken);
            }
        }

        private async Task CreateSampleRecipesAsync(string recipesPath, CancellationToken cancellationToken)
        {
            var recipes = GetSampleRecipes();
            
            foreach (var recipe in recipes)
            {
                var recipePath = Path.Combine(recipesPath, recipe.Name);
                await File.WriteAllTextAsync(recipePath, recipe.Content, cancellationToken);
            }
        }

        private async Task CreateConfigurationFilesAsync(string exportPath, CliExportOptions options, CancellationToken cancellationToken)
        {
            var configContent = GenerateConfigurationContent(options);
            var configPath = Path.Combine(exportPath, "nexo.config.json");
            await File.WriteAllTextAsync(configPath, configContent, cancellationToken);
        }

        private async Task CreateReadmeAsync(string exportPath, CancellationToken cancellationToken)
        {
            var readmeContent = GenerateReadmeContent();
            var readmePath = Path.Combine(exportPath, "README.md");
            await File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);
        }

        private string GenerateExecutableScript(CliExportOptions options)
        {
            return options.Platform switch
            {
                "windows" => "@echo off\necho Nexo CLI v1.0\necho Platform: Windows\necho Configuration loaded from nexo.config.json\npause",
                "linux" => "#!/bin/bash\necho \"Nexo CLI v1.0\"\necho \"Platform: Linux\"\necho \"Configuration loaded from nexo.config.json\"",
                "macos" => "#!/bin/bash\necho \"Nexo CLI v1.0\"\necho \"Platform: macOS\"\necho \"Configuration loaded from nexo.config.json\"",
                _ => "#!/bin/bash\necho \"Nexo CLI v1.0\"\necho \"Platform: Unknown\"\necho \"Configuration loaded from nexo.config.json\""
            };
        }

        private List<DependencyInfo> GetDependencies(CliExportOptions options)
        {
            return new List<DependencyInfo>
            {
                new DependencyInfo { Name = "runtime.json", Content = "{\"version\":\"1.0.0\",\"platform\":\"" + options.Platform + "\"}" },
                new DependencyInfo { Name = "dependencies.json", Content = "{\"packages\":[\"System.Text.Json\",\"Microsoft.Extensions.Logging\"]}" }
            };
        }

        private List<RecipeInfo> GetSampleRecipes()
        {
            return new List<RecipeInfo>
            {
                new RecipeInfo { Name = "hello-world.yaml", Content = "name: Hello World\nsteps:\n  - echo \"Hello from Nexo!\"" },
                new RecipeInfo { Name = "build-project.yaml", Content = "name: Build Project\nsteps:\n  - dotnet build\n  - dotnet test" }
            };
        }

        private string GenerateConfigurationContent(CliExportOptions options)
        {
            return $@"{{
  ""version"": ""1.0.0"",
  ""platform"": ""{options.Platform}"",
  ""features"": {{
    ""ai"": {options.EnableAI.ToString().ToLower()},
    ""platform"": {options.EnablePlatform.ToString().ToLower()},
    ""analysis"": {options.EnableAnalysis.ToString().ToLower()}
  }},
  ""logging"": {{
    ""level"": ""Information"",
    ""output"": ""console""
  }}
}}";
        }

        private string GenerateReadmeContent()
        {
            return @"# Nexo CLI

A powerful command-line interface for the Nexo platform.

## Features
- AI-powered code generation
- Platform-specific optimizations
- Code analysis and quality checks
- Recipe-based automation

## Usage
1. Run the executable
2. Follow the interactive prompts
3. Use recipes for automation

## Configuration
Edit `nexo.config.json` to customize behavior.

## Dependencies
See `dependencies/` folder for required packages.
";
        }
    }

    public class DependencyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class RecipeInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
