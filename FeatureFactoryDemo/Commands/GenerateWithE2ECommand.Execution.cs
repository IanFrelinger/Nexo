using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Services;
using Nexo.Feature.Analysis.Interfaces;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Command execution functionality for E2E generation command.
    /// </summary>
    public partial class GenerateWithE2ECommand
    {
        public override async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                Console.WriteLine("Running Feature Generation with E2E Testing Command");
                Console.WriteLine("=============================================");

                // Parse arguments
                var (description, platform, targetScore, maxIterations) = ParseArguments(args);
                if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(platform))
                {
                    Console.WriteLine("ERROR: Error: Description and platform are required");
                    Console.WriteLine($"Usage: {Usage}");
                    return 1;
                }

                Console.WriteLine($"INFO:  Description: {description}");
                Console.WriteLine($"INFO:  Platform: {platform}");
                Console.WriteLine($"INFO:  Target Score: {targetScore}/100");
                Console.WriteLine($"INFO:  Max Iterations: {maxIterations}");

                // Get services
                var serviceProvider = GetServiceProvider();
                var codeAnalyzer = serviceProvider.GetRequiredService<ICodingStandardAnalyzer>();
                var commandHistoryService = serviceProvider.GetRequiredService<CommandHistoryService>();
                var codebaseAnalysisService = serviceProvider.GetRequiredService<CodebaseAnalysisService>();

                // Generate the feature
                Console.WriteLine("\nProcessing Feature Factory Pipeline Execution:");
                Console.WriteLine("1. Document Parsing natural language requirements...");
                Console.WriteLine("2. 🧠 AI-powered domain analysis...");
                Console.WriteLine("3. Building  Generating Clean Architecture components...");
                Console.WriteLine("4. Tool Creating CRUD operations...");
                Console.WriteLine("5. SUCCESS: Validating generated code...");
                Console.WriteLine("6. Search Running iterative coding standards analysis...");

                var generationResult = await RunIterativeImprovementAsync(
                    codeAnalyzer, 
                    description, 
                    platform, 
                    targetScore, 
                    maxIterations
                );

                if (!generationResult.IsSuccess)
                {
                    Console.WriteLine("ERROR: Feature generation failed");
                    return 1;
                }

                Console.WriteLine($"\nSUCCESS: Feature generated successfully! Quality: {generationResult.QualityScore}/100");

                // Generate E2E tests
                Console.WriteLine("\nTesting Generating Comprehensive E2E Tests...");
                Console.WriteLine("=====================================");

                var e2eTestResult = await GenerateE2ETestsAsync(platform, description, generationResult.GeneratedCode, generationResult.QualityScore);

                // Display E2E test results
                Console.WriteLine($"\nStats E2E Test Results:");
                Console.WriteLine($"   Total Tests: {e2eTestResult.TotalTests}");
                Console.WriteLine($"   Passed: {e2eTestResult.PassedTests}");
                Console.WriteLine($"   Failed: {e2eTestResult.FailedTests}");
                Console.WriteLine($"   Success Rate: {(e2eTestResult.TotalTests > 0 ? (e2eTestResult.PassedTests * 100.0 / e2eTestResult.TotalTests):0):F1}%");

                // Display test breakdown
                Console.WriteLine($"\nList Test Suite Breakdown:");
                Console.WriteLine($"   Unit Tests: {e2eTestResult.UnitTests}");
                Console.WriteLine($"   Integration Tests: {e2eTestResult.IntegrationTests}");
                Console.WriteLine($"   API Tests: {e2eTestResult.APITests}");
                Console.WriteLine($"   UI Tests: {e2eTestResult.UITests}");
                Console.WriteLine($"   Performance Tests: {e2eTestResult.PerformanceTests}");
                Console.WriteLine($"   Security Tests: {e2eTestResult.SecurityTests}");
                Console.WriteLine($"   Load Tests: {e2eTestResult.LoadTests}");

                // Save to database
                await SaveE2ETestHistoryAsync(serviceProvider, platform, description, generationResult.GeneratedCode, generationResult.QualityScore, e2eTestResult);

                Console.WriteLine($"\nSUCCESS: E2E test generation completed successfully!");
                Console.WriteLine($"Target Overall Success: {(e2eTestResult.Success ? "SUCCESS: PASSED" : "ERROR: FAILED")}");

                return e2eTestResult.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateWithE2ECommand");
                Console.WriteLine($"ERROR: Error: {ex.Message}");
                return 1;
            }
        }
    }
}
