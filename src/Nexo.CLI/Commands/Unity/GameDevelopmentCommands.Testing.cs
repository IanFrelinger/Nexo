using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.AI.Agents;
using Nexo.Feature.Unity.Workflows;
using Nexo.Feature.AI.Models;

namespace Nexo.CLI.Commands.Unity
{
    /// <summary>
    /// Testing functionality
    /// </summary>
    public static partial class GameDevelopmentCommands
    {
        /// <summary>
        /// Creates the test command
        /// </summary>
        private static Command CreateTestCommand(IServiceProvider serviceProvider)
        {
            var testCommand = new Command("test", "Run game testing and quality assurance");
            
            var testTypeOption = new Option<string>(
                "--test-type",
                () => "all",
                "Type of tests to run (unit, performance, gameplay, balance, all)");
            
            var projectPathOption = new Option<string>(
                "--project-path",
                () => ".",
                "Path to the Unity project directory");
            
            var durationOption = new Option<int>(
                "--duration",
                () => 300,
                "Test duration in seconds");
            
            testCommand.AddOption(testTypeOption);
            testCommand.AddOption(projectPathOption);
            testCommand.AddOption(durationOption);
            
            testCommand.SetHandler(async (testType, projectPath, duration) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<GameDevelopmentCommands>>();
                var testingWorkflow = serviceProvider.GetRequiredService<GameTestingWorkflow>();
                
                await RunGameTesting(testingWorkflow, logger, testType, projectPath, duration);
            }, testTypeOption, projectPathOption, durationOption);
            
            return testCommand;
        }

        /// <summary>
        /// Runs game testing
        /// </summary>
        private static async Task RunGameTesting(
            GameTestingWorkflow testingWorkflow,
            ILogger logger,
            string testType,
            string projectPath,
            int duration)
        {
            try
            {
                logger.LogInformation("Running game testing: {TestType} for project: {ProjectPath}", testType, projectPath);
                
                Console.WriteLine($"Testing Running {testType} tests...");
                
                var request = new GameTestingWorkflowRequest
                {
                    ProjectPath = projectPath,
                    RunGameplayTests = testType == "gameplay" || testType == "all",
                    TestBalance = testType == "balance" || testType == "all",
                    TestDuration = TimeSpan.FromSeconds(duration)
                };
                
                var result = await testingWorkflow.ExecuteAsync(request);
                
                if (result.Status == WorkflowStatus.Completed)
                {
                    Console.WriteLine($"SUCCESS: Testing completed successfully!");
                    
                    if (result.FinalReport is GameTestReport report)
                    {
                        Console.WriteLine($"\nList Test Report Summary:");
                        Console.WriteLine($"  Total Tests: {report.QualityMetrics.TotalTests}");
                        Console.WriteLine($"  Passed: {report.QualityMetrics.PassedTests}");
                        Console.WriteLine($"  Failed: {report.QualityMetrics.FailedTests}");
                        Console.WriteLine($"  Pass Rate: {report.QualityMetrics.OverallTestPassRate:P}");
                        Console.WriteLine($"  Quality Score: {report.QualityMetrics.QualityScore:F2}/10");
                        
                        if (report.QualityMetrics.AverageFrameRate > 0)
                        {
                            Console.WriteLine($"  Average FPS: {report.QualityMetrics.AverageFrameRate:F1}");
                        }
                        
                        if (report.Recommendations.Any())
                        {
                            Console.WriteLine($"\nIdea Recommendations:");
                            foreach (var recommendation in report.Recommendations)
                            {
                                Console.WriteLine($"  • {recommendation}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: Testing failed: {result.ErrorMessage}");
                }
                
                logger.LogInformation("Game testing completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run game testing");
                Console.WriteLine($"ERROR: Testing failed: {ex.Message}");
            }
        }
    }
}
