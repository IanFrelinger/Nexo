using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.CLI.Commands.Unity
{
    /// <summary>
    /// Analyze command functionality for UnityCommands.
    /// </summary>
    public static partial class UnityCommands
    {
        /// <summary>
        /// Creates the analyze command
        /// </summary>
        private static Command CreateAnalyzeCommand(IServiceProvider serviceProvider)
        {
            var analyzeCommand = new Command("analyze", "Analyze Unity project for optimization opportunities");
            
            var projectPathOption = new Option<string>(
                "--project-path",
                () => ".",
                "Path to the Unity project directory");
            
            var detailedOption = new Option<bool>(
                "--detailed",
                "Show detailed analysis results");
            
            var performanceOption = new Option<bool>(
                "--performance",
                "Focus on performance analysis");
            
            analyzeCommand.AddOption(projectPathOption);
            analyzeCommand.AddOption(detailedOption);
            analyzeCommand.AddOption(performanceOption);
            
            analyzeCommand.SetHandler(async (projectPath, detailed, performance) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<UnityCommands>>();
                var analyzer = serviceProvider.GetRequiredService<IUnityProjectAnalyzer>();
                
                await AnalyzeUnityProject(analyzer, logger, projectPath, detailed, performance);
            }, projectPathOption, detailedOption, performanceOption);
            
            return analyzeCommand;
        }

        /// <summary>
        /// Analyzes Unity project for optimization opportunities
        /// </summary>
        private static async Task AnalyzeUnityProject(
            IUnityProjectAnalyzer analyzer,
            ILogger logger,
            string projectPath,
            bool detailed,
            bool performance)
        {
            try
            {
                logger.LogInformation("Starting Unity project analysis for: {ProjectPath}", projectPath);
                
                Console.WriteLine($"Search Analyzing Unity project at {projectPath}...");
                
                var analysis = await analyzer.AnalyzeProjectAsync(projectPath);
                
                Console.WriteLine($"SUCCESS: Analysis complete!");
                Console.WriteLine($"Stats Project Analysis Results:");
                Console.WriteLine($"  Scripts: {analysis.ScriptAnalysis.Scripts.Count()}");
                Console.WriteLine($"  Scenes: {analysis.SceneAnalysis.Scenes.Count()}");
                Console.WriteLine($"  Assets: {analysis.AssetAnalysis.Assets.Count()}");
                Console.WriteLine($"  Optimization Opportunities: {analysis.IterationOptimizations.Count()}");
                
                if (performance)
                {
                    Console.WriteLine($"\nRunning Performance Optimization Opportunities:");
                    foreach (var opt in analysis.IterationOptimizations.Take(5))
                    {
                        Console.WriteLine($"  • {opt.ScriptPath}:{opt.LineNumber}");
                        Console.WriteLine($"    Current: {opt.CurrentPattern}");
                        Console.WriteLine($"    Optimized: {opt.OptimizedPattern}");
                        Console.WriteLine($"    Est. Gain: {opt.EstimatedPerformanceGain:P}");
                    }
                }
                
                if (detailed)
                {
                    await ShowDetailedAnalysis(analysis);
                }
                
                logger.LogInformation("Unity project analysis completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze Unity project");
                Console.WriteLine($"ERROR: Analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows detailed analysis results
        /// </summary>
        private static async Task ShowDetailedAnalysis(UnityProjectAnalysis analysis)
        {
            Console.WriteLine($"\nList Detailed Analysis Results:");
            
            // Script analysis
            Console.WriteLine($"\nDocument Script Analysis:");
            foreach (var script in analysis.ScriptAnalysis.Scripts.Take(10))
            {
                Console.WriteLine($"  • {script.Name} ({script.LinesOfCode} lines)");
                if (script.PerformanceIssues.Any())
                {
                    Console.WriteLine($"    WARNING: {script.PerformanceIssues.Count()} performance issues");
                }
            }
            
            // Asset analysis
            Console.WriteLine($"\nDesign Asset Analysis:");
            Console.WriteLine($"  Total Assets: {analysis.AssetAnalysis.Assets.Count()}");
            Console.WriteLine($"  Total Size: {analysis.AssetAnalysis.TotalAssetSize / 1024 / 1024:F1} MB");
            Console.WriteLine($"  Optimizable Size: {analysis.AssetAnalysis.OptimizableAssetSize / 1024 / 1024:F1} MB");
            
            // Scene analysis
            Console.WriteLine($"\nMovie Scene Analysis:");
            foreach (var scene in analysis.SceneAnalysis.Scenes.Take(5))
            {
                Console.WriteLine($"  • {scene.Name}: {scene.GameObjects} objects, {scene.DrawCalls} draw calls");
            }
            
            // Performance recommendations
            Console.WriteLine($"\nIdea Performance Recommendations:");
            foreach (var rec in analysis.PerformanceRecommendations.Take(5))
            {
                Console.WriteLine($"  • {rec.Category}: {rec.Description}");
                Console.WriteLine($"    Est. Improvement: {rec.EstimatedImprovement:P}");
            }
        }
    }
}
