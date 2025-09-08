using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Services;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Command to analyze the codebase and store context information
    /// </summary>
    public class AnalyzeCommand : BaseCommand
    {
        private readonly CodebaseAnalysisService _codebaseAnalysisService;
        
        public override string Name => "analyze";
        public override string Description => "Analyze the codebase and store context information for code generation";
        public override string Usage => "analyze [--path <codebase-path>] [--limit <file-limit>]";
        
        public AnalyzeCommand(IServiceProvider serviceProvider, ILogger<AnalyzeCommand> logger) 
            : base(serviceProvider, logger)
        {
            _codebaseAnalysisService = serviceProvider.GetRequiredService<CodebaseAnalysisService>();
        }
        
        public override async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                Console.WriteLine("🔍 Codebase Analysis Command");
                Console.WriteLine("============================");
                
                // Parse arguments
                string codebasePath = "../src";
                int fileLimit = 20;
                
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--path" when i + 1 < args.Length:
                            codebasePath = args[++i];
                            break;
                        case "--limit" when i + 1 < args.Length:
                            if (int.TryParse(args[++i], out int limit))
                                fileLimit = limit;
                            break;
                        case "--help":
                        case "-h":
                            DisplayHelp();
                            return 0;
                    }
                }
                
                DisplayInfo($"Analyzing codebase at: {codebasePath}");
                DisplayInfo($"File limit: {fileLimit}");
                
                // Analyze codebase
                await _codebaseAnalysisService.AnalyzeCodebaseAsync(codebasePath);
                
                // Get and display statistics
                var stats = await _codebaseAnalysisService.GetCodebaseStatsAsync();
                
                Console.WriteLine("\n📊 Analysis Results:");
                Console.WriteLine($"   Files Analyzed: {stats.TotalFiles}");
                Console.WriteLine($"   Average Quality Score: {stats.AverageQualityScore}/100");
                Console.WriteLine($"   High Quality Files (80+): {stats.HighQualityFiles}");
                Console.WriteLine($"   Last Analyzed: {stats.LastAnalyzed:yyyy-MM-dd HH:mm:ss}");
                
                DisplaySuccess("Codebase analysis completed successfully!");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Codebase analysis failed");
                DisplayError($"Analysis failed: {ex.Message}");
                return 1;
            }
        }
    }
}
