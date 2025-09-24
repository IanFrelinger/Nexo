using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Services;

namespace FeatureFactoryDemo.Services
{
    public class FeatureFactoryDemoService
    {
        private readonly ILogger<FeatureFactoryDemoService> _logger;
        private readonly FeatureGenerationService _featureGenerationService;
        private readonly CodebaseAnalysisService _codebaseAnalysisService;
        private readonly CommandHistoryService _commandHistoryService;
        private readonly ValidationService _validationService;
        private readonly DemoDataService _demoDataService;

        public FeatureFactoryDemoService(
            ILogger<FeatureFactoryDemoService> logger,
            FeatureGenerationService featureGenerationService,
            CodebaseAnalysisService codebaseAnalysisService,
            CommandHistoryService commandHistoryService,
            ValidationService validationService,
            DemoDataService demoDataService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _featureGenerationService = featureGenerationService ?? throw new ArgumentNullException(nameof(featureGenerationService));
            _codebaseAnalysisService = codebaseAnalysisService ?? throw new ArgumentNullException(nameof(codebaseAnalysisService));
            _commandHistoryService = commandHistoryService ?? throw new ArgumentNullException(nameof(commandHistoryService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _demoDataService = demoDataService ?? throw new ArgumentNullException(nameof(demoDataService));
        }

        public async Task RunDemoAsync()
        {
            try
            {
                _logger.LogInformation("Starting Feature Factory Demo");

                // Initialize demo data
                await _demoDataService.InitializeDemoDataAsync();

                // Run feature generation demos
                await RunFeatureGenerationDemos();

                // Run codebase analysis demos
                await RunCodebaseAnalysisDemos();

                // Run validation demos
                await RunValidationDemos();

                _logger.LogInformation("Feature Factory Demo completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running Feature Factory Demo");
                throw;
            }
        }

        private async Task RunFeatureGenerationDemos()
        {
            Console.WriteLine("\n=== Feature Generation Demos ===");

            // Demo 1: Simple feature generation
            await _featureGenerationService.DemoSimpleFeatureGeneration();

            // Demo 2: Complex feature generation with iterations
            await _featureGenerationService.DemoComplexFeatureGeneration();

            // Demo 3: Platform-specific generation
            await _featureGenerationService.DemoPlatformSpecificGeneration();
        }

        private async Task RunCodebaseAnalysisDemos()
        {
            Console.WriteLine("\n=== Codebase Analysis Demos ===");

            // Demo 1: Codebase context analysis
            await _codebaseAnalysisService.DemoCodebaseContextAnalysis();

            // Demo 2: Similar command analysis
            await _codebaseAnalysisService.DemoSimilarCommandAnalysis();

            // Demo 3: Codebase pattern analysis
            await _codebaseAnalysisService.DemoCodebasePatternAnalysis();
        }

        private async Task RunValidationDemos()
        {
            Console.WriteLine("\n=== Validation Demos ===");

            // Demo 1: Coding standards validation
            await _validationService.DemoCodingStandardsValidation();

            // Demo 2: Quality score calculation
            await _validationService.DemoQualityScoreCalculation();

            // Demo 3: Iteration improvement
            await _validationService.DemoIterationImprovement();
        }
    }
}
