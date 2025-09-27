using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Services;
using Nexo.Feature.Analysis.Interfaces;

namespace FeatureFactoryDemo.Validation
{
    /// <summary>
    /// Codebase validation functionality
    /// </summary>
    public partial class FeatureValidationService
    {
        private async Task<CodebaseValidationResult> ValidateCodebaseContextAsync()
        {
            Console.WriteLine("\nDocumentation Test 2: Codebase Context Analysis Validation");
            Console.WriteLine("===============================================");
            
            var result = new CodebaseValidationResult();
            
            try
            {
                // Test codebase analysis
                var stats = await _codebaseAnalysisService.GetCodebaseStatsAsync();
                result.FilesAnalyzed = stats.TotalFiles;
                result.AverageQuality = stats.AverageQualityScore;
                result.HighQualityFiles = stats.HighQualityFiles;
                
                Console.WriteLine($"   SUCCESS: Codebase Analysis: SUCCESS");
                Console.WriteLine($"      - Files Analyzed: {stats.TotalFiles}");
                Console.WriteLine($"      - Average Quality: {stats.AverageQualityScore}/100");
                Console.WriteLine($"      - High Quality Files (80+): {stats.HighQualityFiles}");
                
                // Test context retrieval
                var context = await _codebaseAnalysisService.GetRelevantContextAsync("Create a Customer entity", "DotNet");
                result.CanRetrieveContext = !string.IsNullOrEmpty(context.Content);
                Console.WriteLine($"   SUCCESS: Context Retrieval: {(result.CanRetrieveContext ? "SUCCESS" : "FAILED")}");
                
                if (result.CanRetrieveContext)
                {
                    Console.WriteLine($"      - Context File: {context.FilePath}");
                    Console.WriteLine($"      - Context Quality: {context.QualityScore}/100");
                    Console.WriteLine($"      - Context Patterns: {context.Patterns}");
                }
                
                // Test code analysis integration
                if (result.CanRetrieveContext && !string.IsNullOrEmpty(context.Content))
                {
                    var analysisResult = await _codeAnalyzer.ValidateCodeAsync(context.Content, context.FilePath, "validation-test");
                    result.CodeAnalysisWorks = analysisResult != null;
                    Console.WriteLine($"   SUCCESS: Code Analysis Integration: {(result.CodeAnalysisWorks ? "SUCCESS" : "FAILED")}");
                    
                    if (result.CodeAnalysisWorks && analysisResult != null)
                    {
                        Console.WriteLine($"      - Analysis Score: {analysisResult.Score}/100");
                        Console.WriteLine($"      - Violations: {analysisResult.Violations.Count}");
                    }
                }
                
                result.IsValid = result.FilesAnalyzed > 0 && result.CanRetrieveContext && result.CodeAnalysisWorks;
                
                Console.WriteLine($"   Stats Codebase Validation: {(result.IsValid ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Codebase validation failed");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"   ERROR: Codebase Validation: FAILED - {ex.Message}");
            }
            
            return result;
        }
    }
}
