using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Application.Services.Learning;
using Nexo.Feature.Monitoring.Services;
using System.CommandLine;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI commands for managing real-time adaptation capabilities
/// </summary>
[Command("adaptation")]
public class AdaptationCommands
{
    private readonly IServiceProvider _serviceProvider;
    
    public AdaptationCommands(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    [Command("status")]
    public async Task ShowAdaptationStatus()
    {
        var adaptationEngine = _serviceProvider.GetRequiredService<IAdaptationEngine>();
        var status = await adaptationEngine.GetAdaptationStatusAsync();
        
        Console.WriteLine("Processing Nexo Real-Time Adaptation Status");
        Console.WriteLine($"Engine Status: {status.EngineStatus}");
        Console.WriteLine($"Active Adaptations: {status.ActiveAdaptations.Count()}");
        Console.WriteLine($"Recent Improvements: {status.RecentImprovements.Count()}");
        Console.WriteLine($"Total Adaptations Applied: {status.TotalAdaptationsApplied}");
        Console.WriteLine($"Overall Effectiveness: {status.OverallEffectiveness:P}");
        Console.WriteLine($"Last Adaptation: {status.LastAdaptationTime:yyyy-MM-dd HH:mm:ss}");
        
        if (status.ActiveAdaptations.Any())
        {
            Console.WriteLine("\nActive Adaptations:");
            foreach (var adaptation in status.ActiveAdaptations)
            {
                Console.WriteLine($"  • {adaptation.Type}: {adaptation.Description}");
                Console.WriteLine($"    Applied: {adaptation.AppliedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"    Improvement: {adaptation.ActualImprovement:P}");
                Console.WriteLine($"    Strategy: {adaptation.StrategyId}");
            }
        }
        
        if (status.RecentImprovements.Any())
        {
            Console.WriteLine("\nRecent Improvements:");
            foreach (var improvement in status.RecentImprovements.Take(5))
            {
                Console.WriteLine($"  • {improvement.Metric}: {improvement.ImprovementPercentage:P}");
                Console.WriteLine($"    Measured: {improvement.MeasuredAt:yyyy-MM-dd HH:mm:ss}");
            }
        }
    }
    
    [Command("monitor")]
    public async Task MonitorAdaptations(
        [Option] int duration = 300)
    {
        var dashboard = _serviceProvider.GetRequiredService<IAdaptationDashboard>();
        
        Console.WriteLine($"Stats Monitoring adaptations for {duration} seconds...");
        Console.WriteLine("Press Ctrl+C to stop monitoring");
        Console.WriteLine();
        
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(duration)).Token;
        
        try
        {
            await foreach (var adaptationEvent in dashboard.StreamAdaptationEventsAsync(cancellationToken))
            {
                var timestamp = adaptationEvent.Timestamp.ToString("HH:mm:ss");
                var eventIcon = GetEventIcon(adaptationEvent.Type);
                
                Console.WriteLine($"[{timestamp}] {eventIcon} {adaptationEvent.Type}: {adaptationEvent.Description}");
                
                if (adaptationEvent.PerformanceImpact.HasValue)
                {
                    var impactIcon = adaptationEvent.PerformanceImpact.Value > 0 ? "Progress" : "📉";
                    Console.WriteLine($"    {impactIcon} Performance Impact: {adaptationEvent.PerformanceImpact:P}");
                }
                
                if (adaptationEvent.EventData.Any())
                {
                    Console.WriteLine("    Data:");
                    foreach (var kvp in adaptationEvent.EventData)
                    {
                        Console.WriteLine($"      {kvp.Key}: {kvp.Value}");
                    }
                }
                
                Console.WriteLine();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nStopped  Monitoring stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: Error during monitoring: {ex.Message}");
        }
    }
    
    [Command("trigger")]
    public async Task TriggerAdaptation(
        [Option] string type,
        [Option] string? context = null)
    {
        var adaptationEngine = _serviceProvider.GetRequiredService<IAdaptationEngine>();
        
        if (!Enum.TryParse<AdaptationType>(type, true, out var adaptationType))
        {
            Console.WriteLine($"ERROR: Invalid adaptation type: {type}");
            Console.WriteLine("Valid types: PerformanceOptimization, ResourceOptimization, UserExperienceOptimization, EnvironmentOptimization");
            return;
        }
        
        var adaptationContext = CreateAdaptationContext(adaptationType, context);
        
        Console.WriteLine($"Running Triggering {adaptationType} adaptation...");
        
        try
        {
            await adaptationEngine.TriggerAdaptationAsync(adaptationContext);
            Console.WriteLine("SUCCESS: Adaptation triggered successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to trigger adaptation: {ex.Message}");
        }
    }
    
    [Command("learning")]
    public async Task ShowLearningInsights()
    {
        var learningSystem = _serviceProvider.GetRequiredService<IContinuousLearningSystem>();
        var insights = await learningSystem.GetCurrentInsightsAsync();
        
        Console.WriteLine("🧠 Current Learning Insights");
        Console.WriteLine($"Total Insights: {insights.Count()}");
        Console.WriteLine();
        
        if (!insights.Any())
        {
            Console.WriteLine("No learning insights available yet. The system is still learning from usage patterns.");
            return;
        }
        
        foreach (var insight in insights.OrderByDescending(i => i.Confidence))
        {
            var confidenceIcon = GetConfidenceIcon(insight.Confidence);
            Console.WriteLine($"{confidenceIcon} {insight.Type}: {insight.Description}");
            Console.WriteLine($"  Confidence: {insight.Confidence:P}");
            Console.WriteLine($"  Recommended Action: {insight.RecommendedAction}");
            Console.WriteLine($"  Discovered: {insight.DiscoveredAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Applied: {(insight.IsApplied ? "SUCCESS:" : "⏳")}");
            Console.WriteLine();
        }
    }
    
    [Command("effectiveness")]
    public async Task ShowAdaptationEffectiveness()
    {
        var dashboard = _serviceProvider.GetRequiredService<IAdaptationDashboard>();
        var effectiveness = await dashboard.GetAdaptationEffectivenessAsync();
        
        Console.WriteLine("Progress Adaptation Effectiveness Analysis");
        Console.WriteLine($"Overall Effectiveness: {effectiveness.OverallEffectiveness:P}");
        Console.WriteLine($"Total Adaptations: {effectiveness.TotalAdaptations}");
        Console.WriteLine($"Successful Adaptations: {effectiveness.SuccessfulAdaptations}");
        Console.WriteLine($"Success Rate: {(effectiveness.TotalAdaptations > 0 ? (double)effectiveness.SuccessfulAdaptations / effectiveness.TotalAdaptations : 0):P}");
        Console.WriteLine($"Average Improvement: {effectiveness.AverageImprovement:P}");
        Console.WriteLine();
        
        if (effectiveness.AdaptationResults.Any())
        {
            Console.WriteLine("Recent Adaptations:");
            foreach (var result in effectiveness.AdaptationResults.OrderByDescending(r => r.AppliedAt).Take(10))
            {
                var effectivenessIcon = GetEffectivenessIcon(result.EffectivenessScore);
                Console.WriteLine($"  {effectivenessIcon} {result.AdaptationType}:");
                Console.WriteLine($"    Expected: {result.ExpectedImprovement:P}");
                Console.WriteLine($"    Actual: {result.ActualImprovement:P}");
                Console.WriteLine($"    Effectiveness: {result.EffectivenessScore:P}");
                Console.WriteLine($"    Applied: {result.AppliedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }
        }
        
        if (effectiveness.EffectivenessByType.Any())
        {
            Console.WriteLine("Effectiveness by Type:");
            foreach (var kvp in effectiveness.EffectivenessByType.OrderByDescending(x => x.Value))
            {
                var typeIcon = GetTypeIcon(kvp.Key);
                Console.WriteLine($"  {typeIcon} {kvp.Key}: {kvp.Value:P}");
            }
        }
    }
    
    [Command("dashboard")]
    public async Task ShowDashboard()
    {
        var dashboard = _serviceProvider.GetRequiredService<IAdaptationDashboard>();
        var dashboardData = await dashboard.GetRealTimeDashboardDataAsync();
        
        Console.WriteLine("Stats Nexo Real-Time Adaptation Dashboard");
        Console.WriteLine($"Last Updated: {dashboardData.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();
        
        // Engine Status
        var statusIcon = GetStatusIcon(dashboardData.AdaptationStatus.EngineStatus);
        Console.WriteLine($"{statusIcon} Engine Status: {dashboardData.AdaptationStatus.EngineStatus}");
        Console.WriteLine($"Active Adaptations: {dashboardData.AdaptationStatus.ActiveAdaptations.Count()}");
        Console.WriteLine($"Total Adaptations: {dashboardData.AdaptationStatus.TotalAdaptationsApplied}");
        Console.WriteLine();
        
        // Performance Metrics
        Console.WriteLine("⚡ Performance Metrics:");
        var performanceIcon = GetPerformanceIcon(dashboardData.PerformanceMetrics.Severity);
        Console.WriteLine($"  {performanceIcon} Overall Score: {dashboardData.PerformanceMetrics.OverallScore:P}");
        Console.WriteLine($"  CPU Utilization: {dashboardData.PerformanceMetrics.CpuUtilization:P}");
        Console.WriteLine($"  Memory Utilization: {dashboardData.PerformanceMetrics.MemoryUtilization:P}");
        Console.WriteLine($"  Response Time: {dashboardData.PerformanceMetrics.ResponseTime:F0}ms");
        Console.WriteLine($"  Throughput: {dashboardData.PerformanceMetrics.Throughput:F0} ops/sec");
        Console.WriteLine();
        
        // Recent Adaptations
        if (dashboardData.RecentAdaptations.Any())
        {
            Console.WriteLine("Processing Recent Adaptations:");
            foreach (var adaptation in dashboardData.RecentAdaptations.Take(5))
            {
                var adaptationIcon = GetAdaptationIcon(adaptation.Type);
                Console.WriteLine($"  {adaptationIcon} {adaptation.Type}: {adaptation.Description}");
                Console.WriteLine($"    Applied: {adaptation.AppliedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"    Improvement: {adaptation.EstimatedImprovementFactor:P}");
            }
            Console.WriteLine();
        }
        
        // Learning Insights
        if (dashboardData.LearningInsights.Any())
        {
            Console.WriteLine("🧠 Recent Learning Insights:");
            foreach (var insight in dashboardData.LearningInsights.Take(3))
            {
                var confidenceIcon = GetConfidenceIcon(insight.Confidence);
                Console.WriteLine($"  {confidenceIcon} {insight.Type}: {insight.Description}");
                Console.WriteLine($"    Confidence: {insight.Confidence:P}");
            }
            Console.WriteLine();
        }
        
        // Environment Status
        Console.WriteLine("🌍 Environment Status:");
        Console.WriteLine($"  Context: {dashboardData.EnvironmentStatus.CurrentEnvironment.Context}");
        Console.WriteLine($"  Platform: {dashboardData.EnvironmentStatus.CurrentEnvironment.Platform}");
        Console.WriteLine($"  Active Optimizations: {dashboardData.EnvironmentStatus.ActiveOptimizations.Count()}");
        Console.WriteLine($"  Validation: {(dashboardData.EnvironmentStatus.ValidationResult.IsValid ? "SUCCESS: Valid" : "ERROR: Invalid")}");
    }
    
    [Command("environment")]
    public async Task ShowEnvironmentStatus()
    {
        var dashboard = _serviceProvider.GetRequiredService<IAdaptationDashboard>();
        var environmentStatus = await dashboard.GetEnvironmentAdaptationStatusAsync();
        
        Console.WriteLine("🌍 Environment Adaptation Status");
        Console.WriteLine($"Last Check: {environmentStatus.LastEnvironmentCheck:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();
        
        var env = environmentStatus.CurrentEnvironment;
        Console.WriteLine("Current Environment:");
        Console.WriteLine($"  Context: {env.Context}");
        Console.WriteLine($"  Platform: {env.Platform}");
        Console.WriteLine($"  CPU Cores: {env.Resources.CpuCores}");
        Console.WriteLine($"  Total Memory: {env.Resources.TotalMemoryMB:N0} MB");
        Console.WriteLine($"  Available Memory: {env.Resources.AvailableMemoryMB:N0} MB");
        Console.WriteLine($"  CPU Utilization: {env.Resources.CpuUtilization:P}");
        Console.WriteLine($"  Memory Utilization: {env.Resources.MemoryUtilization:P}");
        Console.WriteLine($"  Resource Constrained: {(env.Resources.IsResourceConstrained ? "Yes" : "No")}");
        Console.WriteLine();
        
        if (environmentStatus.ActiveOptimizations.Any())
        {
            Console.WriteLine("Active Optimizations:");
            foreach (var optimization in environmentStatus.ActiveOptimizations)
            {
                Console.WriteLine($"  • {optimization.Type}: {optimization.Description}");
                Console.WriteLine($"    Priority: {optimization.Priority}");
                Console.WriteLine($"    Enabled: {(optimization.IsEnabled ? "Yes" : "No")}");
            }
            Console.WriteLine();
        }
        
        if (environmentStatus.RecentChanges.Any())
        {
            Console.WriteLine("Recent Environment Changes:");
            foreach (var change in environmentStatus.RecentChanges.Take(5))
            {
                Console.WriteLine($"  • {change.ChangeType}: {change.Description}");
                Console.WriteLine($"    Changed: {change.ChangedAt:yyyy-MM-dd HH:mm:ss}");
            }
            Console.WriteLine();
        }
        
        var validation = environmentStatus.ValidationResult;
        Console.WriteLine("Environment Validation:");
        Console.WriteLine($"  Valid: {(validation.IsValid ? "SUCCESS:" : "ERROR:")}");
        
        if (validation.ValidationErrors.Any())
        {
            Console.WriteLine("  Errors:");
            foreach (var error in validation.ValidationErrors)
            {
                Console.WriteLine($"    ERROR: {error}");
            }
        }
        
        if (validation.ValidationWarnings.Any())
        {
            Console.WriteLine("  Warnings:");
            foreach (var warning in validation.ValidationWarnings)
            {
                Console.WriteLine($"    WARNING:  {warning}");
            }
        }
        
        if (validation.Recommendations.Any())
        {
            Console.WriteLine("  Recommendations:");
            foreach (var recommendation in validation.Recommendations)
            {
                Console.WriteLine($"    Idea {recommendation}");
            }
        }
    }
    
    [Command("start")]
    public async Task StartAdaptationEngine()
    {
        var adaptationEngine = _serviceProvider.GetRequiredService<IAdaptationEngine>();
        
        Console.WriteLine("Running Starting adaptation engine...");
        
        try
        {
            await adaptationEngine.StartAdaptationAsync();
            Console.WriteLine("SUCCESS: Adaptation engine started successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to start adaptation engine: {ex.Message}");
        }
    }
    
    [Command("stop")]
    public async Task StopAdaptationEngine()
    {
        var adaptationEngine = _serviceProvider.GetRequiredService<IAdaptationEngine>();
        
        Console.WriteLine("Stopped  Stopping adaptation engine...");
        
        try
        {
            await adaptationEngine.StopAdaptationAsync();
            Console.WriteLine("SUCCESS: Adaptation engine stopped successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to stop adaptation engine: {ex.Message}");
        }
    }
    
    private AdaptationContext CreateAdaptationContext(AdaptationType type, string? context)
    {
        return new AdaptationContext
        {
            Trigger = AdaptationTrigger.ManualTrigger,
            Priority = AdaptationPriority.Medium,
            Description = context ?? $"Manual trigger for {type}",
            AdditionalData = new Dictionary<string, object>
            {
                ["ManualTrigger"] = true,
                ["TriggeredBy"] = "CLI",
                ["Context"] = context ?? "No specific context"
            }
        };
    }
    
    private string GetEventIcon(AdaptationEventType eventType)
    {
        return eventType switch
        {
            AdaptationEventType.AdaptationApplied => "Processing",
            AdaptationEventType.PerformanceDegradation => "📉",
            AdaptationEventType.ResourceConstraint => "WARNING:",
            AdaptationEventType.UserFeedback => "💬",
            AdaptationEventType.EnvironmentChange => "🌍",
            AdaptationEventType.LearningInsight => "🧠",
            AdaptationEventType.ErrorOccurred => "ERROR:",
            _ => "Stats"
        };
    }
    
    private string GetConfidenceIcon(double confidence)
    {
        return confidence switch
        {
            >= 0.8 => "🟢",
            >= 0.6 => "🟡",
            >= 0.4 => "🟠",
            _ => "🔴"
        };
    }
    
    private string GetEffectivenessIcon(double effectiveness)
    {
        return effectiveness switch
        {
            >= 1.0 => "🟢",
            >= 0.8 => "🟡",
            >= 0.6 => "🟠",
            _ => "🔴"
        };
    }
    
    private string GetStatusIcon(AdaptationEngineStatus status)
    {
        return status switch
        {
            AdaptationEngineStatus.Running => "🟢",
            AdaptationEngineStatus.Starting => "🟡",
            AdaptationEngineStatus.Stopping => "🟠",
            AdaptationEngineStatus.Stopped => "⚪",
            AdaptationEngineStatus.Error => "🔴",
            _ => "UNKNOWN"
        };
    }
    
    private string GetPerformanceIcon(PerformanceSeverity severity)
    {
        return severity switch
        {
            PerformanceSeverity.Critical => "🔴",
            PerformanceSeverity.High => "🟠",
            PerformanceSeverity.Medium => "🟡",
            PerformanceSeverity.Low => "🟢",
            PerformanceSeverity.None => "⚪",
            _ => "UNKNOWN"
        };
    }
    
    private string GetAdaptationIcon(string adaptationType)
    {
        return adaptationType switch
        {
            "PerformanceOptimization" => "⚡",
            "ResourceOptimization" => "💾",
            "UserExperienceOptimization" => "👤",
            "EnvironmentOptimization" => "🌍",
            "SecurityOptimization" => "Security",
            "ReliabilityOptimization" => "Safety",
            _ => "Processing"
        };
    }
    
    private string GetTypeIcon(string type)
    {
        return type switch
        {
            "PerformanceOptimization" => "⚡",
            "ResourceOptimization" => "💾",
            "UserExperienceOptimization" => "👤",
            "EnvironmentOptimization" => "🌍",
            "SecurityOptimization" => "Security",
            "ReliabilityOptimization" => "Safety",
            _ => "Stats"
        };
    }
    
    /// <summary>
    /// Create the adaptation command group
    /// </summary>
    public static Command CreateAdaptationCommand(IServiceProvider serviceProvider)
    {
        var adaptationCommand = new Command("adaptation", "Real-time adaptation management commands");
        
        // Status command
        var statusCommand = new Command("status", "Show current adaptation status");
        statusCommand.SetHandler(async () =>
        {
            var commands = new AdaptationCommands(serviceProvider);
            await commands.ShowAdaptationStatus();
        });
        adaptationCommand.AddCommand(statusCommand);
        
        // Monitor command
        var monitorCommand = new Command("monitor", "Monitor adaptations in real-time");
        var durationOption = new Option<int>("--duration", () => 300, "Duration in seconds to monitor");
        monitorCommand.AddOption(durationOption);
        monitorCommand.SetHandler(async (duration) =>
        {
            var commands = new AdaptationCommands(serviceProvider);
            await commands.MonitorAdaptations(duration);
        }, durationOption);
        adaptationCommand.AddCommand(monitorCommand);
        
        // Trigger command
        var triggerCommand = new Command("trigger", "Trigger a specific adaptation");
        var typeOption = new Option<string>("--type", "Type of adaptation to trigger");
        var contextOption = new Option<string>("--context", "Context for the adaptation");
        triggerCommand.AddOption(typeOption);
        triggerCommand.AddOption(contextOption);
        triggerCommand.SetHandler(async (type, context) =>
        {
            var commands = new AdaptationCommands(serviceProvider);
            await commands.TriggerAdaptation(type, context);
        }, typeOption, contextOption);
        adaptationCommand.AddCommand(triggerCommand);
        
        // Learning command
        var learningCommand = new Command("learning", "Show learning insights");
        learningCommand.SetHandler(async () =>
        {
            var commands = new AdaptationCommands(serviceProvider);
            await commands.ShowLearningInsights();
        });
        adaptationCommand.AddCommand(learningCommand);
        
        // Effectiveness command
        var effectivenessCommand = new Command("effectiveness", "Show adaptation effectiveness");
        effectivenessCommand.SetHandler(async () =>
        {
            var commands = new AdaptationCommands(serviceProvider);
            await commands.ShowAdaptationEffectiveness();
        });
        adaptationCommand.AddCommand(effectivenessCommand);
        
        // Dashboard command
        var dashboardCommand = new Command("dashboard", "Show real-time adaptation dashboard");
        dashboardCommand.SetHandler(async () =>
        {
            var commands = new AdaptationCommands(serviceProvider);
            await commands.ShowDashboard();
        });
        adaptationCommand.AddCommand(dashboardCommand);
        
        // Environment command
        var environmentCommand = new Command("environment", "Show environment adaptation status");
        environmentCommand.SetHandler(async () =>
        {
            var commands = new AdaptationCommands(serviceProvider);
            await commands.ShowEnvironmentStatus();
        });
        adaptationCommand.AddCommand(environmentCommand);
        
        // Start command
        var startCommand = new Command("start", "Start the adaptation engine");
        startCommand.SetHandler(async () =>
        {
            var commands = new AdaptationCommands(serviceProvider);
            await commands.StartAdaptationEngine();
        });
        adaptationCommand.AddCommand(startCommand);
        
        // Stop command
        var stopCommand = new Command("stop", "Stop the adaptation engine");
        stopCommand.SetHandler(async () =>
        {
            var commands = new AdaptationCommands(serviceProvider);
            await commands.StopAdaptationEngine();
        });
        adaptationCommand.AddCommand(stopCommand);
        
        return adaptationCommand;
    }
}