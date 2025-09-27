using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Services.Adaptation;

namespace Nexo.CLI.Commands.Adaptation.Handlers
{
    /// <summary>
    /// Handles adaptation triggering functionality
    /// </summary>
    public partial class TriggerHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public TriggerHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task TriggerAdaptation(string type, string? context)
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
    }
}
