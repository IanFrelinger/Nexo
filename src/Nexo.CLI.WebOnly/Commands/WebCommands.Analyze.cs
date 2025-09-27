using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;
using System.Linq;
using System.IO;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Analyze command implementation for WebCommands.
    /// </summary>
    public static partial class WebCommands
    {
        /// <summary>
        /// Creates the analyze command
        /// </summary>
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
                        Console.WriteLine($"SUCCESS: Analysis results saved to: {output}");
                    }

                    // Display results
                    Console.WriteLine("\nStats Performance Analysis:");
                    foreach (var metric in performanceAnalysis.PerformanceMetrics)
                    {
                        Console.WriteLine($"   - {metric.Key}: {metric.Value:F2}");
                    }

                    Console.WriteLine("\nPackage Bundle Size Analysis:");
                    foreach (var size in bundleAnalysis.BundleSizes)
                    {
                        Console.WriteLine($"   - {size.Key}: {size.Value:N0} bytes");
                    }

                    Console.WriteLine("\nCompression  Compression Ratios:");
                    foreach (var ratio in bundleAnalysis.CompressionRatios)
                    {
                        Console.WriteLine($"   - {ratio.Key}: {ratio.Value:P1}");
                    }

                    if (performanceAnalysis.PerformanceRecommendations.Any())
                    {
                        Console.WriteLine("\nIdea Performance Recommendations:");
                        foreach (var recommendation in performanceAnalysis.PerformanceRecommendations)
                        {
                            Console.WriteLine($"   - {recommendation}");
                        }
                    }

                    if (bundleAnalysis.SizeOptimizationSuggestions.Any())
                    {
                        Console.WriteLine("\nIdea Size Optimization Suggestions:");
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
    }
}
