using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Platform.Services.Generators;

/// <summary>
/// Generates React code for web applications
/// </summary>
public partial class ReactCodeGenerator
{
    private readonly ILogger<ReactCodeGenerator> _logger;

    public ReactCodeGenerator(ILogger<ReactCodeGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WebCodeGenerationResult> GenerateReactCodeAsync(
        StandardizedApplicationLogic applicationLogic,
        WebGenerationOptions webOptions,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (applicationLogic == null)
            throw new ArgumentNullException(nameof(applicationLogic));
        
        if (webOptions == null)
            throw new ArgumentNullException(nameof(webOptions));

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting React code generation");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new WebCodeGenerationResult
            {
                GeneratedCode = new WebGeneratedCode()
            };

            // Generate React components
            var reactFiles = await GenerateReactFilesAsync(applicationLogic, webOptions, cancellationToken);
            result.GeneratedCode.ReactFiles = reactFiles;

            // Generate TypeScript files
            var typescriptFiles = await GenerateTypeScriptFilesAsync(applicationLogic, webOptions, cancellationToken);
            result.GeneratedCode.TypeScriptFiles = typescriptFiles;

            // Generate package.json
            result.GeneratedCode.PackageJson = GeneratePackageJson(webOptions);

            // Generate webpack config
            result.GeneratedCode.WebpackConfig = GenerateWebpackConfig(webOptions);

            // Generate ESLint config
            result.GeneratedCode.ESLintConfig = GenerateESLintConfig(webOptions);

            stopwatch.Stop();
            result.Success = true;
            result.Message = $"React code generation completed in {stopwatch.ElapsedMilliseconds}ms";
            result.GenerationTime = stopwatch.Elapsed;

            _logger.LogInformation("React code generation completed successfully");
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("React code generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during React code generation");
            return new WebCodeGenerationResult
            {
                Success = false,
                Message = $"React code generation failed: {ex.Message}",
                GenerationTime = stopwatch.Elapsed
            };
        }
    }

    private async Task<List<JavaScriptFile>> GenerateReactFilesAsync(
        StandardizedApplicationLogic applicationLogic,
        WebGenerationOptions webOptions,
        CancellationToken cancellationToken)
    {
        var files = new List<JavaScriptFile>();

        try
        {
            _logger.LogDebug("Generating React files for {ComponentCount} components", 
                applicationLogic.Components.Count);

            foreach (var component in applicationLogic.Components)
            {
                var reactFile = new JavaScriptFile
                {
                    FileName = $"{component.Name}.jsx",
                    Content = GenerateReactComponent(component, webOptions),
                    FileType = JavaScriptFileType.ReactComponent,
                    GeneratedAt = DateTime.UtcNow
                };

                files.Add(reactFile);
            }

            // Generate main App component
            var appComponent = new JavaScriptFile
            {
                FileName = "App.jsx",
                Content = GenerateAppComponent(applicationLogic, webOptions),
                FileType = JavaScriptFileType.ReactComponent,
                GeneratedAt = DateTime.UtcNow
            };
            files.Add(appComponent);

            // Generate index.js
            var indexFile = new JavaScriptFile
            {
                FileName = "index.js",
                Content = GenerateIndexFile(webOptions),
                FileType = JavaScriptFileType.EntryPoint,
                GeneratedAt = DateTime.UtcNow
            };
            files.Add(indexFile);

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating React files");
            return files;
        }
    }

    private async Task<List<TypeScriptFile>> GenerateTypeScriptFilesAsync(
        StandardizedApplicationLogic applicationLogic,
        WebGenerationOptions webOptions,
        CancellationToken cancellationToken)
    {
        var files = new List<TypeScriptFile>();

        try
        {
            _logger.LogDebug("Generating TypeScript files for React application");

            // Generate type definitions
            foreach (var model in applicationLogic.Models)
            {
                var typeFile = new TypeScriptFile
                {
                    FileName = $"{model.Name}.types.ts",
                    Content = GenerateTypeDefinitions(model),
                    FileType = TypeScriptFileType.TypeDefinition,
                    GeneratedAt = DateTime.UtcNow
                };
                files.Add(typeFile);
            }

            // Generate interfaces
            var interfaceFile = new TypeScriptFile
            {
                FileName = "interfaces.ts",
                Content = GenerateInterfaces(applicationLogic),
                FileType = TypeScriptFileType.Interface,
                GeneratedAt = DateTime.UtcNow
            };
            files.Add(interfaceFile);

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TypeScript files");
            return files;
        }
    }

    private string GenerateReactComponent(ApplicationComponent component, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("import React from 'react';");
        sb.AppendLine("import PropTypes from 'prop-types';");
        sb.AppendLine();
        
        sb.AppendLine($"const {component.Name} = ({{ {string.Join(", ", component.Properties.Select(p => p.Name))} }}) => {{");
        sb.AppendLine("  return (");
        sb.AppendLine("    <div className=\"component-container\">");
        sb.AppendLine($"      <h2>{component.Name}</h2>");
        sb.AppendLine("      {/* Component content */}");
        sb.AppendLine("    </div>");
        sb.AppendLine("  );");
        sb.AppendLine("};");
        sb.AppendLine();
        
        sb.AppendLine($"{component.Name}.propTypes = {{");
        foreach (var prop in component.Properties)
        {
            sb.AppendLine($"  {prop.Name}: PropTypes.{GetPropType(prop.Type)},");
        }
        sb.AppendLine("};");
        sb.AppendLine();
        
        sb.AppendLine($"export default {component.Name};");
        
        return sb.ToString();
    }

    private string GenerateAppComponent(StandardizedApplicationLogic applicationLogic, WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("import React from 'react';");
        sb.AppendLine("import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';");
        sb.AppendLine();
        
        foreach (var component in applicationLogic.Components)
        {
            sb.AppendLine($"import {component.Name} from './components/{component.Name}';");
        }
        sb.AppendLine();
        
        sb.AppendLine("const App = () => {");
        sb.AppendLine("  return (");
        sb.AppendLine("    <Router>");
        sb.AppendLine("      <div className=\"App\">");
        sb.AppendLine("        <Routes>");
        
        foreach (var component in applicationLogic.Components)
        {
            sb.AppendLine($"          <Route path=\"/{component.Name.ToLower()}\" element={{<{component.Name} />}} />");
        }
        
        sb.AppendLine("        </Routes>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </Router>");
        sb.AppendLine("  );");
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine("export default App;");
        
        return sb.ToString();
    }

    private string GenerateIndexFile(WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("import React from 'react';");
        sb.AppendLine("import ReactDOM from 'react-dom/client';");
        sb.AppendLine("import App from './App';");
        sb.AppendLine("import './index.css';");
        sb.AppendLine();
        sb.AppendLine("const root = ReactDOM.createRoot(document.getElementById('root'));");
        sb.AppendLine("root.render(<App />);");
        
        return sb.ToString();
    }

    private string GenerateTypeDefinitions(ApplicationModel model)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"export interface {model.Name} {{");
        foreach (var property in model.Properties)
        {
            sb.AppendLine($"  {property.Name}: {GetTypeScriptType(property.Type)};");
        }
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine($"export type {model.Name}Create = Omit<{model.Name}, 'id'>;");
        sb.AppendLine($"export type {model.Name}Update = Partial<{model.Name}>;");
        
        return sb.ToString();
    }

    private string GenerateInterfaces(StandardizedApplicationLogic applicationLogic)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("export interface ApiResponse<T> {");
        sb.AppendLine("  data: T;");
        sb.AppendLine("  success: boolean;");
        sb.AppendLine("  message?: string;");
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine("export interface ApiError {");
        sb.AppendLine("  code: string;");
        sb.AppendLine("  message: string;");
        sb.AppendLine("  details?: any;");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private string GeneratePackageJson(WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("{");
        sb.AppendLine("  \"name\": \"react-app\",");
        sb.AppendLine("  \"version\": \"1.0.0\",");
        sb.AppendLine("  \"dependencies\": {");
        sb.AppendLine("    \"react\": \"^18.2.0\",");
        sb.AppendLine("    \"react-dom\": \"^18.2.0\",");
        sb.AppendLine("    \"react-router-dom\": \"^6.8.0\",");
        sb.AppendLine("    \"prop-types\": \"^15.8.1\"");
        sb.AppendLine("  },");
        sb.AppendLine("  \"devDependencies\": {");
        sb.AppendLine("    \"@types/react\": \"^18.0.0\",");
        sb.AppendLine("    \"@types/react-dom\": \"^18.0.0\",");
        sb.AppendLine("    \"typescript\": \"^4.9.0\"");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private string GenerateWebpackConfig(WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("const path = require('path');");
        sb.AppendLine();
        sb.AppendLine("module.exports = {");
        sb.AppendLine("  entry: './src/index.js',");
        sb.AppendLine("  output: {");
        sb.AppendLine("    path: path.resolve(__dirname, 'dist'),");
        sb.AppendLine("    filename: 'bundle.js'");
        sb.AppendLine("  },");
        sb.AppendLine("  module: {");
        sb.AppendLine("    rules: [");
        sb.AppendLine("      {");
        sb.AppendLine("        test: /\\.(js|jsx)$/,");
        sb.AppendLine("        exclude: /node_modules/,");
        sb.AppendLine("        use: {");
        sb.AppendLine("          loader: 'babel-loader'");
        sb.AppendLine("        }");
        sb.AppendLine("      }");
        sb.AppendLine("    ]");
        sb.AppendLine("  }");
        sb.AppendLine("};");
        
        return sb.ToString();
    }

    private string GenerateESLintConfig(WebGenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("{");
        sb.AppendLine("  \"extends\": [\"eslint:recommended\", \"plugin:react/recommended\"],");
        sb.AppendLine("  \"parserOptions\": {");
        sb.AppendLine("    \"ecmaVersion\": 2021,");
        sb.AppendLine("    \"sourceType\": \"module\",");
        sb.AppendLine("    \"ecmaFeatures\": {");
        sb.AppendLine("      \"jsx\": true");
        sb.AppendLine("    }");
        sb.AppendLine("  },");
        sb.AppendLine("  \"rules\": {");
        sb.AppendLine("    \"react/prop-types\": \"warn\"");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private string GetPropType(string type)
    {
        return type.ToLower() switch
        {
            "string" => "string",
            "number" => "number",
            "boolean" => "bool",
            "date" => "instanceOf(Date)",
            _ => "any"
        };
    }

    private string GetTypeScriptType(string type)
    {
        return type.ToLower() switch
        {
            "string" => "string",
            "number" => "number",
            "boolean" => "boolean",
            "date" => "Date",
            _ => "any"
        };
    }
}
