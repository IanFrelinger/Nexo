using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Platform;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Services.Platform.Generators;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// Web code generator.
    /// Uses smaller, focused generator classes for better maintainability.
    /// </summary>
    public class WebCodeGenerator : IWebCodeGenerator
    {
        private readonly ILogger<WebCodeGenerator> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ReactComponentGenerator _reactGenerator;
        private readonly WebAssemblyGenerator _webAssemblyGenerator;
        private readonly PWAGenerator _pwaGenerator;

        public WebCodeGenerator(
            ILogger<WebCodeGenerator> logger,
            IModelOrchestrator modelOrchestrator,
            ReactComponentGenerator reactGenerator,
            WebAssemblyGenerator webAssemblyGenerator,
            PWAGenerator pwaGenerator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _reactGenerator = reactGenerator ?? throw new ArgumentNullException(nameof(reactGenerator));
            _webAssemblyGenerator = webAssemblyGenerator ?? throw new ArgumentNullException(nameof(webAssemblyGenerator));
            _pwaGenerator = pwaGenerator ?? throw new ArgumentNullException(nameof(pwaGenerator));
        }

        /// <summary>
        /// Generates web application code from application logic.
        /// </summary>
        public async Task<WebGenerationResult> GenerateCodeAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting web code generation for application: {ApplicationName}", 
                applicationLogic.ApplicationName);

            var result = new WebGenerationResult
            {
                Success = false,
                Message = "Starting web code generation",
                GeneratedFiles = new List<GeneratedFile>()
            };

            try
            {
                // 1. Generate React/Vue Components
                if (options.GenerateReactComponents)
                {
                    var reactResult = await _reactGenerator.GenerateComponentsAsync(applicationLogic, options, cancellationToken);
                    if (reactResult.Success)
                    {
                        result.GeneratedFiles.AddRange(reactResult.Components.Select(c => new GeneratedFile
                        {
                            Name = c.Name,
                            Content = c.Content,
                            FilePath = c.FilePath,
                            Type = "React Component"
                        }));
                        result.Message += " React components generated.";
                    }
                }

                // 2. Generate WebAssembly Modules
                if (options.GenerateWebAssembly)
                {
                    var wasmResult = await _webAssemblyGenerator.GenerateModulesAsync(applicationLogic, options, cancellationToken);
                    if (wasmResult.Success)
                    {
                        result.GeneratedFiles.AddRange(wasmResult.Modules.Select(m => new GeneratedFile
                        {
                            Name = m.Name,
                            Content = m.Content,
                            FilePath = m.FilePath,
                            Type = "WebAssembly Module"
                        }));
                        result.Message += " WebAssembly modules generated.";
                    }
                }

                // 3. Generate PWA Features
                if (options.GeneratePWA)
                {
                    var pwaResult = await _pwaGenerator.GeneratePWAFilesAsync(applicationLogic, options, cancellationToken);
                    if (pwaResult.Success)
                    {
                        result.GeneratedFiles.AddRange(pwaResult.PWAFiles.Select(f => new GeneratedFile
                        {
                            Name = f.Name,
                            Content = f.Content,
                            FilePath = f.FilePath,
                            Type = "PWA File"
                        }));
                        result.Message += " PWA features generated.";
                    }
                }

                // 4. Generate configuration files
                var configFiles = await GenerateConfigurationFilesAsync(applicationLogic, options, cancellationToken);
                result.GeneratedFiles.AddRange(configFiles);

                result.Success = true;
                result.Message = $"Web code generation completed successfully. Generated {result.GeneratedFiles.Count} files.";

                _logger.LogInformation("Web code generation completed for application: {ApplicationName}. Generated {Count} files", 
                    applicationLogic.ApplicationName, result.GeneratedFiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during web code generation for application: {ApplicationName}", 
                    applicationLogic.ApplicationName);
                result.Success = false;
                result.Message = $"Web code generation failed: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Generates configuration files for the web application
        /// </summary>
        private async Task<List<GeneratedFile>> GenerateConfigurationFilesAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var configFiles = new List<GeneratedFile>();

            // Generate package.json
            var packageJson = new GeneratedFile
            {
                Name = "package.json",
                Content = GeneratePackageJsonContent(applicationLogic, options),
                FilePath = "package.json",
                Type = "Configuration"
            };
            configFiles.Add(packageJson);

            // Generate webpack.config.js
            var webpackConfig = new GeneratedFile
            {
                Name = "webpack.config.js",
                Content = GenerateWebpackConfigContent(applicationLogic, options),
                FilePath = "webpack.config.js",
                Type = "Configuration"
            };
            configFiles.Add(webpackConfig);

            // Generate tsconfig.json
            var tsConfig = new GeneratedFile
            {
                Name = "tsconfig.json",
                Content = GenerateTsConfigContent(applicationLogic, options),
                FilePath = "tsconfig.json",
                Type = "Configuration"
            };
            configFiles.Add(tsConfig);

            await Task.Delay(100, cancellationToken); // Simulate work
            return configFiles;
        }

        private string GeneratePackageJsonContent(ApplicationLogic applicationLogic, WebGenerationOptions options)
        {
            return $@"{{
  ""name"": ""{applicationLogic.ApplicationName.ToLower()}"",
  ""version"": ""1.0.0"",
  ""description"": ""{applicationLogic.Description}"",
  ""main"": ""src/index.tsx"",
  ""scripts"": {{
    ""start"": ""react-scripts start"",
    ""build"": ""react-scripts build"",
    ""test"": ""react-scripts test"",
    ""eject"": ""react-scripts eject""
  }},
  ""dependencies"": {{
    ""react"": ""^18.0.0"",
    ""react-dom"": ""^18.0.0"",
    ""react-router-dom"": ""^6.0.0"",
    ""typescript"": ""^4.0.0""
  }},
  ""devDependencies"": {{
    ""@types/react"": ""^18.0.0"",
    ""@types/react-dom"": ""^18.0.0"",
    ""react-scripts"": ""^5.0.0""
  }}
}}";
        }

        private string GenerateWebpackConfigContent(ApplicationLogic applicationLogic, WebGenerationOptions options)
        {
            return @"const path = require('path');

module.exports = {
  entry: './src/index.tsx',
  output: {
    path: path.resolve(__dirname, 'dist'),
    filename: 'bundle.js'
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        exclude: /node_modules/
      }
    ]
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js']
  }
};";
        }

        private string GenerateTsConfigContent(ApplicationLogic applicationLogic, WebGenerationOptions options)
        {
            return @"{
  ""compilerOptions"": {
    ""target"": ""es5"",
    ""lib"": [""dom"", ""dom.iterable"", ""es6""],
    ""allowJs"": true,
    ""skipLibCheck"": true,
    ""esModuleInterop"": true,
    ""allowSyntheticDefaultImports"": true,
    ""strict"": true,
    ""forceConsistentCasingInFileNames"": true,
    ""module"": ""esnext"",
    ""moduleResolution"": ""node"",
    ""resolveJsonModule"": true,
    ""isolatedModules"": true,
    ""noEmit"": true,
    ""jsx"": ""react-jsx""
  },
  ""include"": [""src""]
}";
        }
    }
}
