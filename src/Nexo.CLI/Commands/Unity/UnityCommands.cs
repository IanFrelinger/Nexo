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
    /// Unity-specific CLI commands for game development
    /// </summary>
    public static class UnityCommands
    {
        /// <summary>
        /// Creates the main Unity command with all subcommands
        /// </summary>
        public static Command CreateUnityCommand(IServiceProvider serviceProvider)
        {
            var unityCommand = new Command("unity", "Unity game development tools and optimizations");
            
            // Add subcommands
            unityCommand.AddCommand(CreateAnalyzeCommand(serviceProvider));
            unityCommand.AddCommand(CreateOptimizeCommand(serviceProvider));
            unityCommand.AddCommand(CreateMonitorCommand(serviceProvider));
            unityCommand.AddCommand(CreateBuildOptimizeCommand(serviceProvider));
            
            return unityCommand;
        }
        
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
        /// Creates the monitor command
        /// </summary>
        private static Command CreateMonitorCommand(IServiceProvider serviceProvider)
        {
            var monitorCommand = new Command("monitor", "Monitor Unity project performance in real-time");
            
            var projectPathOption = new Option<string>(
                "--project-path",
                () => ".",
                "Path to the Unity project directory");
            
            var durationOption = new Option<int>(
                "--duration",
                () => 300,
                "Monitoring duration in seconds");
            
            var realTimeOption = new Option<bool>(
                "--real-time",
                () => true,
                "Show real-time performance updates");
            
            monitorCommand.AddOption(projectPathOption);
            monitorCommand.AddOption(durationOption);
            monitorCommand.AddOption(realTimeOption);
            
            monitorCommand.SetHandler(async (projectPath, duration, realTime) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<UnityCommands>>();
                var monitor = serviceProvider.GetRequiredService<IGamePerformanceMonitor>();
                
                await MonitorUnityProject(monitor, logger, projectPath, duration, realTime);
            }, projectPathOption, durationOption, realTimeOption);
            
            return monitorCommand;
        }
        
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
                
                Console.WriteLine($"🔍 Analyzing Unity project at {projectPath}...");
                
                var analysis = await analyzer.AnalyzeProjectAsync(projectPath);
                
                Console.WriteLine($"✅ Analysis complete!");
                Console.WriteLine($"📊 Project Analysis Results:");
                Console.WriteLine($"  Scripts: {analysis.ScriptAnalysis.Scripts.Count()}");
                Console.WriteLine($"  Scenes: {analysis.SceneAnalysis.Scenes.Count()}");
                Console.WriteLine($"  Assets: {analysis.AssetAnalysis.Assets.Count()}");
                Console.WriteLine($"  Optimization Opportunities: {analysis.IterationOptimizations.Count()}");
                
                if (performance)
                {
                    Console.WriteLine($"\n🚀 Performance Optimization Opportunities:");
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
                Console.WriteLine($"❌ Analysis failed: {ex.Message}");
            }
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
                
                Console.WriteLine($"🚀 Optimizing Unity project for {target}...");
                
                var optimizationRequest = new UnityOptimizationRequest
                {
                    ProjectPath = projectPath,
                    OptimizationTarget = ParseOptimizationTarget(target),
                    ApplyOptimizations = apply
                };
                
                var result = await optimizer.OptimizeProjectAsync(optimizationRequest);
                
                Console.WriteLine($"✅ Optimization complete!");
                Console.WriteLine($"📈 Performance Improvements:");
                
                foreach (var improvement in result.Improvements)
                {
                    Console.WriteLine($"  • {improvement.Category}: {improvement.ImprovementFactor:P}");
                    Console.WriteLine($"    {improvement.Description}");
                }
                
                if (!apply)
                {
                    Console.WriteLine($"\n💡 Run with --apply to apply optimizations");
                }
                
                logger.LogInformation("Unity project optimization completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to optimize Unity project");
                Console.WriteLine($"❌ Optimization failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Monitors Unity project performance in real-time
        /// </summary>
        private static async Task MonitorUnityProject(
            IGamePerformanceMonitor monitor,
            ILogger logger,
            string projectPath,
            int duration,
            bool realTime)
        {
            try
            {
                logger.LogInformation("Starting Unity project performance monitoring for: {ProjectPath}", projectPath);
                
                Console.WriteLine($"📊 Monitoring Unity project performance for {duration} seconds...");
                
                var config = new GameMonitoringConfiguration
                {
                    GameName = "Unity Project",
                    ProjectPath = projectPath,
                    MonitoringInterval = TimeSpan.FromSeconds(1),
                    MaxHistorySize = duration,
                    CancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(duration)).Token
                };
                
                await monitor.StartMonitoringAsync(config);
                
                if (realTime)
                {
                    await StreamRealTimePerformance(monitor, config.CancellationToken);
                }
                
                // Generate final report
                var report = await monitor.GeneratePerformanceReportAsync(TimeSpan.FromSeconds(duration));
                
                Console.WriteLine($"\n📋 Performance Report:");
                Console.WriteLine($"  Average FPS: {report.AverageFrameRate:F1}");
                Console.WriteLine($"  Min FPS: {report.MinFrameRate:F1}");
                Console.WriteLine($"  Performance Issues: {report.CriticalEvents.Count()}");
                Console.WriteLine($"  Optimization Opportunities: {report.OptimizationOpportunities.Count()}");
                
                logger.LogInformation("Unity project performance monitoring completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to monitor Unity project performance");
                Console.WriteLine($"❌ Monitoring failed: {ex.Message}");
            }
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
                
                Console.WriteLine($"🏗️ Optimizing build for platforms: {string.Join(", ", targetPlatforms)}");
                
                var buildRequest = new UnityBuildRequest
                {
                    ProjectPath = projectPath,
                    TargetPlatforms = targetPlatforms,
                    BuildSettings = await LoadCurrentBuildSettings(projectPath)
                };
                
                var optimization = await buildOptimizer.OptimizeBuildAsync(buildRequest);
                
                Console.WriteLine($"✅ Build optimization complete!");
                
                foreach (var platformOpt in optimization.PlatformOptimizations)
                {
                    Console.WriteLine($"\n📱 {platformOpt.Key} Optimizations:");
                    foreach (var appliedOpt in platformOpt.Value.AppliedOptimizations)
                    {
                        Console.WriteLine($"  • {appliedOpt}");
                    }
                }
                
                if (apply)
                {
                    await buildOptimizer.ApplyBuildOptimizationsAsync(projectPath, optimization);
                    Console.WriteLine($"✅ Optimizations applied to project");
                }
                else
                {
                    Console.WriteLine($"\n💡 Run with --apply to apply build optimizations");
                }
                
                logger.LogInformation("Unity build optimization completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to optimize Unity build");
                Console.WriteLine($"❌ Build optimization failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows detailed analysis results
        /// </summary>
        private static async Task ShowDetailedAnalysis(UnityProjectAnalysis analysis)
        {
            Console.WriteLine($"\n📋 Detailed Analysis Results:");
            
            // Script analysis
            Console.WriteLine($"\n📝 Script Analysis:");
            foreach (var script in analysis.ScriptAnalysis.Scripts.Take(10))
            {
                Console.WriteLine($"  • {script.Name} ({script.LinesOfCode} lines)");
                if (script.PerformanceIssues.Any())
                {
                    Console.WriteLine($"    ⚠️ {script.PerformanceIssues.Count()} performance issues");
                }
            }
            
            // Asset analysis
            Console.WriteLine($"\n🎨 Asset Analysis:");
            Console.WriteLine($"  Total Assets: {analysis.AssetAnalysis.Assets.Count()}");
            Console.WriteLine($"  Total Size: {analysis.AssetAnalysis.TotalAssetSize / 1024 / 1024:F1} MB");
            Console.WriteLine($"  Optimizable Size: {analysis.AssetAnalysis.OptimizableAssetSize / 1024 / 1024:F1} MB");
            
            // Scene analysis
            Console.WriteLine($"\n🎬 Scene Analysis:");
            foreach (var scene in analysis.SceneAnalysis.Scenes.Take(5))
            {
                Console.WriteLine($"  • {scene.Name}: {scene.GameObjects} objects, {scene.DrawCalls} draw calls");
            }
            
            // Performance recommendations
            Console.WriteLine($"\n💡 Performance Recommendations:");
            foreach (var rec in analysis.PerformanceRecommendations.Take(5))
            {
                Console.WriteLine($"  • {rec.Category}: {rec.Description}");
                Console.WriteLine($"    Est. Improvement: {rec.EstimatedImprovement:P}");
            }
        }
        
        /// <summary>
        /// Streams real-time performance updates
        /// </summary>
        private static async Task StreamRealTimePerformance(IGamePerformanceMonitor monitor, CancellationToken cancellationToken)
        {
            Console.WriteLine($"\n📊 Real-time Performance Updates:");
            Console.WriteLine($"{"Time",-8} {"FPS",-6} {"CPU",-6} {"GPU",-6} {"Memory",-8} {"GC",-6}");
            Console.WriteLine(new string('-', 50));
            
            var startTime = DateTime.UtcNow;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var snapshot = await monitor.GetCurrentPerformanceSnapshotAsync();
                    var elapsed = DateTime.UtcNow - startTime;
                    
                    Console.WriteLine($"{elapsed.TotalSeconds:F0}s    {snapshot.FrameRate:F1}   {snapshot.CpuTime:F1}   {snapshot.GpuTime:F1}   {snapshot.MemoryUsage / 1024 / 1024:F1}MB   {snapshot.GarbageCollectionTime:F1}");
                    
                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting performance snapshot: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
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
    
    /// <summary>
    /// Unity project optimizer interface
    /// </summary>
    public interface IUnityProjectOptimizer
    {
        Task<UnityOptimizationResult> OptimizeProjectAsync(UnityOptimizationRequest request);
    }
    
    /// <summary>
    /// Unity optimization request
    /// </summary>
    public class UnityOptimizationRequest
    {
        public string ProjectPath { get; set; } = string.Empty;
        public UnityOptimizationTarget OptimizationTarget { get; set; }
        public bool ApplyOptimizations { get; set; }
    }
    
    /// <summary>
    /// Unity optimization result
    /// </summary>
    public class UnityOptimizationResult
    {
        public bool Success { get; set; }
        public IEnumerable<OptimizationImprovement> Improvements { get; set; } = new List<OptimizationImprovement>();
        public string Summary { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Optimization improvement
    /// </summary>
    public class OptimizationImprovement
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double ImprovementFactor { get; set; }
    }
    
    /// <summary>
    /// Unity optimization target
    /// </summary>
    public enum UnityOptimizationTarget
    {
        Performance,
        Memory,
        BuildSize,
        All
    }
}
