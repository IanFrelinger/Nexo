using Microsoft.Extensions.Logging;

namespace FeatureFactoryDemo.Validation
{
    /// <summary>
    /// Reporting functionality
    /// </summary>
    public partial class FeatureValidationService
    {
        private void GenerateValidationReport(ValidationResults results)
        {
            Console.WriteLine("\nStats VALIDATION REPORT");
            Console.WriteLine("====================");
            
            var totalTests = 5;
            var passedTests = 0;
            
            if (results.DatabaseValidation.IsValid) passedTests++;
            if (results.CodebaseValidation.IsValid) passedTests++;
            if (results.CommandHistoryValidation.IsValid) passedTests++;
            if (results.IterativeImprovementValidation.IsValid) passedTests++;
            if (results.IntegrationValidation.IsValid) passedTests++;
            
            Console.WriteLine($"List Test Results: {passedTests}/{totalTests} tests passed");
            Console.WriteLine($"Stats Success Rate: {(double)passedTests / totalTests * 100:F1}%");
            
            Console.WriteLine("\nDocument Detailed Results:");
            Console.WriteLine($"   SUCCESS: Database Operations: {(results.DatabaseValidation.IsValid ? "PASSED" : "FAILED")}");
            Console.WriteLine($"   SUCCESS: Codebase Context: {(results.CodebaseValidation.IsValid ? "PASSED" : "FAILED")}");
            Console.WriteLine($"   SUCCESS: Command History: {(results.CommandHistoryValidation.IsValid ? "PASSED" : "FAILED")}");
            Console.WriteLine($"   SUCCESS: Iterative Improvement: {(results.IterativeImprovementValidation.IsValid ? "PASSED" : "FAILED")}");
            Console.WriteLine($"   SUCCESS: End-to-End Integration: {(results.IntegrationValidation.IsValid ? "PASSED" : "FAILED")}");
            
            if (passedTests == totalTests)
            {
                Console.WriteLine("\nSUCCESS ALL VALIDATIONS PASSED! Feature Factory is fully operational!");
            }
            else
            {
                Console.WriteLine($"\nWARNING:  {totalTests - passedTests} validation(s) failed. Check the details above.");
            }
        }
    }
}
