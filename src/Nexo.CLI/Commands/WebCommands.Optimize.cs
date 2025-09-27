using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Models;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// WebAssembly optimization command functionality
    /// </summary>
    public static partial class WebCommands
    {
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

                    var sourceCode = File.ReadAllText(source);

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
                            var configContent = File.ReadAllText(config);
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
                        File.WriteAllText(outputPath, result.OptimizedCode);

                        Console.WriteLine($"SUCCESS: Successfully optimized WebAssembly code");
                        Console.WriteLine($"Directory Output file: {outputPath}");
                        Console.WriteLine($"⚡ Optimization time: {result.OptimizationTime.TotalMilliseconds:F2}ms");

                        if (result.Metrics.Any())
                        {
                            Console.WriteLine("\nStats Optimization metrics:");
                            foreach (var metric in result.Metrics)
                            {
                                Console.WriteLine($"   - {metric.Key}: {metric.Value}");
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
                        Console.WriteLine($"ERROR: Failed to optimize code: {string.Join(", ", result.Warnings)}");
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
    }
}
