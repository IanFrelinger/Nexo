using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Services.Learning;
using Nexo.CLI.Commands.Adaptation.Utilities;

namespace Nexo.CLI.Commands.Adaptation.Handlers
{
    /// <summary>
    /// Handles learning insights functionality
    /// </summary>
    public partial class LearningHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public LearningHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task ShowLearningInsights()
        {
            var learningSystem = _serviceProvider.GetRequiredService<IContinuousLearningSystem>();
            var insights = await learningSystem.GetCurrentInsightsAsync();
            var iconHelper = new IconHelper();
            
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
                var confidenceIcon = iconHelper.GetConfidenceIcon(insight.Confidence);
                Console.WriteLine($"{confidenceIcon} {insight.Type}: {insight.Description}");
                Console.WriteLine($"  Confidence: {insight.Confidence:P}");
                Console.WriteLine($"  Recommended Action: {insight.RecommendedAction}");
                Console.WriteLine($"  Discovered: {insight.DiscoveredAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Applied: {(insight.IsApplied ? "SUCCESS:" : "⏳")}");
                Console.WriteLine();
            }
        }
    }
}
