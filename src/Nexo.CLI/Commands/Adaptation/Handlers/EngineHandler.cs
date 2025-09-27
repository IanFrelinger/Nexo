using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Services.Adaptation;

namespace Nexo.CLI.Commands.Adaptation.Handlers
{
    /// <summary>
    /// Handles engine control functionality
    /// </summary>
    public class EngineHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public EngineHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

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
    }
}
