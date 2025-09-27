using Microsoft.Extensions.DependencyInjection;
using Nexo.CLI.Commands.Adaptation.Utilities;

namespace Nexo.CLI.Commands.Adaptation.Handlers
{
    /// <summary>
    /// Handles dashboard display functionality
    /// </summary>
    public partial class DashboardHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public DashboardHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task ShowDashboard()
        {
            var dashboard = _serviceProvider.GetRequiredService<IAdaptationDashboard>();
            var dashboardData = await dashboard.GetRealTimeDashboardDataAsync();
            var iconHelper = new IconHelper();
            
            Console.WriteLine("Stats Nexo Real-Time Adaptation Dashboard");
            Console.WriteLine($"Last Updated: {dashboardData.LastUpdated:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
            
            // Engine Status
            var statusIcon = iconHelper.GetStatusIcon(dashboardData.AdaptationStatus.EngineStatus);
            Console.WriteLine($"{statusIcon} Engine Status: {dashboardData.AdaptationStatus.EngineStatus}");
            Console.WriteLine($"Active Adaptations: {dashboardData.AdaptationStatus.ActiveAdaptations.Count()}");
            Console.WriteLine($"Total Adaptations: {dashboardData.AdaptationStatus.TotalAdaptationsApplied}");
            Console.WriteLine();
            
            // Performance Metrics
            Console.WriteLine("⚡ Performance Metrics:");
            var performanceIcon = iconHelper.GetPerformanceIcon(dashboardData.PerformanceMetrics.Severity);
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
                    var adaptationIcon = iconHelper.GetAdaptationIcon(adaptation.Type);
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
                    var confidenceIcon = iconHelper.GetConfidenceIcon(insight.Confidence);
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
    }
}
