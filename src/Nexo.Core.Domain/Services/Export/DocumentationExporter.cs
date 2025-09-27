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
    public partial class DocumentationExporter
    {
        private readonly ILogger _logger;

        public DocumentationExporter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExportResult> ExportDocumentationAsync(
            string outputPath,
            DocumentationExportOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting documentation to {OutputPath}", outputPath);

                var exportPath = Path.Combine(outputPath, "nexo-docs");
                Directory.CreateDirectory(exportPath);

                // Create main documentation
                await CreateMainDocumentationAsync(exportPath, options, cancellationToken);

                // Create API documentation
                if (options.IncludeApiDocs)
                {
                    await CreateApiDocumentationAsync(exportPath, options, cancellationToken);
                }

                // Create user guides
                if (options.IncludeUserGuides)
                {
                    await CreateUserGuidesAsync(exportPath, options, cancellationToken);
                }

                // Create developer guides
                if (options.IncludeDeveloperGuides)
                {
                    await CreateDeveloperGuidesAsync(exportPath, options, cancellationToken);
                }

                // Create examples
                if (options.IncludeExamples)
                {
                    await CreateExamplesAsync(exportPath, options, cancellationToken);
                }

                // Create configuration files
                await CreateDocumentationConfigurationAsync(exportPath, options, cancellationToken);

                var result = new ExportResult
                {
                    Success = true,
                    ExportPath = exportPath,
                    ArtifactType = "Documentation",
                    Files = Directory.GetFiles(exportPath, "*", SearchOption.AllDirectories).ToList()
                };

                _logger.LogInformation("Documentation export completed successfully. {FileCount} files created.", result.Files.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export documentation");
                return new ExportResult
                {
                    Success = false,
                    Message = $"Documentation export failed: {ex.Message}",
                    Files = new List<string>()
                };
            }
        }

        private async Task CreateMainDocumentationAsync(string exportPath, DocumentationExportOptions options, CancellationToken cancellationToken)
        {
            // Create main README
            var readmeContent = GenerateMainReadme(options);
            var readmePath = Path.Combine(exportPath, "README.md");
            await File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);

            // Create getting started guide
            var gettingStartedContent = GenerateGettingStartedGuide(options);
            var gettingStartedPath = Path.Combine(exportPath, "getting-started.md");
            await File.WriteAllTextAsync(gettingStartedPath, gettingStartedContent, cancellationToken);
        }

        private async Task CreateApiDocumentationAsync(string exportPath, DocumentationExportOptions options, CancellationToken cancellationToken)
        {
            var apiPath = Path.Combine(exportPath, "api");
            Directory.CreateDirectory(apiPath);

            // Create API overview
            var apiOverviewContent = GenerateApiOverview();
            var apiOverviewPath = Path.Combine(apiPath, "overview.md");
            await File.WriteAllTextAsync(apiOverviewPath, apiOverviewContent, cancellationToken);

            // Create API reference
            var apiReferenceContent = GenerateApiReference();
            var apiReferencePath = Path.Combine(apiPath, "reference.md");
            await File.WriteAllTextAsync(apiReferencePath, apiReferenceContent, cancellationToken);
        }

        private async Task CreateUserGuidesAsync(string exportPath, DocumentationExportOptions options, CancellationToken cancellationToken)
        {
            var guidesPath = Path.Combine(exportPath, "guides");
            Directory.CreateDirectory(guidesPath);

            // Create user guide
            var userGuideContent = GenerateUserGuide();
            var userGuidePath = Path.Combine(guidesPath, "user-guide.md");
            await File.WriteAllTextAsync(userGuidePath, userGuideContent, cancellationToken);

            // Create troubleshooting guide
            var troubleshootingContent = GenerateTroubleshootingGuide();
            var troubleshootingPath = Path.Combine(guidesPath, "troubleshooting.md");
            await File.WriteAllTextAsync(troubleshootingPath, troubleshootingContent, cancellationToken);
        }

        private async Task CreateDeveloperGuidesAsync(string exportPath, DocumentationExportOptions options, CancellationToken cancellationToken)
        {
            var devPath = Path.Combine(exportPath, "developer");
            Directory.CreateDirectory(devPath);

            // Create developer guide
            var devGuideContent = GenerateDeveloperGuide();
            var devGuidePath = Path.Combine(devPath, "developer-guide.md");
            await File.WriteAllTextAsync(devGuidePath, devGuideContent, cancellationToken);

            // Create contribution guide
            var contributionContent = GenerateContributionGuide();
            var contributionPath = Path.Combine(devPath, "contributing.md");
            await File.WriteAllTextAsync(contributionPath, contributionContent, cancellationToken);
        }

        private async Task CreateExamplesAsync(string exportPath, DocumentationExportOptions options, CancellationToken cancellationToken)
        {
            var examplesPath = Path.Combine(exportPath, "examples");
            Directory.CreateDirectory(examplesPath);

            // Create basic example
            var basicExampleContent = GenerateBasicExample();
            var basicExamplePath = Path.Combine(examplesPath, "basic-example.md");
            await File.WriteAllTextAsync(basicExamplePath, basicExampleContent, cancellationToken);

            // Create advanced example
            var advancedExampleContent = GenerateAdvancedExample();
            var advancedExamplePath = Path.Combine(examplesPath, "advanced-example.md");
            await File.WriteAllTextAsync(advancedExamplePath, advancedExampleContent, cancellationToken);
        }

        private async Task CreateDocumentationConfigurationAsync(string exportPath, DocumentationExportOptions options, CancellationToken cancellationToken)
        {
            var configContent = GenerateDocumentationConfiguration(options);
            var configPath = Path.Combine(exportPath, "docs.config.json");
            await File.WriteAllTextAsync(configPath, configContent, cancellationToken);
        }

        private string GenerateMainReadme(DocumentationExportOptions options)
        {
            return @"# Nexo Documentation

Welcome to the Nexo platform documentation.

## Quick Start
1. Install Nexo CLI
2. Run your first command
3. Explore the features

## Features
- AI-powered code generation
- Platform-specific optimizations
- Code analysis and quality checks
- Recipe-based automation

## Getting Started
See [getting-started.md](getting-started.md) for detailed instructions.

## API Reference
See [api/](api/) for complete API documentation.

## Examples
See [examples/](examples/) for usage examples.
";
        }

        private string GenerateGettingStartedGuide(DocumentationExportOptions options)
        {
            return @"# Getting Started with Nexo

## Installation
1. Download the Nexo package
2. Run the installation script
3. Verify installation

## First Steps
1. Create a new project
2. Configure your settings
3. Run your first command

## Next Steps
- Explore the API
- Read the user guide
- Check out examples
";
        }

        private string GenerateApiOverview()
        {
            return @"# API Overview

The Nexo API provides comprehensive functionality for:
- Code generation
- Platform optimization
- Analysis and quality checks
- Automation workflows

## Core Components
- **CLI Interface**: Command-line operations
- **AI Engine**: Intelligent code generation
- **Platform Adapters**: Platform-specific optimizations
- **Analysis Tools**: Code quality and performance analysis
";
        }

        private string GenerateApiReference()
        {
            return @"# API Reference

## Commands
- `nexo generate` - Generate code
- `nexo analyze` - Analyze code quality
- `nexo optimize` - Optimize for platform
- `nexo deploy` - Deploy application

## Configuration
- `nexo.config.json` - Main configuration
- `recipes/` - Automation recipes
- `templates/` - Code templates
";
        }

        private string GenerateUserGuide()
        {
            return @"# User Guide

## Basic Usage
1. Initialize a project
2. Configure settings
3. Generate code
4. Deploy application

## Advanced Features
- Custom templates
- Recipe automation
- Platform optimization
- Quality analysis
";
        }

        private string GenerateTroubleshootingGuide()
        {
            return @"# Troubleshooting

## Common Issues
- Installation problems
- Configuration errors
- Runtime issues
- Performance problems

## Solutions
- Check system requirements
- Verify configuration
- Review logs
- Contact support
";
        }

        private string GenerateDeveloperGuide()
        {
            return @"# Developer Guide

## Architecture
- Modular design
- Plugin system
- Extensible framework
- API-first approach

## Extending Nexo
- Custom plugins
- Template development
- Recipe creation
- Platform adapters
";
        }

        private string GenerateContributionGuide()
        {
            return @"# Contributing

## Development Setup
1. Clone repository
2. Install dependencies
3. Run tests
4. Make changes

## Contribution Process
1. Fork repository
2. Create feature branch
3. Make changes
4. Submit pull request
";
        }

        private string GenerateBasicExample()
        {
            return @"# Basic Example

## Simple Code Generation
```bash
nexo generate --template web-app --output ./my-app
```

## Configuration
```json
{
  ""template"": ""web-app"",
  ""output"": ""./my-app"",
  ""features"": [""auth"", ""database""]
}
```
";
        }

        private string GenerateAdvancedExample()
        {
            return @"# Advanced Example

## Custom Recipe
```yaml
name: Full Stack App
steps:
  - generate: frontend
  - generate: backend
  - configure: database
  - deploy: production
```

## Platform Optimization
```bash
nexo optimize --platform mobile --target ios,android
```
";
        }

        private string GenerateDocumentationConfiguration(DocumentationExportOptions options)
        {
            return $@"{{
  ""version"": ""1.0.0"",
  ""format"": ""{options.Format}"",
  ""includeApiDocs"": {options.IncludeApiDocs.ToString().ToLower()},
  ""includeUserGuides"": {options.IncludeUserGuides.ToString().ToLower()},
  ""includeDeveloperGuides"": {options.IncludeDeveloperGuides.ToString().ToLower()},
  ""includeExamples"": {options.IncludeExamples.ToString().ToLower()},
  ""theme"": ""{options.Theme}"",
  ""navigation"": {{
    ""autoGenerate"": true,
    ""includeSearch"": true
  }}
}}";
        }
    }
}
