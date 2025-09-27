using System;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.CLI.Commands.Unity
{
    /// <summary>
    /// Monitor command functionality for UnityCommands.
    /// </summary>
    public static partial class UnityCommands
    {
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
                
                Console.WriteLine($"Stats Monitoring Unity project performance for {duration} seconds...");
                
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
                
                Console.WriteLine($"\nList Performance Report:");
                Console.WriteLine($"  Average FPS: {report.AverageFrameRate:F1}");
                Console.WriteLine($"  Min FPS: {report.MinFrameRate:F1}");
                Console.WriteLine($"  Performance Issues: {report.CriticalEvents.Count()}");
                Console.WriteLine($"  Optimization Opportunities: {report.OptimizationOpportunities.Count()}");
                
                logger.LogInformation("Unity project performance monitoring completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to monitor Unity project performance");
                Console.WriteLine($"ERROR: Monitoring failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Streams real-time performance updates
        /// </summary>
        private static async Task StreamRealTimePerformance(IGamePerformanceMonitor monitor, CancellationToken cancellationToken)
        {
            Console.WriteLine($"\nStats Real-time Performance Updates:");
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
    }
}
