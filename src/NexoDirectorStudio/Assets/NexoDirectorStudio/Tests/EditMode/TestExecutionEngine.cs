using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validation;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Engine for executing different types of tests.
    /// </summary>
    public class TestExecutionEngine
    {
        /// <summary>
        /// Runs E2E tests with the given configuration.
        /// </summary>
        public static async Task<TestExecutionResult> RunE2ETestsAsync(TestSuiteConfig config)
        {
            var result = new TestExecutionResult
            {
                Category = "E2E",
                StartTime = DateTimeOffset.UtcNow
            };

            try
            {
                using var service = new DirectorStudioServiceUnified();
                var testRunner = new TestRunnerIntegration(config.MaxRetries, config.TimeoutSeconds);

                var briefs = new[]
                {
                    new DesignBrief("E2E FPS test", "FPS", 2, 3, 1001),
                    new DesignBrief("E2E Platformer test", "Platformer", 2, 2, 1002),
                    new DesignBrief("E2E RPG test", "RPG", 2, 3, 1003)
                };

                var successCount = 0;
                foreach (var brief in briefs)
                {
                    try
                    {
                        var plan = await service.GetService<IPlanGameSliceCommand>()
                            .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                        var world = await service.GetService<IBuildWorldLayoutCommand>()
                            .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                        var interactions = await service.GetService<IPlaceInteractionsCommand>()
                            .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                        var content = await service.GetService<ICreateContentBundleCommand>()
                            .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                        var validationResult = await testRunner.RunValidationTestsAsync(content, plan, CancellationToken.None);

                        if (validationResult.OverallPassed && validationResult.OverallScore >= config.MinValidationScore)
                        {
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"E2E test failed for {brief.GenreHint}: {ex.Message}");
                    }
                }

                result.Success = successCount == briefs.Length;
                result.TestsRun = briefs.Length;
                result.TestsPassed = successCount;
                result.TestsFailed = briefs.Length - successCount;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            return result;
        }

        /// <summary>
        /// Runs integration tests with the given configuration.
        /// </summary>
        public static async Task<TestExecutionResult> RunIntegrationTestsAsync(TestSuiteConfig config)
        {
            var result = new TestExecutionResult
            {
                Category = "Integration",
                StartTime = DateTimeOffset.UtcNow
            };

            try
            {
                using var service = new DirectorStudioServiceUnified();

                // Test service resolution
                var services = new object[]
                {
                    service.GetService<IPlanGameSliceCommand>(),
                    service.GetService<IBuildWorldLayoutCommand>(),
                    service.GetService<IPlaceInteractionsCommand>(),
                    service.GetService<ICreateContentBundleCommand>(),
                    service.GetService<IOllamaAdapter>(),
                    service.GetService<ITextureGenAdapter>(),
                    service.GetService<ITtsAdapter>()
                };

                var nullServices = services.Count(s => s == null);
                result.Success = nullServices == 0;
                result.TestsRun = services.Length;
                result.TestsPassed = services.Length - nullServices;
                result.TestsFailed = nullServices;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            return result;
        }

        /// <summary>
        /// Runs performance tests with the given configuration.
        /// </summary>
        public static async Task<TestExecutionResult> RunPerformanceTestsAsync(TestSuiteConfig config)
        {
            var result = new TestExecutionResult
            {
                Category = "Performance",
                StartTime = DateTimeOffset.UtcNow
            };

            if (!config.EnablePerformanceTests)
            {
                result.Success = true;
                result.TestsRun = 0;
                result.TestsPassed = 0;
                result.TestsFailed = 0;
                result.EndTime = DateTimeOffset.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                return result;
            }

            try
            {
                using var service = new DirectorStudioServiceUnified();
                var brief = new DesignBrief("Performance test", "FPS", 1, 2, 2001);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var plan = await service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                var world = await service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                var interactions = await service.GetService<IPlaceInteractionsCommand>()
                    .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                var content = await service.GetService<ICreateContentBundleCommand>()
                    .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                stopwatch.Stop();

                result.Success = stopwatch.ElapsedMilliseconds < config.TimeoutSeconds * 1000;
                result.TestsRun = 1;
                result.TestsPassed = result.Success ? 1 : 0;
                result.TestsFailed = result.Success ? 0 : 1;
                result.PerformanceMetrics = new Dictionary<string, object>
                {
                    ["DurationMs"] = stopwatch.ElapsedMilliseconds,
                    ["TimeoutMs"] = config.TimeoutSeconds * 1000
                };
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            return result;
        }

        /// <summary>
        /// Runs CI/CD tests with the given configuration.
        /// </summary>
        public static async Task<TestExecutionResult> RunCICDTestsAsync(TestSuiteConfig config)
        {
            var result = new TestExecutionResult
            {
                Category = "CICD",
                StartTime = DateTimeOffset.UtcNow
            };

            try
            {
                using var service = new DirectorStudioServiceUnified();

                // Quick smoke test
                var brief = new DesignBrief("CI/CD smoke test", "FPS", 1, 1, 3001);
                var plan = await service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                result.Success = plan != null;
                result.TestsRun = 1;
                result.TestsPassed = result.Success ? 1 : 0;
                result.TestsFailed = result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            return result;
        }

        /// <summary>
        /// Runs component validation tests with the given configuration.
        /// </summary>
        public static async Task<TestExecutionResult> RunComponentValidationTestsAsync(TestSuiteConfig config)
        {
            var result = new TestExecutionResult
            {
                Category = "ComponentValidation",
                StartTime = DateTimeOffset.UtcNow
            };

            try
            {
                using var service = new DirectorStudioServiceUnified();
                var testRunner = new TestRunnerIntegration(config.MaxRetries, config.TimeoutSeconds);

                var brief = new DesignBrief("Component validation test", "FPS", 1, 2, 4001);
                var plan = await service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                var world = await service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                var interactions = await service.GetService<IPlaceInteractionsCommand>()
                    .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                var content = await service.GetService<ICreateContentBundleCommand>()
                    .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                var validationResult = await testRunner.RunValidationTestsAsync(content, plan, CancellationToken.None);

                result.Success = validationResult.OverallPassed && validationResult.OverallScore >= config.MinValidationScore;
                result.TestsRun = validationResult.TotalTests;
                result.TestsPassed = validationResult.PassedTests;
                result.TestsFailed = validationResult.FailedTests;
                result.PerformanceMetrics = new Dictionary<string, object>
                {
                    ["ValidationScore"] = validationResult.OverallScore,
                    ["MinRequiredScore"] = config.MinValidationScore
                };
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            return result;
        }
    }
}
