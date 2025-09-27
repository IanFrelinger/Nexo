using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Services.Adaptation;

namespace Nexo.CLI.Commands.Adaptation.Handlers
{
    /// <summary>
    /// Handles status display functionality
    /// </summary>
    public partial class StatusHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public StatusHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

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
    }
}
