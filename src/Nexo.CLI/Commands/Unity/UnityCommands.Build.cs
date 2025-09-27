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
    /// Build optimize command functionality for UnityCommands.
    /// </summary>
    public static partial class UnityCommands
    {
        /// <summary>
        /// Creates the build-optimize command
        /// </summary>
        private static Command CreateBuildOptimizeCommand(IServiceProvider serviceProvider)
        {
            var buildOptimizeCommand = new Command("build-optimize", "Optimize Unity build for target platforms");
            
            var projectPathOption = new Option<string>(
                "--project-path",
                () => ".",
                "Path to the Unity project directory");
            
            var platformsOption = new Option<string>(
                "--platforms",
                () => "android,ios",
                "Target platforms (comma-separated)");
            
            var applyOption = new Option<bool>(
                "--apply",
                "Apply build optimizations to the project");
            
            buildOptimizeCommand.AddOption(projectPathOption);
            buildOptimizeCommand.AddOption(platformsOption);
            buildOptimizeCommand.AddOption(applyOption);
            
            buildOptimizeCommand.SetHandler(async (projectPath, platforms, apply) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<UnityCommands>>();
                var buildOptimizer = serviceProvider.GetRequiredService<IUnityBuildOptimizer>();
                
                await OptimizeUnityBuild(buildOptimizer, logger, projectPath, platforms, apply);
            }, projectPathOption, platformsOption, applyOption);
            
            return buildOptimizeCommand;
        }

        /// <summary>
        /// Optimizes Unity build for target platforms
        /// </summary>
        private static async Task OptimizeUnityBuild(
            IUnityBuildOptimizer buildOptimizer,
            ILogger logger,
            string projectPath,
            string platforms,
            bool apply)
        {
            try
            {
                logger.LogInformation("Starting Unity build optimization for: {ProjectPath}", projectPath);
                
                var targetPlatforms = platforms.Split(',')
                    .Select(p => Enum.Parse<UnityBuildTarget>(p, true))
                    .ToArray();
                
                Console.WriteLine($"Building Optimizing build for platforms: {string.Join(", ", targetPlatforms)}");
                
                var buildRequest = new UnityBuildRequest
                {
                    ProjectPath = projectPath,
                    TargetPlatforms = targetPlatforms,
                    BuildSettings = await LoadCurrentBuildSettings(projectPath)
                };
                
                var optimization = await buildOptimizer.OptimizeBuildAsync(buildRequest);
                
                Console.WriteLine($"SUCCESS: Build optimization complete!");
                
                foreach (var platformOpt in optimization.PlatformOptimizations)
                {
                    Console.WriteLine($"\nMobile {platformOpt.Key} Optimizations:");
                    foreach (var appliedOpt in platformOpt.Value.AppliedOptimizations)
                    {
                        Console.WriteLine($"  • {appliedOpt}");
                    }
                }
                
                if (apply)
                {
                    await buildOptimizer.ApplyBuildOptimizationsAsync(projectPath, optimization);
                    Console.WriteLine($"SUCCESS: Optimizations applied to project");
                }
                else
                {
                    Console.WriteLine($"\nIdea Run with --apply to apply build optimizations");
                }
                
                logger.LogInformation("Unity build optimization completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to optimize Unity build");
                Console.WriteLine($"ERROR: Build optimization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads current build settings from Unity project
        /// </summary>
        private static async Task<UnityBuildSettings> LoadCurrentBuildSettings(string projectPath)
        {
            // This would load actual build settings from Unity project
            // For now, return default settings
            return new UnityBuildSettings
            {
                ScriptingBackend = ScriptingImplementation.IL2CPP,
                ApiCompatibilityLevel = ApiCompatibilityLevel.NET_Standard_2_0,
                CodeOptimization = CodeOptimization.Release,
                StrippingLevel = StrippingLevel.High
            };
        }
    }
}
