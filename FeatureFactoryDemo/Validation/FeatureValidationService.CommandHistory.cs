using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Services;

namespace FeatureFactoryDemo.Validation
{
    /// <summary>
    /// Command history validation functionality
    /// </summary>
    public partial class FeatureValidationService
    {
        private async Task<CommandHistoryValidationResult> ValidateCommandHistoryAsync()
        {
            Console.WriteLine("\nList Test 3: Command History Operations Validation");
            Console.WriteLine("================================================");
            
            var result = new CommandHistoryValidationResult();
            
            try
            {
                // Test saving a command
                await _commandHistoryService.SaveSuccessfulCommandAsync(
                    "Test Customer Entity Generation",
                    "DotNet",
                    "// Generated test code",
                    95,
                    5,
                    "Test context",
                    "test,validation,entity"
                );
                result.CanSaveCommand = true;
                Console.WriteLine($"   SUCCESS: Save Command: SUCCESS");
                
                // Test retrieving recent commands
                var recentCommands = await _commandHistoryService.GetRecentSuccessfulCommandsAsync(5);
                result.CanRetrieveRecent = recentCommands.Any();
                Console.WriteLine($"   SUCCESS: Retrieve Recent Commands: {(result.CanRetrieveRecent ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"      - Recent Commands Found: {recentCommands.Count}");
                
                // Test similarity matching
                var similarCommands = await _commandHistoryService.GetSimilarCommandsAsync("Create Customer", "DotNet");
                result.CanFindSimilar = similarCommands.Any();
                Console.WriteLine($"   SUCCESS: Find Similar Commands: {(result.CanFindSimilar ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"      - Similar Commands Found: {similarCommands.Count}");
                
                // Test statistics
                var stats = await _commandHistoryService.GetStatisticsAsync();
                result.CanGetStatistics = stats.TotalCommands > 0;
                Console.WriteLine($"   SUCCESS: Get Statistics: {(result.CanGetStatistics ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"      - Total Commands: {stats.TotalCommands}");
                Console.WriteLine($"      - Success Rate: {stats.SuccessRate:F1}%");
                Console.WriteLine($"      - Average Quality: {stats.AverageQualityScore}/100");
                
                result.IsValid = result.CanSaveCommand && result.CanRetrieveRecent && 
                               result.CanFindSimilar && result.CanGetStatistics;
                
                Console.WriteLine($"   Stats Command History Validation: {(result.IsValid ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Command history validation failed");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"   ERROR: Command History Validation: FAILED - {ex.Message}");
            }
            
            return result;
        }
    }
}
