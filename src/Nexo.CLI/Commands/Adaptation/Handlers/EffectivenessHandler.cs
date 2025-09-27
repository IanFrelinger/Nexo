using Microsoft.Extensions.DependencyInjection;
using Nexo.CLI.Commands.Adaptation.Utilities;

namespace Nexo.CLI.Commands.Adaptation.Handlers
{
    /// <summary>
    /// Handles effectiveness analysis functionality
    /// </summary>
    public partial class EffectivenessHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public EffectivenessHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task ShowAdaptationEffectiveness()
        {
            var dashboard = _serviceProvider.GetRequiredService<IAdaptationDashboard>();
            var effectiveness = await dashboard.GetAdaptationEffectivenessAsync();
            var iconHelper = new IconHelper();
            
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
                    var effectivenessIcon = iconHelper.GetEffectivenessIcon(result.EffectivenessScore);
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
                    var typeIcon = iconHelper.GetTypeIcon(kvp.Key);
                    Console.WriteLine($"  {typeIcon} {kvp.Key}: {kvp.Value:P}");
                }
            }
        }
    }
}
