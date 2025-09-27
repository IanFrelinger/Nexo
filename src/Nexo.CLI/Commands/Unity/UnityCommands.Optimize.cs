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
    /// Optimize command functionality for UnityCommands.
    /// </summary>
    public static partial class UnityCommands
    {
        /// <summary>
        /// Creates the optimize command
        /// </summary>
        private static Command CreateOptimizeCommand(IServiceProvider serviceProvider)
        {
            var optimizeCommand = new Command("optimize", "Optimize Unity project for performance and build size");
            
            var projectPathOption = new Option<string>(
                "--project-path",
                () => ".",
                "Path to the Unity project directory");
            
            var applyOption = new Option<bool>(
                "--apply",
                "Apply optimizations to the project");
            
            var targetOption = new Option<string>(
                "--target",
                () => "all",
                "Optimization target (performance, memory, build-size, all)");
            
            optimizeCommand.AddOption(projectPathOption);
            optimizeCommand.AddOption(applyOption);
            optimizeCommand.AddOption(targetOption);
            
            optimizeCommand.SetHandler(async (projectPath, apply, target) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<UnityCommands>>();
                var optimizer = serviceProvider.GetRequiredService<IUnityProjectOptimizer>();
                
                await OptimizeUnityProject(optimizer, logger, projectPath, apply, target);
            }, projectPathOption, applyOption, targetOption);
            
            return optimizeCommand;
        }

        /// <summary>
        /// Optimizes Unity project for performance and build size
        /// </summary>
        private static async Task OptimizeUnityProject(
            IUnityProjectOptimizer optimizer,
            ILogger logger,
            string projectPath,
            bool apply,
            string target)
        {
            try
            {
                logger.LogInformation("Starting Unity project optimization for: {ProjectPath}", projectPath);
                
                Console.WriteLine($"Running Optimizing Unity project for {target}...");
                
                var optimizationRequest = new UnityOptimizationRequest
                {
                    ProjectPath = projectPath,
                    OptimizationTarget = ParseOptimizationTarget(target),
                    ApplyOptimizations = apply
                };
                
                var result = await optimizer.OptimizeProjectAsync(optimizationRequest);
                
                Console.WriteLine($"SUCCESS: Optimization complete!");
                Console.WriteLine($"Progress Performance Improvements:");
                
                foreach (var improvement in result.Improvements)
                {
                    Console.WriteLine($"  • {improvement.Category}: {improvement.ImprovementFactor:P}");
                    Console.WriteLine($"    {improvement.Description}");
                }
                
                if (!apply)
                {
                    Console.WriteLine($"\nIdea Run with --apply to apply optimizations");
                }
                
                logger.LogInformation("Unity project optimization completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to optimize Unity project");
                Console.WriteLine($"ERROR: Optimization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses optimization target from string
        /// </summary>
        private static UnityOptimizationTarget ParseOptimizationTarget(string target)
        {
            return target.ToLower() switch
            {
                "performance" => UnityOptimizationTarget.Performance,
                "memory" => UnityOptimizationTarget.Memory,
                "build-size" => UnityOptimizationTarget.BuildSize,
                "all" => UnityOptimizationTarget.All,
                _ => UnityOptimizationTarget.All
            };
        }
    }
}
