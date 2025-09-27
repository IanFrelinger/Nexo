using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;
using Nexo.Feature.Web.UseCases;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Web code generation command functionality
    /// </summary>
    public static partial class WebCommands
    {
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
                        // Ensure output directory exists
                        Directory.CreateDirectory(output);

                        // Write generated files
                        var filesWritten = new List<string>();

                        // Write component code
                        if (!string.IsNullOrEmpty(result.ComponentCode))
                        {
                            var componentFile = Path.Combine(output, $"{name}.{GetFileExtension(frameworkType)}");
                            File.WriteAllText(componentFile, result.ComponentCode);
                            filesWritten.Add(componentFile);
                        }

                        // Write TypeScript types
                        if (!string.IsNullOrEmpty(result.TypeScriptTypes))
                        {
                            var typesFile = Path.Combine(output, $"{name}.d.ts");
                            File.WriteAllText(typesFile, result.TypeScriptTypes);
                            filesWritten.Add(typesFile);
                        }

                        // Write styling code
                        if (!string.IsNullOrEmpty(result.StylingCode))
                        {
                            var stylingFile = Path.Combine(output, $"{name}.{GetStylingExtension(frameworkType)}");
                            File.WriteAllText(stylingFile, result.StylingCode);
                            filesWritten.Add(stylingFile);
                        }

                        // Write documentation
                        if (!string.IsNullOrEmpty(result.Documentation))
                        {
                            var docFile = Path.Combine(output, $"{name}.md");
                            File.WriteAllText(docFile, result.Documentation);
                            filesWritten.Add(docFile);
                        }

                        Console.WriteLine($"SUCCESS: Successfully generated web code for '{name}'");
                        Console.WriteLine($"Directory Output directory: {output}");
                        Console.WriteLine($"Tool Framework: {frameworkType}");
                        Console.WriteLine($"Package Component type: {componentType}");
                        Console.WriteLine($"⚡ Optimization: {optimizationType}");

                        if (filesWritten.Any())
                        {
                            Console.WriteLine("\nFile Generated files:");
                            foreach (var file in filesWritten)
                            {
                                Console.WriteLine($"   - {file}");
                            }
                        }

                        if (result.PerformanceMetrics.Any())
                        {
                            Console.WriteLine("\nStats Performance metrics:");
                            foreach (var metric in result.PerformanceMetrics)
                            {
                                Console.WriteLine($"   - {metric.Key}: {metric.Value:F2}");
                            }
                        }

                        if (result.BundleSizes.Any())
                        {
                            Console.WriteLine("\nPackage Bundle sizes:");
                            foreach (var size in result.BundleSizes)
                            {
                                Console.WriteLine($"   - {size.Key}: {size.Value:N0} bytes");
                            }
                        }

                        if (result.Warnings.Any())
                        {
                            Console.WriteLine("\nWARNING:  Warnings:");
                            foreach (var warning in result.Warnings)
                            {
                                Console.WriteLine($"   - {warning}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ERROR: Failed to generate web code: {result.Message}");
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
    }
}
