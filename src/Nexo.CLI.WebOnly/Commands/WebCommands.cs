using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;
using Nexo.Feature.Web.UseCases;
using System.Linq;
using System.IO;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Web-related commands for the CLI.
    /// </summary>
    public static class WebCommands
    {
        /// <summary>
        /// Creates the web command with all its subcommands.
        /// </summary>
        /// <param name="webCodeGenerator">Web code generator service.</param>
        /// <param name="wasmOptimizer">WebAssembly optimizer service.</param>
        /// <param name="generateWebCodeUseCase">Web code generation use case.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>Configured web command.</returns>
        public static Command CreateWebCommand(
            IWebCodeGenerator webCodeGenerator,
            IWebAssemblyOptimizer wasmOptimizer,
            GenerateWebCodeUseCase generateWebCodeUseCase,
            ILogger logger)
        {
            var webCommand = new Command("web", "Web code generation and optimization tools");

            // Generate command
            var generateCommand = CreateGenerateCommand(webCodeGenerator, generateWebCodeUseCase, logger);
            webCommand.AddCommand(generateCommand);

            // Optimize command
            var optimizeCommand = CreateOptimizeCommand(wasmOptimizer, logger);
            webCommand.AddCommand(optimizeCommand);

            // Analyze command
            var analyzeCommand = CreateAnalyzeCommand(wasmOptimizer, logger);
            webCommand.AddCommand(analyzeCommand);

            // List command
            var listCommand = CreateListCommand(webCodeGenerator, wasmOptimizer, logger);
            webCommand.AddCommand(listCommand);

            // Validate command
            var validateCommand = CreateValidateCommand(webCodeGenerator, wasmOptimizer, logger);
            webCommand.AddCommand(validateCommand);

            return webCommand;
        }

        private static Command CreateGenerateCommand(
            IWebCodeGenerator webCodeGenerator,
            GenerateWebCodeUseCase generateWebCodeUseCase,
            ILogger logger)
        {
            var generateCommand = new Command("generate", "Generate web code for various frameworks");

            // Required options
            var componentNameOption = new Option<string>("--name", "Component name") { IsRequired = true };
            var frameworkOption = new Option<string>("--framework", "Target framework (react, vue, nextjs, nuxtjs, angular, svelte)") { IsRequired = true };
            var componentTypeOption = new Option<string>("--type", "Component type (functional, class, pure, hook, page, layout)") { IsRequired = true };
            var targetPathOption = new Option<string>("--output", "Output directory path") { IsRequired = true };

            // Optional options
            var sourceCodeOption = new Option<string>("--source", "Source code or specification") { IsRequired = false };
            var optimizationOption = new Option<string>("--optimization", "WebAssembly optimization (none, basic, aggressive, size, balanced, custom)") { IsRequired = false };

            generateCommand.AddOption(componentNameOption);
            generateCommand.AddOption(frameworkOption);
            generateCommand.AddOption(componentTypeOption);
            generateCommand.AddOption(targetPathOption);
            generateCommand.AddOption(sourceCodeOption);
            generateCommand.AddOption(optimizationOption);

            generateCommand.SetHandler(async (name, framework, type, output, source, optimization) =>
            {
                try
                {
                    logger.LogInformation("Generating web code for component: {Name} with framework: {Framework}", name, framework);

                    // Parse framework
                    if (!Enum.TryParse<WebFrameworkType>(framework, true, out var frameworkType))
                    {
                        Console.WriteLine($"Error: Invalid framework '{framework}'. Valid options: react, vue, nextjs, nuxtjs, angular, svelte");
                        return;
                    }

                    // Parse component type
                    if (!Enum.TryParse<WebComponentType>(type, true, out var componentType))
                    {
                        Console.WriteLine($"Error: Invalid component type '{type}'. Valid options: functional, class, pure, hook, page, layout");
                        return;
                    }

                    // Parse optimization
                    var optimizationType = WebAssemblyOptimization.Balanced;
                    if (!string.IsNullOrEmpty(optimization))
                    {
                        if (!Enum.TryParse<WebAssemblyOptimization>(optimization, true, out optimizationType))
                        {
                            Console.WriteLine($"Error: Invalid optimization '{optimization}'. Valid options: none, basic, aggressive, size, balanced, custom");
                            return;
                        }
                    }

                    // Create request
                    var request = new WebCodeGenerationRequest
                    {
                        ComponentName = name,
                        Framework = frameworkType,
                        ComponentType = componentType,
                        TargetPath = output,
                        SourceCode = source ?? string.Empty,
                        Optimization = optimizationType,
                        IncludeTypeScript = true,
                        IncludeStyling = true,
                        IncludeTests = false,
                        IncludeDocumentation = true
                    };



                    // Generate code
                    var result = await generateWebCodeUseCase.ExecuteAsync(request);

                    if (result.Success)
                    {
                        Console.WriteLine($"✅ Successfully generated web code for '{name}'");
                        Console.WriteLine($"📁 Output directory: {output}");
                        Console.WriteLine($"🔧 Framework: {frameworkType}");
                        Console.WriteLine($"📦 Component type: {componentType}");
                        Console.WriteLine($"⚡ Optimization: {optimizationType}");

                        if (result.GeneratedFiles.Any())
                        {
                            Console.WriteLine("\n📄 Generated files:");
                            foreach (var file in result.GeneratedFiles)
                            {
                                Console.WriteLine($"   - {file}");
                            }
                        }

                        if (result.PerformanceMetrics.Any())
                        {
                            Console.WriteLine("\n📊 Performance metrics:");
                            foreach (var metric in result.PerformanceMetrics)
                            {
                                Console.WriteLine($"   - {metric.Key}: {metric.Value:F2}");
                            }
                        }

                        if (result.BundleSizes.Any())
                        {
                            Console.WriteLine("\n📦 Bundle sizes:");
                            foreach (var size in result.BundleSizes)
                            {
                                Console.WriteLine($"   - {size.Key}: {size.Value:N0} bytes");
                            }
                        }

                        if (result.Warnings.Any())
                        {
                            Console.WriteLine("\n⚠️  Warnings:");
                            foreach (var warning in result.Warnings)
                            {
                                Console.WriteLine($"   - {warning}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to generate web code: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to generate web code");
                    Console.WriteLine($"Error: Failed to generate web code: {ex.Message}");
                }
            }, componentNameOption, frameworkOption, componentTypeOption, targetPathOption, sourceCodeOption, 
               optimizationOption);

            return generateCommand;
        }

        private static Command CreateOptimizeCommand(IWebAssemblyOptimizer wasmOptimizer, ILogger logger)
        {
            var optimizeCommand = new Command("optimize", "Optimize WebAssembly code for performance");

            var sourceFileOption = new Option<string>("--source", "Source file path") { IsRequired = true };
            var outputFileOption = new Option<string>("--output", "Output file path") { IsRequired = false };
            var optimizationOption = new Option<string>("--strategy", "Optimization strategy (none, basic, aggressive, size, balanced, custom)") { IsRequired = false };
            var configFileOption = new Option<string>("--config", "Configuration file path") { IsRequired = false };

            optimizeCommand.AddOption(sourceFileOption);
            optimizeCommand.AddOption(outputFileOption);
            optimizeCommand.AddOption(optimizationOption);
            optimizeCommand.AddOption(configFileOption);

            optimizeCommand.SetHandler(async (source, output, strategy, config) =>
            {
                try
                {
                    logger.LogInformation("Optimizing WebAssembly code from: {Source}", source);

                    if (!File.Exists(source))
                    {
                        Console.WriteLine($"Error: Source file not found: {source}");
                        return;
                    }

                    var sourceCode = await File.ReadAllTextAsync(source);

                    // Parse optimization strategy
                    var optimizationType = WebAssemblyOptimization.Balanced;
                    if (!string.IsNullOrEmpty(strategy))
                    {
                        if (!Enum.TryParse<WebAssemblyOptimization>(strategy, true, out optimizationType))
                        {
                            Console.WriteLine($"Error: Invalid optimization strategy '{strategy}'. Valid options: none, basic, aggressive, size, balanced, custom");
                            return;
                        }
                    }

                    // Create configuration
                    var wasmConfig = new WebAssemblyConfig
                    {
                        Optimization = optimizationType
                    };

                    // Load configuration file if provided
                    if (!string.IsNullOrEmpty(config) && File.Exists(config))
                    {
                        try
                        {
                            var configContent = await File.ReadAllTextAsync(config);
                            var configData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configContent);
                            if (configData != null)
                            {
                                foreach (var kvp in configData)
                                {
                                    wasmConfig.CustomFlags[kvp.Key] = kvp.Value;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to load configuration file: {Config}", config);
                            Console.WriteLine($"Warning: Failed to load configuration file: {ex.Message}");
                        }
                    }

                    // Optimize code
                    var result = await wasmOptimizer.OptimizeAsync(sourceCode, wasmConfig);

                    if (result.Success)
                    {
                        // Write optimized code
                        var outputPath = output ?? source.Replace(".js", ".optimized.js");
                        await File.WriteAllTextAsync(outputPath, result.OptimizedCode);

                        Console.WriteLine($"✅ Successfully optimized WebAssembly code");
                        Console.WriteLine($"📁 Output file: {outputPath}");
                        Console.WriteLine($"⚡ Optimization time: {result.OptimizationTime.TotalMilliseconds:F2}ms");

                        if (result.Metrics.Any())
                        {
                            Console.WriteLine("\n📊 Optimization metrics:");
                            foreach (var metric in result.Metrics)
                            {
                                Console.WriteLine($"   - {metric.Key}: {metric.Value}");
                            }
                        }

                        if (result.Warnings.Any())
                        {
                            Console.WriteLine("\n⚠️  Warnings:");
                            foreach (var warning in result.Warnings)
                            {
                                Console.WriteLine($"   - {warning}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to optimize code: {string.Join(", ", result.Warnings)}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to optimize WebAssembly code");
                    Console.WriteLine($"Error: Failed to optimize code: {ex.Message}");
                }
            }, sourceFileOption, outputFileOption, optimizationOption, configFileOption);

            return optimizeCommand;
        }

        private static Command CreateAnalyzeCommand(IWebAssemblyOptimizer wasmOptimizer, ILogger logger)
        {
            var analyzeCommand = new Command("analyze", "Analyze web code performance and bundle size");

            var sourceFileOption = new Option<string>("--source", "Source file path") { IsRequired = true };
            var outputFileOption = new Option<string>("--output", "Output JSON file path") { IsRequired = false };

            analyzeCommand.AddOption(sourceFileOption);
            analyzeCommand.AddOption(outputFileOption);

            analyzeCommand.SetHandler(async (source, output) =>
            {
                try
                {
                    logger.LogInformation("Analyzing web code from: {Source}", source);

                    if (!File.Exists(source))
                    {
                        Console.WriteLine($"Error: Source file not found: {source}");
                        return;
                    }

                    var sourceCode = await File.ReadAllTextAsync(source);

                    // Analyze performance
                    var performanceAnalysis = await wasmOptimizer.AnalyzePerformanceAsync(sourceCode);

                    // Analyze bundle size
                    var bundleAnalysis = await wasmOptimizer.EstimateBundleSizeAsync(sourceCode, new WebAssemblyConfig());

                    // Create analysis result
                    var analysisResult = new
                    {
                        Performance = performanceAnalysis.PerformanceMetrics,
                        BundleSizes = bundleAnalysis.BundleSizes,
                        CompressionRatios = bundleAnalysis.CompressionRatios,
                        PerformanceRecommendations = performanceAnalysis.PerformanceRecommendations,
                        SizeOptimizationSuggestions = bundleAnalysis.SizeOptimizationSuggestions
                    };

                    // Output results
                    if (!string.IsNullOrEmpty(output))
                    {
                        var jsonResult = System.Text.Json.JsonSerializer.Serialize(analysisResult, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(output, jsonResult);
                        Console.WriteLine($"✅ Analysis results saved to: {output}");
                    }

                    // Display results
                    Console.WriteLine("\n📊 Performance Analysis:");
                    foreach (var metric in performanceAnalysis.PerformanceMetrics)
                    {
                        Console.WriteLine($"   - {metric.Key}: {metric.Value:F2}");
                    }

                    Console.WriteLine("\n📦 Bundle Size Analysis:");
                    foreach (var size in bundleAnalysis.BundleSizes)
                    {
                        Console.WriteLine($"   - {size.Key}: {size.Value:N0} bytes");
                    }

                    Console.WriteLine("\n🗜️  Compression Ratios:");
                    foreach (var ratio in bundleAnalysis.CompressionRatios)
                    {
                        Console.WriteLine($"   - {ratio.Key}: {ratio.Value:P1}");
                    }

                    if (performanceAnalysis.PerformanceRecommendations.Any())
                    {
                        Console.WriteLine("\n💡 Performance Recommendations:");
                        foreach (var recommendation in performanceAnalysis.PerformanceRecommendations)
                        {
                            Console.WriteLine($"   - {recommendation}");
                        }
                    }

                    if (bundleAnalysis.SizeOptimizationSuggestions.Any())
                    {
                        Console.WriteLine("\n💡 Size Optimization Suggestions:");
                        foreach (var suggestion in bundleAnalysis.SizeOptimizationSuggestions)
                        {
                            Console.WriteLine($"   - {suggestion}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to analyze web code");
                    Console.WriteLine($"Error: Failed to analyze code: {ex.Message}");
                }
            }, sourceFileOption, outputFileOption);

            return analyzeCommand;
        }

        private static Command CreateListCommand(IWebCodeGenerator webCodeGenerator, IWebAssemblyOptimizer wasmOptimizer, ILogger logger)
        {
            var listCommand = new Command("list", "List available frameworks, component types, and optimizations");

            var frameworksOption = new Option<bool>("--frameworks", "List supported frameworks") { IsRequired = false };
            var componentTypesOption = new Option<bool>("--types", "List supported component types") { IsRequired = false };
            var optimizationsOption = new Option<bool>("--optimizations", "List available optimizations") { IsRequired = false };

            listCommand.AddOption(frameworksOption);
            listCommand.AddOption(componentTypesOption);
            listCommand.AddOption(optimizationsOption);

            listCommand.SetHandler((frameworks, types, optimizations) =>
            {
                try
                {
                    logger.LogInformation("Listing available options");

                    if (frameworks)
                    {
                        Console.WriteLine("🚀 Supported Frameworks:");
                        var supportedFrameworks = webCodeGenerator.GetSupportedFrameworks();
                        foreach (var framework in supportedFrameworks)
                        {
                            Console.WriteLine($"   - {framework}");
                        }
                    }

                    if (types)
                    {
                        Console.WriteLine("\n🎯 Supported Component Types:");
                        var componentTypes = Enum.GetNames<WebComponentType>();
                        foreach (var type in componentTypes)
                        {
                            Console.WriteLine($"   - {type}");
                        }
                    }

                    if (optimizations)
                    {
                        Console.WriteLine("\n⚡ Available Optimizations:");
                        var optimizationsList = wasmOptimizer.GetAvailableOptimizations();
                        foreach (var optimization in optimizationsList)
                        {
                            Console.WriteLine($"   - {optimization}");
                        }
                    }

                    if (!frameworks && !types && !optimizations)
                    {
                        // Show all if no specific option is selected
                        Console.WriteLine("🚀 Supported Frameworks:");
                        var supportedFrameworks = webCodeGenerator.GetSupportedFrameworks();
                        foreach (var framework in supportedFrameworks)
                        {
                            Console.WriteLine($"   - {framework}");
                        }

                        Console.WriteLine("\n🎯 Supported Component Types:");
                        var componentTypes = Enum.GetNames<WebComponentType>();
                        foreach (var type in componentTypes)
                        {
                            Console.WriteLine($"   - {type}");
                        }

                        Console.WriteLine("\n⚡ Available Optimizations:");
                        var optimizationsList = wasmOptimizer.GetAvailableOptimizations();
                        foreach (var optimization in optimizationsList)
                        {
                            Console.WriteLine($"   - {optimization}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to list options");
                    Console.WriteLine($"Error: Failed to list options: {ex.Message}");
                }
            }, frameworksOption, componentTypesOption, optimizationsOption);

            return listCommand;
        }

        private static Command CreateValidateCommand(IWebCodeGenerator webCodeGenerator, IWebAssemblyOptimizer wasmOptimizer, ILogger logger)
        {
            var validateCommand = new Command("validate", "Validate web code generation configuration");

            var configFileOption = new Option<string>("--config", "Configuration file path") { IsRequired = true };

            validateCommand.AddOption(configFileOption);

            validateCommand.SetHandler(async (config) =>
            {
                try
                {
                    logger.LogInformation("Validating web configuration from: {Config}", config);

                    if (!File.Exists(config))
                    {
                        Console.WriteLine($"Error: Configuration file not found: {config}");
                        return;
                    }

                    var configContent = await File.ReadAllTextAsync(config);
                    var configData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configContent);

                    if (configData == null)
                    {
                        Console.WriteLine("Error: Invalid configuration file format");
                        return;
                    }

                    Console.WriteLine("🔍 Validating configuration...");

                    var isValid = true;
                    var errors = new List<string>();

                    // Validate framework
                    if (configData.TryGetValue("framework", out var frameworkValue))
                    {
                        var framework = frameworkValue?.ToString();
                        if (!string.IsNullOrEmpty(framework))
                        {
                            if (!Enum.TryParse<WebFrameworkType>(framework, true, out _))
                            {
                                errors.Add($"Invalid framework: {framework}");
                                isValid = false;
                            }
                            else
                            {
                                Console.WriteLine($"✅ Framework: {framework}");
                            }
                        }
                    }

                    // Validate component type
                    if (configData.TryGetValue("componentType", out var componentTypeValue))
                    {
                        var componentType = componentTypeValue?.ToString();
                        if (!string.IsNullOrEmpty(componentType))
                        {
                            if (!Enum.TryParse<WebComponentType>(componentType, true, out _))
                            {
                                errors.Add($"Invalid component type: {componentType}");
                                isValid = false;
                            }
                            else
                            {
                                Console.WriteLine($"✅ Component type: {componentType}");
                            }
                        }
                    }

                    // Validate optimization
                    if (configData.TryGetValue("optimization", out var optimizationValue))
                    {
                        var optimization = optimizationValue?.ToString();
                        if (!string.IsNullOrEmpty(optimization))
                        {
                            if (!Enum.TryParse<WebAssemblyOptimization>(optimization, true, out _))
                            {
                                errors.Add($"Invalid optimization: {optimization}");
                                isValid = false;
                            }
                            else
                            {
                                Console.WriteLine($"✅ Optimization: {optimization}");
                            }
                        }
                    }

                    // Validate required fields
                    if (!configData.ContainsKey("componentName"))
                    {
                        errors.Add("Missing required field: componentName");
                        isValid = false;
                    }
                    else
                    {
                        Console.WriteLine($"✅ Component name: {configData["componentName"]}");
                    }

                    if (!configData.ContainsKey("targetPath"))
                    {
                        errors.Add("Missing required field: targetPath");
                        isValid = false;
                    }
                    else
                    {
                        Console.WriteLine($"✅ Target path: {configData["targetPath"]}");
                    }

                    if (isValid)
                    {
                        Console.WriteLine("\n✅ Configuration is valid!");
                    }
                    else
                    {
                        Console.WriteLine("\n❌ Configuration validation failed:");
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"   - {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to validate configuration");
                    Console.WriteLine($"Error: Failed to validate configuration: {ex.Message}");
                }
            }, configFileOption);

            return validateCommand;
        }
    }
} 