using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Services;
using Nexo.Feature.Analysis.Interfaces;

namespace FeatureFactoryDemo.Validation
{
    /// <summary>
    /// Iterative improvement validation functionality
    /// </summary>
    public partial class FeatureValidationService
    {
        private async Task<IterativeImprovementValidationResult> ValidateIterativeImprovementAsync()
        {
            Console.WriteLine("\nProcessing Test 4: Iterative Improvement with Database Integration");
            Console.WriteLine("=========================================================");
            
            var result = new IterativeImprovementValidationResult();
            
            try
            {
                // Test iterative improvement process
                var testCode = @"public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}";
                
                var analysisResult = await _codeAnalyzer.ValidateCodeAsync(testCode, "TestEntity.cs", "validation-test");
                result.CanAnalyzeCode = analysisResult != null;
                Console.WriteLine($"   SUCCESS: Code Analysis: {(result.CanAnalyzeCode ? "SUCCESS" : "FAILED")}");
                
                if (result.CanAnalyzeCode && analysisResult != null)
                {
                    Console.WriteLine($"      - Initial Score: {analysisResult.Score}/100");
                    Console.WriteLine($"      - Violations: {analysisResult.Violations.Count}");
                    
                    // Test improvement simulation
                    var improvedCode = testCode.Replace("public string Name { get; set; }", 
                        "public string Name { get; set; } = string.Empty;");
                    
                    var improvedAnalysis = await _codeAnalyzer.ValidateCodeAsync(improvedCode, "TestEntity.cs", "validation-test");
                    result.CanImproveCode = improvedAnalysis != null;
                    Console.WriteLine($"   SUCCESS: Code Improvement: {(result.CanImproveCode ? "SUCCESS" : "FAILED")}");
                    
                    if (result.CanImproveCode && improvedAnalysis != null)
                    {
                        Console.WriteLine($"      - Improved Score: {improvedAnalysis.Score}/100");
                        Console.WriteLine($"      - Violations: {improvedAnalysis.Violations.Count}");
                        result.QualityImproved = improvedAnalysis.Score >= analysisResult.Score;
                    }
                }
                
                // Test database integration
                await _commandHistoryService.SaveSuccessfulCommandAsync(
                    "Test Iterative Improvement",
                    "DotNet",
                    testCode,
                    analysisResult?.Score ?? 0,
                    1,
                    "Test iterative improvement validation",
                    "test,validation,iterative"
                );
                result.CanSaveToDatabase = true;
                Console.WriteLine($"   SUCCESS: Database Integration: SUCCESS");
                
                result.IsValid = result.CanAnalyzeCode && result.CanImproveCode && 
                               result.QualityImproved && result.CanSaveToDatabase;
                
                Console.WriteLine($"   Stats Iterative Improvement Validation: {(result.IsValid ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Iterative improvement validation failed");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"   ERROR: Iterative Improvement Validation: FAILED - {ex.Message}");
            }
            
            return result;
        }
    }
}
