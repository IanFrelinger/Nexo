using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Extensions;
using Nexo.Core.Application.Services.AI.Monitoring;
using Nexo.Core.Application.Services.AI.Pipeline;
using Nexo.Core.Application.Services.AI.Runtime;
using Nexo.Core.Application.Services.AI.Safety;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Demo
{
    /// <summary>
    /// Phase 3 AI Integration Demo - Showcasing Advanced Pipeline Integration, Safety, and Monitoring
    /// </summary>
    public class AI_Demo_Phase3
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Running AI Integration Phase 3 Demo - Advanced Pipeline Integration");
            Console.WriteLine("===============================================================");
            Console.WriteLine();

            // Create host builder
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Add AI services
                    services.AddAIServices();
                    
                    // Add HTTP client for model downloads
                    services.AddHttpClient();
                })
                .Build();

            // Get services
            var logger = host.Services.GetRequiredService<ILogger<AI_Demo_Phase3>>();
            var runtimeSelector = host.Services.GetRequiredService<IAIRuntimeSelector>();
            var safetyValidator = host.Services.GetRequiredService<AISafetyValidator>();
            var usageMonitor = host.Services.GetRequiredService<AIUsageMonitor>();

            try
            {
                // Demonstrate advanced AI pipeline steps
                await DemonstrateAdvancedPipelineStepsAsync(logger, runtimeSelector);

                // Demonstrate safety validation
                await DemonstrateSafetyValidationAsync(logger, safetyValidator);

                // Demonstrate usage monitoring
                await DemonstrateUsageMonitoringAsync(logger, usageMonitor);

                // Demonstrate comprehensive AI workflow
                await DemonstrateComprehensiveWorkflowAsync(logger, runtimeSelector, safetyValidator, usageMonitor);

                Console.WriteLine();
                Console.WriteLine("Target AI Integration Phase 3 Demo Complete!");
                Console.WriteLine("SUCCESS: Advanced Pipeline Steps: Ready");
                Console.WriteLine("SUCCESS: Safety Validation: Ready");
                Console.WriteLine("SUCCESS: Usage Monitoring: Ready");
                Console.WriteLine("SUCCESS: Comprehensive Workflow: Ready");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo failed with error");
                Console.WriteLine($"ERROR: Demo failed: {ex.Message}");
            }
            finally
            {
                await host.StopAsync();
            }
        }

        private static async Task DemonstrateAdvancedPipelineStepsAsync(ILogger logger, IAIRuntimeSelector runtimeSelector)
        {
            Console.WriteLine("Tool Advanced AI Pipeline Steps Demo");
            Console.WriteLine("==================================");

            try
            {
                // Demonstrate Code Review Step
                await DemonstrateCodeReviewStepAsync(logger, runtimeSelector);

                // Demonstrate Optimization Step
                await DemonstrateOptimizationStepAsync(logger, runtimeSelector);

                // Demonstrate Documentation Step
                await DemonstrateDocumentationStepAsync(logger, runtimeSelector);

                // Demonstrate Testing Step
                await DemonstrateTestingStepAsync(logger, runtimeSelector);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Advanced pipeline steps demo failed");
                Console.WriteLine($"ERROR: Advanced pipeline steps demo failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateCodeReviewStepAsync(ILogger logger, IAIRuntimeSelector runtimeSelector)
        {
            Console.WriteLine("Document Code Review Step Demo");
            Console.WriteLine("------------------------");

            try
            {
                var codeReviewStep = new AICodeReviewStep(runtimeSelector, logger);
                
                var request = new CodeReviewRequest
                {
                    Code = @"
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
    
    public int Divide(int a, int b)
    {
        return a / b; // Potential division by zero
    }
}",
                    Language = CodeLanguage.CSharp,
                    Context = "Simple calculator implementation"
                };

                var context = new PipelineContext
                {
                    EnvironmentProfile = new EnvironmentProfile
                    {
                        CurrentPlatform = PlatformType.Windows
                    }
                };

                Console.WriteLine("  Search Reviewing C# calculator code...");
                var result = await codeReviewStep.ExecuteAsync(request, context);

                if (result.ReviewCompleted && result.Result != null)
                {
                    Console.WriteLine($"  SUCCESS: Code review completed with quality score: {result.Result.QualityScore}");
                    Console.WriteLine($"  Stats Issues found: {result.Result.Issues.Count}");
                    Console.WriteLine($"  Idea Suggestions: {result.Result.Suggestions.Count}");
                    
                    foreach (var issue in result.Result.Issues.Take(3))
                    {
                        Console.WriteLine($"    • {issue.Severity}: {issue.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Code review step demo failed");
                Console.WriteLine($"    ERROR: Code review failed: {ex.Message}");
            }
        }

        private static async Task DemonstrateOptimizationStepAsync(ILogger logger, IAIRuntimeSelector runtimeSelector)
        {
            Console.WriteLine("⚡ Optimization Step Demo");
            Console.WriteLine("-------------------------");

            try
            {
                var optimizationStep = new AIOptimizationStep(runtimeSelector, logger);
                
                var request = new CodeOptimizationRequest
                {
                    Code = @"
public class DataProcessor
{
    public string ProcessData(List<string> items)
    {
        string result = "";
        for (int i = 0; i < items.Count; i++)
        {
            result += items[i] + ",";
        }
        return result;
    }
}",
                    Language = CodeLanguage.CSharp,
                    OptimizationType = OptimizationType.Performance,
                    Context = "Data processing optimization"
                };

                var context = new PipelineContext
                {
                    EnvironmentProfile = new EnvironmentProfile
                    {
                        CurrentPlatform = PlatformType.Windows
                    }
                };

                Console.WriteLine("  Tool Optimizing C# data processor...");
                var result = await optimizationStep.ExecuteAsync(request, context);

                if (result.OptimizationCompleted && result.Result != null)
                {
                    Console.WriteLine($"  SUCCESS: Optimization completed with score: {result.Result.OptimizationScore}");
                    Console.WriteLine($"  Progress Performance gain: {result.Result.PerformanceGain:F1}%");
                    Console.WriteLine($"  Commands:  Improvements: {result.Result.Improvements.Count}");
                    
                    foreach (var improvement in result.Result.Improvements.Take(3))
                    {
                        Console.WriteLine($"    • {improvement}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Optimization step demo failed");
                Console.WriteLine($"    ERROR: Optimization failed: {ex.Message}");
            }
        }

        private static async Task DemonstrateDocumentationStepAsync(ILogger logger, IAIRuntimeSelector runtimeSelector)
        {
            Console.WriteLine("Documentation Documentation Step Demo");
            Console.WriteLine("--------------------------");

            try
            {
                var documentationStep = new AIDocumentationStep(runtimeSelector, logger);
                
                var request = new DocumentationRequest
                {
                    Code = @"
public class UserService
{
    public async Task<User> GetUserAsync(int userId)
    {
        // Implementation here
        return new User { Id = userId };
    }
}",
                    Language = CodeLanguage.CSharp,
                    DocumentationType = DocumentationType.API,
                    Context = "User service API documentation"
                };

                var context = new PipelineContext
                {
                    EnvironmentProfile = new EnvironmentProfile
                    {
                        CurrentPlatform = PlatformType.WebAssembly
                    }
                };

                Console.WriteLine("  📖 Generating API documentation...");
                var result = await documentationStep.ExecuteAsync(request, context);

                if (result.DocumentationCompleted && result.Result != null)
                {
                    Console.WriteLine($"  SUCCESS: Documentation generated with quality score: {result.Result.QualityScore}");
                    Console.WriteLine($"  Stats Coverage: {result.Result.Coverage}%");
                    Console.WriteLine($"  Document Documentation length: {result.Result.GeneratedDocumentation.Length} characters");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Documentation step demo failed");
                Console.WriteLine($"    ERROR: Documentation generation failed: {ex.Message}");
            }
        }

        private static async Task DemonstrateTestingStepAsync(ILogger logger, IAIRuntimeSelector runtimeSelector)
        {
            Console.WriteLine("Testing Testing Step Demo");
            Console.WriteLine("--------------------");

            try
            {
                var testingStep = new AITestingStep(runtimeSelector, logger);
                
                var request = new TestingRequest
                {
                    Code = @"
public class MathUtils
{
    public static int Factorial(int n)
    {
        if (n <= 1) return 1;
        return n * Factorial(n - 1);
    }
}",
                    Language = CodeLanguage.CSharp,
                    TestType = TestType.Unit,
                    Context = "Math utilities unit testing"
                };

                var context = new PipelineContext
                {
                    EnvironmentProfile = new EnvironmentProfile
                    {
                        CurrentPlatform = PlatformType.Linux
                    }
                };

                Console.WriteLine("  Testing Generating unit tests...");
                var result = await testingStep.ExecuteAsync(request, context);

                if (result.TestGenerationCompleted && result.Result != null)
                {
                    Console.WriteLine($"  SUCCESS: Test generation completed with quality score: {result.Result.QualityScore}");
                    Console.WriteLine($"  Stats Coverage: {result.Result.Coverage}%");
                    Console.WriteLine($"  Document Test code length: {result.Result.GeneratedTests.Length} characters");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Testing step demo failed");
                Console.WriteLine($"    ERROR: Test generation failed: {ex.Message}");
            }
        }

        private static async Task DemonstrateSafetyValidationAsync(ILogger logger, AISafetyValidator safetyValidator)
        {
            Console.WriteLine("Safety  Safety Validation Demo");
            Console.WriteLine("==========================");

            try
            {
                // Test safe content
                var safeContent = @"
public class SafeClass
{
    public string ProcessData(string input)
    {
        return input.ToUpper();
    }
}";

                Console.WriteLine("  SUCCESS: Validating safe content...");
                var safeResult = await safetyValidator.ValidateContentAsync(safeContent, SafetyLevel.High);
                Console.WriteLine($"    Safe content validation: {(safeResult.IsValid ? "SUCCESS: Valid" : "ERROR: Invalid")}");
                Console.WriteLine($"    Issues found: {safeResult.Issues.Count}");

                // Test potentially unsafe content
                var unsafeContent = @"
public class UnsafeClass
{
    public void ExecuteCommand(string cmd)
    {
        System.Diagnostics.Process.Start(cmd);
    }
}";

                Console.WriteLine("  WARNING:  Validating potentially unsafe content...");
                var unsafeResult = await safetyValidator.ValidateContentAsync(unsafeContent, SafetyLevel.High);
                Console.WriteLine($"    Unsafe content validation: {(unsafeResult.IsValid ? "SUCCESS: Valid" : "ERROR: Invalid")}");
                Console.WriteLine($"    Issues found: {unsafeResult.Issues.Count}");
                
                foreach (var issue in unsafeResult.Issues.Take(2))
                {
                    Console.WriteLine($"      • {issue.Severity}: {issue.Message}");
                }

                // Test operation context validation
                var operationContext = new AIOperationContext
                {
                    OperationType = AIOperationType.CodeGeneration,
                    TargetPlatform = PlatformType.WebAssembly,
                    MaxTokens = 2048,
                    Temperature = 0.7,
                    Priority = AIPriority.Quality
                };

                Console.WriteLine("  Search Validating operation context...");
                var contextResult = await safetyValidator.ValidateOperationContextAsync(operationContext);
                Console.WriteLine($"    Context validation: {(contextResult.IsValid ? "SUCCESS: Valid" : "ERROR: Invalid")}");
                Console.WriteLine($"    Issues found: {contextResult.Issues.Count}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Safety validation demo failed");
                Console.WriteLine($"ERROR: Safety validation demo failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateUsageMonitoringAsync(ILogger logger, AIUsageMonitor usageMonitor)
        {
            Console.WriteLine("Stats Usage Monitoring Demo");
            Console.WriteLine("========================");

            try
            {
                // Simulate some AI operations
                var operationIds = new List<string>();
                
                for (int i = 0; i < 5; i++)
                {
                    var operationId = Guid.NewGuid().ToString();
                    operationIds.Add(operationId);
                    
                    var context = new AIOperationContext
                    {
                        OperationType = AIOperationType.CodeGeneration,
                        TargetPlatform = PlatformType.Windows,
                        MaxTokens = 1024,
                        Temperature = 0.7,
                        Priority = AIPriority.Balanced
                    };

                    // Start operation
                    await usageMonitor.StartOperationAsync(operationId, context, $"user_{i}");
                    
                    // Simulate operation progress
                    await Task.Delay(100);
                    await usageMonitor.UpdateOperationAsync(operationId, AIOperationStatus.Running, 
                        new Dictionary<string, object> { ["Progress"] = 50 });
                    
                    // Complete operation
                    await Task.Delay(100);
                    var success = i < 4; // 80% success rate
                    await usageMonitor.CompleteOperationAsync(operationId, success, 
                        success ? null : "Simulated error", 
                        new Dictionary<string, object> { ["GeneratedCode"] = "Mock code" });
                }

                // Get usage statistics
                Console.WriteLine("  Progress Generating usage statistics...");
                var statistics = await usageMonitor.GetUsageStatisticsAsync();
                Console.WriteLine($"    Total operations: {statistics.TotalOperations}");
                Console.WriteLine($"    Success rate: {statistics.SuccessRate:F1}%");
                Console.WriteLine($"    Average duration: {statistics.AverageOperationDuration.TotalMilliseconds:F0}ms");

                // Get active operations
                var activeOperations = await usageMonitor.GetActiveOperationsAsync();
                Console.WriteLine($"    Active operations: {activeOperations.Count}");

                // Generate analytics
                Console.WriteLine("  Search Generating usage analytics...");
                var analytics = await usageMonitor.GenerateAnalyticsAsync();
                Console.WriteLine($"    Usage trends: {analytics.UsageTrends.Count}");
                Console.WriteLine($"    Performance insights: {analytics.PerformanceInsights.Count}");
                Console.WriteLine($"    Recommendations: {analytics.Recommendations.Count}");
                
                foreach (var recommendation in analytics.Recommendations.Take(2))
                {
                    Console.WriteLine($"      • {recommendation}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Usage monitoring demo failed");
                Console.WriteLine($"ERROR: Usage monitoring demo failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateComprehensiveWorkflowAsync(ILogger logger, IAIRuntimeSelector runtimeSelector, AISafetyValidator safetyValidator, AIUsageMonitor usageMonitor)
        {
            Console.WriteLine("Processing Comprehensive AI Workflow Demo");
            Console.WriteLine("=================================");

            try
            {
                var operationId = Guid.NewGuid().ToString();
                var userId = "demo_user";

                // Create operation context
                var context = new AIOperationContext
                {
                    OperationType = AIOperationType.CodeGeneration,
                    TargetPlatform = PlatformType.WebAssembly,
                    MaxTokens = 2048,
                    Temperature = 0.7,
                    Priority = AIPriority.Quality,
                    Requirements = new AIRequirements
                    {
                        QualityThreshold = 85,
                        SafetyLevel = SafetyLevel.High,
                        PerformanceTarget = PerformanceTarget.Balanced
                    }
                };

                // Start monitoring
                Console.WriteLine("  Running Starting comprehensive AI workflow...");
                await usageMonitor.StartOperationAsync(operationId, context, userId);

                // Validate operation context
                Console.WriteLine("  Safety  Validating operation context...");
                var safetyResult = await safetyValidator.ValidateOperationContextAsync(context);
                if (!safetyResult.IsValid)
                {
                    Console.WriteLine($"    ERROR: Safety validation failed: {safetyResult.Issues.Count} issues");
                    return;
                }
                Console.WriteLine("    SUCCESS: Safety validation passed");

                // Simulate AI operation
                Console.WriteLine("  AI Executing AI operation...");
                await usageMonitor.UpdateOperationAsync(operationId, AIOperationStatus.Running, 
                    new Dictionary<string, object> { ["Progress"] = 25 });

                // Simulate code generation
                var generatedCode = @"
public class GeneratedClass
{
    public string ProcessData(string input)
    {
        return input.ToUpper();
    }
}";

                // Validate generated content
                Console.WriteLine("  Search Validating generated content...");
                var contentSafetyResult = await safetyValidator.ValidateContentAsync(generatedCode, SafetyLevel.High);
                if (!contentSafetyResult.IsValid)
                {
                    Console.WriteLine($"    ERROR: Content safety validation failed: {contentSafetyResult.Issues.Count} issues");
                    return;
                }
                Console.WriteLine("    SUCCESS: Content safety validation passed");

                // Complete operation
                Console.WriteLine("  SUCCESS: Completing AI operation...");
                await usageMonitor.CompleteOperationAsync(operationId, true, null, 
                    new Dictionary<string, object> 
                    { 
                        ["GeneratedCode"] = generatedCode,
                        ["QualityScore"] = 92,
                        ["ProcessingTime"] = 1500
                    });

                // Generate final analytics
                Console.WriteLine("  Stats Generating final analytics...");
                var analytics = await usageMonitor.GenerateAnalyticsAsync();
                Console.WriteLine($"    Workflow completed successfully!");
                Console.WriteLine($"    Final success rate: {analytics.Statistics.SuccessRate:F1}%");
                Console.WriteLine($"    Performance insights: {analytics.PerformanceInsights.Count}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Comprehensive workflow demo failed");
                Console.WriteLine($"ERROR: Comprehensive workflow demo failed: {ex.Message}");
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Mock environment profile for demo purposes
    /// </summary>
    public class EnvironmentProfile
    {
        public PlatformType CurrentPlatform { get; set; } = PlatformType.Windows;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
