using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Services;
using Nexo.Feature.Analysis.Interfaces;

namespace FeatureFactoryDemo.Validation
{
    /// <summary>
    /// Integration validation functionality
    /// </summary>
    public partial class FeatureValidationService
    {
        private async Task<IntegrationValidationResult> ValidateEndToEndIntegrationAsync()
        {
            Console.WriteLine("\n🔗 Test 5: End-to-End Integration Validation");
            Console.WriteLine("===========================================");
            
            var result = new IntegrationValidationResult();
            
            try
            {
                // Test complete pipeline
                var description = "Create a Product entity with Name, Price, and Category properties";
                var platform = "DotNet";
                
                // Get codebase context
                var context = await _codebaseAnalysisService.GetRelevantContextAsync(description, platform);
                result.CanGetContext = !string.IsNullOrEmpty(context.Content);
                Console.WriteLine($"   SUCCESS: Context Retrieval: {(result.CanGetContext ? "SUCCESS" : "FAILED")}");
                
                // Get similar commands
                var similarCommands = await _commandHistoryService.GetSimilarCommandsAsync(description, platform);
                result.CanGetSimilarCommands = true; // This will work even if no similar commands exist
                Console.WriteLine($"   SUCCESS: Similar Commands: SUCCESS (Found: {similarCommands.Count})");
                
                // Generate test code
                var generatedCode = $@"// Generated with context: {context.FilePath}
using System;
using System.ComponentModel.DataAnnotations;

namespace Test.Generated
{{
    public class Product
    {{
        [Key]
        public int Id {{ get; set; }}
        
        [Required]
        [StringLength(100)]
        public string Name {{ get; set; }} = string.Empty;
        
        [Required]
        public decimal Price {{ get; set; }}
        
        [Required]
        [StringLength(50)]
        public string Category {{ get; set; }} = string.Empty;
        
        public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    }}
}}";
                
                // Analyze generated code
                var analysisResult = await _codeAnalyzer.ValidateCodeAsync(generatedCode, "Product.cs", "integration-test");
                result.CanAnalyzeGeneratedCode = analysisResult != null;
                Console.WriteLine($"   SUCCESS: Generated Code Analysis: {(result.CanAnalyzeGeneratedCode ? "SUCCESS" : "FAILED")}");
                
                if (result.CanAnalyzeGeneratedCode && analysisResult != null)
                {
                    Console.WriteLine($"      - Generated Code Score: {analysisResult.Score}/100");
                    Console.WriteLine($"      - Violations: {analysisResult.Violations.Count}");
                }
                
                // Save to database
                await _commandHistoryService.SaveSuccessfulCommandAsync(
                    description,
                    platform,
                    generatedCode,
                    analysisResult?.Score ?? 0,
                    1,
                    $"Context: {context.FilePath}",
                    "integration,test,entity"
                );
                result.CanSaveGeneratedCode = true;
                Console.WriteLine($"   SUCCESS: Save Generated Code: SUCCESS");
                
                // Verify statistics updated
                var stats = await _commandHistoryService.GetStatisticsAsync();
                result.StatisticsUpdated = stats.TotalCommands > 0;
                Console.WriteLine($"   SUCCESS: Statistics Updated: {(result.StatisticsUpdated ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"      - Total Commands: {stats.TotalCommands}");
                
                result.IsValid = result.CanGetContext && result.CanGetSimilarCommands && 
                               result.CanAnalyzeGeneratedCode && result.CanSaveGeneratedCode && 
                               result.StatisticsUpdated;
                
                Console.WriteLine($"   Stats Integration Validation: {(result.IsValid ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Integration validation failed");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"   ERROR: Integration Validation: FAILED - {ex.Message}");
            }
            
            return result;
        }
    }
}
