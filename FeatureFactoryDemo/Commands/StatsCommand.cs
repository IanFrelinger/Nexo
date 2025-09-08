using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Services;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Command to display statistics about the Feature Factory system
    /// </summary>
    public class StatsCommand : BaseCommand
    {
        private readonly CommandHistoryService _commandHistoryService;
        private readonly CodebaseAnalysisService _codebaseAnalysisService;
        
        public override string Name => "stats";
        public override string Description => "Display statistics about the Feature Factory system";
        public override string Usage => "stats [--command-history] [--codebase] [--all]";
        
        public StatsCommand(IServiceProvider serviceProvider, ILogger<StatsCommand> logger) 
            : base(serviceProvider, logger)
        {
            _commandHistoryService = serviceProvider.GetRequiredService<CommandHistoryService>();
            _codebaseAnalysisService = serviceProvider.GetRequiredService<CodebaseAnalysisService>();
        }
        
        public override async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                Console.WriteLine("📊 Feature Factory Statistics");
                Console.WriteLine("=============================");
                
                // Parse arguments
                bool showCommandHistory = false;
                bool showCodebase = false;
                bool showAll = false;
                
                foreach (var arg in args)
                {
                    switch (arg)
                    {
                        case "--command-history":
                            showCommandHistory = true;
                            break;
                        case "--codebase":
                            showCodebase = true;
                            break;
                        case "--all":
                            showAll = true;
                            break;
                        case "--help":
                        case "-h":
                            DisplayHelp();
                            return 0;
                    }
                }
                
                // If no specific options, show all
                if (!showCommandHistory && !showCodebase)
                {
                    showAll = true;
                }
                
                if (showAll || showCommandHistory)
                {
                    await DisplayCommandHistoryStats();
                }
                
                if (showAll || showCodebase)
                {
                    await DisplayCodebaseStats();
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stats command failed");
                DisplayError($"Stats command failed: {ex.Message}");
                return 1;
            }
        }
        
        private async Task DisplayCommandHistoryStats()
        {
            try
            {
                Console.WriteLine("\n📋 Command History Statistics:");
                Console.WriteLine("==============================");
                
                var stats = await _commandHistoryService.GetStatisticsAsync();
                
                Console.WriteLine($"   Total Commands: {stats.TotalCommands}");
                Console.WriteLine($"   Successful Commands: {stats.SuccessfulCommands}");
                Console.WriteLine($"   Success Rate: {stats.SuccessRate:F1}%");
                Console.WriteLine($"   Average Quality Score: {stats.AverageQualityScore}/100");
                Console.WriteLine($"   Average Iterations: {stats.AverageIterations}");
                
                // Get recent commands
                var recentCommands = await _commandHistoryService.GetRecentSuccessfulCommandsAsync(5);
                if (recentCommands.Any())
                {
                    Console.WriteLine($"\n   Recent Successful Commands:");
                    foreach (var cmd in recentCommands)
                    {
                        Console.WriteLine($"      - {cmd.Description} (Quality: {cmd.FinalQualityScore}/100, {cmd.ExecutedAt:yyyy-MM-dd HH:mm})");
                    }
                }
                
                DisplaySuccess("Command history statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                DisplayError($"Failed to retrieve command history stats: {ex.Message}");
            }
        }
        
        private async Task DisplayCodebaseStats()
        {
            try
            {
                Console.WriteLine("\n📚 Codebase Statistics:");
                Console.WriteLine("=======================");
                
                var stats = await _codebaseAnalysisService.GetCodebaseStatsAsync();
                
                Console.WriteLine($"   Analyzed Files: {stats.TotalFiles}");
                Console.WriteLine($"   Average Quality Score: {stats.AverageQualityScore}/100");
                Console.WriteLine($"   High Quality Files (80+): {stats.HighQualityFiles}");
                Console.WriteLine($"   Last Analyzed: {stats.LastAnalyzed:yyyy-MM-dd HH:mm:ss}");
                
                DisplaySuccess("Codebase statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                DisplayError($"Failed to retrieve codebase stats: {ex.Message}");
            }
        }
    }
}
