using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Validation;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Command to run comprehensive validation of all Feature Factory features
    /// </summary>
    public class ValidateCommand : BaseCommand
    {
        private readonly FeatureValidationService _validationService;
        
        public override string Name => "validate";
        public override string Description => "Run comprehensive validation of all Feature Factory features";
        public override string Usage => "validate [--quick] [--verbose]";
        
        public ValidateCommand(IServiceProvider serviceProvider, ILogger<ValidateCommand> logger) 
            : base(serviceProvider, logger)
        {
            _validationService = serviceProvider.GetRequiredService<FeatureValidationService>();
        }
        
        public override async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                Console.WriteLine("🔍 Feature Factory Validation Command");
                Console.WriteLine("====================================");
                
                // Parse arguments
                bool quickMode = false;
                bool verbose = false;
                
                foreach (var arg in args)
                {
                    switch (arg)
                    {
                        case "--quick":
                            quickMode = true;
                            break;
                        case "--verbose":
                            verbose = true;
                            break;
                        case "--help":
                        case "-h":
                            DisplayHelp();
                            return 0;
                    }
                }
                
                if (quickMode)
                {
                    DisplayInfo("Running quick validation mode...");
                }
                
                if (verbose)
                {
                    DisplayInfo("Verbose output enabled");
                }
                
                // Run comprehensive validation
                var results = await _validationService.RunFullValidationAsync();
                
                // Determine exit code
                var allPassed = results.DatabaseValidation.IsValid &&
                              results.CodebaseValidation.IsValid &&
                              results.CommandHistoryValidation.IsValid &&
                              results.IterativeImprovementValidation.IsValid &&
                              results.IntegrationValidation.IsValid;
                
                if (allPassed)
                {
                    DisplaySuccess("All validations passed! Feature Factory is fully operational!");
                    return 0;
                }
                else
                {
                    DisplayError("Some validations failed. Check the details above.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation command failed");
                DisplayError($"Validation failed: {ex.Message}");
                return 1;
            }
        }
    }
}
