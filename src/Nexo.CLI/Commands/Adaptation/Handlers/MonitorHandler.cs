using Microsoft.Extensions.DependencyInjection;
using Nexo.CLI.Commands.Adaptation.Utilities;

namespace Nexo.CLI.Commands.Adaptation.Handlers
{
    /// <summary>
    /// Handles monitoring functionality
    /// </summary>
    public class MonitorHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public MonitorHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task MonitorAdaptations(int duration)
        {
            var dashboard = _serviceProvider.GetRequiredService<IAdaptationDashboard>();
            var iconHelper = new IconHelper();
            
            Console.WriteLine($"Stats Monitoring adaptations for {duration} seconds...");
            Console.WriteLine("Press Ctrl+C to stop monitoring");
            Console.WriteLine();
            
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(duration)).Token;
            
            try
            {
                await foreach (var adaptationEvent in dashboard.StreamAdaptationEventsAsync(cancellationToken))
                {
                    var timestamp = adaptationEvent.Timestamp.ToString("HH:mm:ss");
                    var eventIcon = iconHelper.GetEventIcon(adaptationEvent.Type);
                    
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
    }
}
