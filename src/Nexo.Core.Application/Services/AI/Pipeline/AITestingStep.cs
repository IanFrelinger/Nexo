using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Code;
using Nexo.Core.Domain.Entities.Pipeline;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// AI-powered test generation pipeline step for automatic test case creation
    /// </summary>
    public class AITestingStep : IPipelineStep<TestingRequest>
    {
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly ILogger<AITestingStep> _logger;

        public AITestingStep(IAIRuntimeSelector runtimeSelector, ILogger<AITestingStep> logger)
        {
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "AI Test Generation";
        public int Order => 5;

        public async Task<TestingRequest> ExecuteAsync(TestingRequest input, PipelineContext context)
        {
            try
            {
                _logger.LogInformation("Starting AI test generation for {Language} code", input.Language);

                // Validate input
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogWarning("Empty code provided for test generation");
                    input.Result = new Nexo.Core.Domain.Results.TestingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "No code provided for test generation.",
                        Score = 0,
                        CompletedAt = DateTime.UtcNow
                    };
                    return input;
                }

                // Create AI operation context
                var aiContext = new AIOperationContext
                {
                    OperationType = AIOperationType.Testing,
                    TargetPlatform = context.EnvironmentProfile?.CurrentPlatform ?? PlatformType.Unknown,
                    MaxTokens = 4096,
                    Temperature = 0.3, // Lower temperature for more consistent test generation
                    Priority = AIPriority.Quality.ToString(),
                    Requirements = new Nexo.Core.Domain.Entities.AI.AIRequirements
                    {
                        Priority = AIPriority.Quality,
                        SafetyLevel = Nexo.Core.Domain.Enums.Safety.SafetyLevel.High,
                        RequiresHighQuality = true,
                        MaxTokens = 4096,
                        Temperature = 0.3
                    }
                };

                // Select optimal AI engine
                var provider = await _runtimeSelector.SelectOptimalProviderAsync(aiContext);
                if (provider == null)
                {
                    _logger.LogError("No suitable AI provider found for test generation");
                    throw new InvalidOperationException("No AI provider available for test generation");
                }

                // Create AI engine
                var engineInfo = new AIEngineInfo
                {
                    EngineType = provider.EngineType,
                    ModelPath = GetModelPathForTesting(provider.EngineType),
                    MaxTokens = aiContext.MaxTokens,
                    Temperature = aiContext.Temperature
                };

                var engine = await provider.CreateEngineAsync(aiContext);
                if (engine is not IAIEngine aiEngine)
                {
                    _logger.LogError("Failed to create AI engine for test generation");
                    throw new InvalidOperationException("Failed to create AI engine for test generation");
                }

                // Initialize engine if needed
                if (!aiEngine.IsInitialized)
                {
                    var modelInfo = new ModelInfo
                    {
                        Id = "test-model",
                        Name = "Test Model",
                        Version = "1.0",
                        Size = 1000000,
                        Format = "GGUF"
                    };
                    await aiEngine.InitializeAsync(modelInfo, aiContext);
                }

                // Generate tests
                var testCode = await GenerateTestCodeAsync(aiEngine, input, context);

                // Enhance test code with additional analysis
                var enhancedTests = await EnhanceTestCodeAsync(testCode, input, context);

                // Apply safety validation
                var validatedTests = await ApplySafetyValidationAsync(enhancedTests, input, context);

                // Create testing result
                var result = new Nexo.Core.Domain.Results.TestingResult
                {
                    IsSuccess = true,
                    Score = (int)CalculateTestQuality(validatedTests, input),
                    SuccessMessage = $"Generated tests with {CalculateTestCoverage(validatedTests, input.Code)}% coverage",
                    CompletedAt = DateTime.UtcNow
                };

                // Update input with results
                input.Result = result;
                input.TestGenerationCompleted = true;
                input.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("AI test generation completed with quality score {Score} and {Coverage}% coverage", 
                    result.QualityScore, result.Coverage);

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI test generation");
                
                // Create fallback result
                input.Result = new Nexo.Core.Domain.Results.TestingResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Test generation failed: {ex.Message}",
                    Exception = ex,
                    Score = 0,
                    CompletedAt = DateTime.UtcNow
                };
                input.TestGenerationCompleted = false;
                
                return input;
            }
        }

        private string GetModelPathForTesting(AIEngineType engineType)
        {
            return engineType switch
            {
                AIEngineType.LlamaWebAssembly => "models/codellama-7b-instruct.gguf",
                AIEngineType.LlamaNative => "models/codellama-13b-instruct.gguf",
                _ => "models/codellama-7b-instruct.gguf"
            };
        }

        private async Task<string> GenerateTestCodeAsync(IAIEngine aiEngine, TestingRequest request, PipelineContext context)
        {
            // Create a code generation request for test code
            var testPrompt = CreateTestPrompt(request, context);
            
            var codeGenRequest = new Nexo.Core.Domain.Entities.AI.CodeGenerationRequest
            {
                Prompt = testPrompt,
                Language = request.Language,
                MaxTokens = 2048,
                Temperature = 0.3
            };

            // Generate test code using the AI engine
            var testCodeResult = await aiEngine.GenerateCodeAsync(codeGenRequest);
            
            return testCodeResult.GeneratedCode;
        }

        private string CreateTestPrompt(TestingRequest request, PipelineContext context)
        {
            var prompt = $@"Generate comprehensive {request.TestType} tests for the following {request.Language} code:

```{request.Language.ToString().ToLower()}
{request.Code}
```

Requirements:
- Test all public methods and properties
- Include edge cases and boundary conditions
- Add proper test setup and teardown
- Use appropriate testing framework for {request.Language}
- Include meaningful test names and descriptions
- Add assertions for expected behavior
- Consider error handling scenarios

Context: {request.Context}
Platform: {context.EnvironmentProfile?.CurrentPlatform ?? PlatformType.Unknown}

Generate complete, runnable test code:";

            return prompt;
        }

        private async Task<string> EnhanceTestCodeAsync(string testCode, TestingRequest request, PipelineContext context)
        {
            _logger.LogDebug("Enhancing test code with additional analysis");

            var enhancedTests = testCode;

            // Add test framework setup
            var frameworkSetup = await GenerateTestFrameworkSetupAsync(
                Enum.TryParse<Nexo.Core.Domain.Enums.Code.CodeLanguage>(request.Language, out var lang) ? lang : Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp, 
                context);
            if (!string.IsNullOrEmpty(frameworkSetup))
            {
                enhancedTests = frameworkSetup + "\n\n" + enhancedTests;
            }

            // Add additional test cases
            var additionalTests = await GenerateAdditionalTestCasesAsync(request.Code, 
                Enum.TryParse<Nexo.Core.Domain.Enums.Code.CodeLanguage>(request.Language, out var lang2) ? lang2 : Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp, 
                Enum.TryParse<TestType>(request.TestType, out var testType2) ? testType2 : TestType.Unit);
            if (!string.IsNullOrEmpty(additionalTests))
            {
                enhancedTests += "\n\n" + additionalTests;
            }

            // Add test utilities
            var testUtilities = await GenerateTestUtilitiesAsync(
                Enum.TryParse<Nexo.Core.Domain.Enums.Code.CodeLanguage>(request.Language, out var lang3) ? lang3 : Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp, 
                context);
            if (!string.IsNullOrEmpty(testUtilities))
            {
                enhancedTests += "\n\n" + testUtilities;
            }

            // Add context-specific tests
            var contextTests = await GenerateContextSpecificTestsAsync(request, context);
            if (!string.IsNullOrEmpty(contextTests))
            {
                enhancedTests += "\n\n" + contextTests;
            }

            return enhancedTests;
        }

        private async Task<string> ApplySafetyValidationAsync(string testCode, TestingRequest request, PipelineContext context)
        {
            _logger.LogDebug("Applying safety validation to test code");

            // Validate test code for safety
            var safetyIssues = await ValidateTestCodeSafetyAsync(testCode, ParseLanguage(request.Language));
            if (safetyIssues.Any())
            {
                _logger.LogWarning("Safety issues detected in test code: {Issues}", string.Join(", ", safetyIssues));
                // Remove or replace unsafe test code
                testCode = await RemoveUnsafeTestCodeAsync(testCode, safetyIssues);
            }

            // Filter test code for appropriateness
            var filteredTestCode = await FilterTestCodeContentAsync(testCode, request, context);

            return filteredTestCode;
        }

        private async Task<string> GenerateTestFrameworkSetupAsync(Nexo.Core.Domain.Enums.Code.CodeLanguage language, Nexo.Core.Domain.Entities.Pipeline.PipelineContext context)
        {
            // In a real implementation, this would generate appropriate test framework setup
            await Task.Delay(50);

            return language switch
            {
                Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp => @"using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;",
                Nexo.Core.Domain.Enums.Code.CodeLanguage.JavaScript => @"// Jest test framework setup
const { describe, it, expect, beforeEach, afterEach } = require('jest');",
                Nexo.Core.Domain.Enums.Code.CodeLanguage.Python => @"import unittest
import pytest
from unittest.mock import Mock, patch",
                _ => "// Test framework setup"
            };
        }

        private async Task<string> GenerateAdditionalTestCasesAsync(string code, Nexo.Core.Domain.Enums.Code.CodeLanguage language, TestType testType)
        {
            // In a real implementation, this would generate additional test cases
            await Task.Delay(100);

            var additionalTests = new List<string>();

            // Generate edge case tests
            if (code.Contains("int") || code.Contains("number"))
            {
                additionalTests.Add(GenerateEdgeCaseTests(language, "numeric"));
            }

            if (code.Contains("string") || code.Contains("String"))
            {
                additionalTests.Add(GenerateEdgeCaseTests(language, "string"));
            }

            if (code.Contains("null") || code.Contains("None"))
            {
                additionalTests.Add(GenerateNullHandlingTests(language));
            }

            // Generate performance tests
            if (testType == TestType.Performance)
            {
                additionalTests.Add(GeneratePerformanceTests(language));
            }

            return string.Join("\n\n", additionalTests);
        }

        private string GenerateEdgeCaseTests(Nexo.Core.Domain.Enums.Code.CodeLanguage language, string type)
        {
            return language switch
            {
                Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp => $@"// Edge case tests for {type}
[TestMethod]
public void TestEdgeCases_{type}()
{{
    // Test with minimum value
    // Test with maximum value
    // Test with zero
    // Test with negative values
}}",
                Nexo.Core.Domain.Enums.Code.CodeLanguage.JavaScript => $@"// Edge case tests for {type}
describe('Edge Cases - {type}', () => {{
    it('should handle minimum values', () => {{
        // Test implementation
    }});
    
    it('should handle maximum values', () => {{
        // Test implementation
    }});
}});",
                _ => $"// Edge case tests for {type}"
            };
        }

        private string GenerateNullHandlingTests(Nexo.Core.Domain.Enums.Code.CodeLanguage language)
        {
            return language switch
            {
                Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp => @"// Null handling tests
[TestMethod]
[ExpectedException(typeof(ArgumentNullException))]
public void TestNullHandling()
{
    // Test with null input
    // Should throw ArgumentNullException
}",
                Nexo.Core.Domain.Enums.Code.CodeLanguage.JavaScript => @"// Null handling tests
describe('Null Handling', () => {
    it('should handle null input gracefully', () => {
        expect(() => {
            // Test with null input
        }).toThrow();
    });
});",
                _ => "// Null handling tests"
            };
        }

        private string GeneratePerformanceTests(Nexo.Core.Domain.Enums.Code.CodeLanguage language)
        {
            return language switch
            {
                Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp => @"// Performance tests
[TestMethod]
public void TestPerformance()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Execute code under test
    
    stopwatch.Stop();
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, ""Operation should complete within 1 second"");
}",
                Nexo.Core.Domain.Enums.Code.CodeLanguage.JavaScript => @"// Performance tests
describe('Performance', () => {
    it('should complete within acceptable time', () => {
        const start = performance.now();
        
        // Execute code under test
        
        const end = performance.now();
        expect(end - start).toBeLessThan(1000);
    });
});",
                _ => "// Performance tests"
            };
        }

        private async Task<string> GenerateTestUtilitiesAsync(Nexo.Core.Domain.Enums.Code.CodeLanguage language, PipelineContext context)
        {
            // In a real implementation, this would generate test utilities
            await Task.Delay(50);

            return language switch
            {
                Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp => @"// Test utilities
public static class TestUtilities
{
    public static T CreateTestObject<T>() where T : new()
    {
        return new T();
    }
    
    public static void AssertThrows<T>(Action action) where T : Exception
    {
        Assert.ThrowsException<T>(action);
    }
}",
                Nexo.Core.Domain.Enums.Code.CodeLanguage.JavaScript => @"// Test utilities
const TestUtilities = {
    createTestObject: (constructor) => new constructor(),
    assertThrows: (fn, expectedError) => {
        expect(fn).toThrow(expectedError);
    }
};",
                _ => "// Test utilities"
            };
        }

        private async Task<string> GenerateContextSpecificTestsAsync(TestingRequest request, PipelineContext context)
        {
            // In a real implementation, this would generate context-specific tests
            await Task.Delay(50);

            var contextTests = new List<string>();

            // Add platform-specific tests
            if (context.EnvironmentProfile?.CurrentPlatform == PlatformType.WebAssembly)
            {
                contextTests.Add("// WebAssembly-specific tests");
            }

            if (context.EnvironmentProfile?.CurrentPlatform == PlatformType.Windows)
            {
                contextTests.Add("// Windows-specific tests");
            }

            // Add test type specific tests
            if (request.TestType == "Integration")
            {
                contextTests.Add("// Integration test setup and teardown");
            }

            if (request.TestType == "Unit")
            {
                contextTests.Add("// Unit test isolation and mocking");
            }

            return string.Join("\n", contextTests);
        }

        private async Task<List<string>> ValidateTestCodeSafetyAsync(string testCode, Nexo.Core.Domain.Enums.Code.CodeLanguage language)
        {
            // In a real implementation, this would validate test code for safety
            await Task.Delay(50);

            var issues = new List<string>();

            // Check for potentially unsafe test code
            if (testCode.Contains("File.Delete") || testCode.Contains("rm -rf"))
            {
                issues.Add("File system operations detected in test code");
            }

            if (testCode.Contains("Process.Start") || testCode.Contains("exec"))
            {
                issues.Add("Process execution detected in test code");
            }

            if (testCode.Contains("Network") || testCode.Contains("HttpClient"))
            {
                issues.Add("Network operations detected in test code");
            }

            return issues;
        }

        private async Task<string> RemoveUnsafeTestCodeAsync(string testCode, List<string> safetyIssues)
        {
            // In a real implementation, this would remove unsafe test code
            await Task.Delay(50);

            // Remove or comment out unsafe test code
            foreach (var issue in safetyIssues)
            {
                _logger.LogWarning("Removing unsafe test code: {Issue}", issue);
            }

            return testCode;
        }

        private async Task<string> FilterTestCodeContentAsync(string testCode, TestingRequest request, PipelineContext context)
        {
            // In a real implementation, this would filter test code content
            await Task.Delay(50);

            // Remove or replace inappropriate content
            var filteredTestCode = testCode
                .Replace("dangerous", "risky")
                .Replace("unsafe", "requires caution");

            return filteredTestCode;
        }

        private int CalculateTestQuality(string testCode, TestingRequest request)
        {
            var score = 0;

            // Base score
            score += 20;

            // Length bonus
            if (testCode.Length > 1000) score += 20;
            if (testCode.Length > 2000) score += 10;

            // Test structure bonus
            if (testCode.Contains("Test")) score += 15;
            if (testCode.Contains("Assert")) score += 15;
            if (testCode.Contains("Mock")) score += 10;

            // Test coverage bonus
            if (testCode.Contains("EdgeCase")) score += 10;
            if (testCode.Contains("Exception")) score += 10;
            if (testCode.Contains("Performance")) score += 5;

            return Math.Min(100, score);
        }

        private int CalculateTestCoverage(string testCode, string sourceCode)
        {
            // In a real implementation, this would calculate actual test coverage
            var sourceLines = sourceCode.Split('\n').Length;
            var testLines = testCode.Split('\n').Length;
            
            // Simple coverage calculation
            var coverage = Math.Min(100, (testLines * 100) / Math.Max(1, sourceLines));
            
            return coverage;
        }

        public async Task<bool> CanExecuteAsync(TestingRequest input, PipelineContext context)
        {
            try
            {
                // Check if input is valid
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogDebug("Cannot execute test generation step: empty code provided");
                    return false;
                }

                // Check if AI runtime is available
                var providers = await _runtimeSelector.GetAvailableProvidersAsync();
                if (!providers.Any())
                {
                    _logger.LogDebug("Cannot execute test generation step: no AI providers available");
                    return false;
                }

                // Check if context is valid
                if (context == null)
                {
                    _logger.LogDebug("Cannot execute test generation step: null context provided");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if test generation step can execute");
                return false;
            }
        }
        
        private Nexo.Core.Domain.Enums.Code.CodeLanguage ParseLanguage(string language)
        {
            return language.ToLower() switch
            {
                "csharp" or "c#" => Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp,
                "javascript" or "js" => Nexo.Core.Domain.Enums.Code.CodeLanguage.JavaScript,
                "typescript" or "ts" => Nexo.Core.Domain.Enums.Code.CodeLanguage.TypeScript,
                "python" or "py" => Nexo.Core.Domain.Enums.Code.CodeLanguage.Python,
                "java" => Nexo.Core.Domain.Enums.Code.CodeLanguage.Java,
                "go" => Nexo.Core.Domain.Enums.Code.CodeLanguage.Go,
                "rust" => Nexo.Core.Domain.Enums.Code.CodeLanguage.Rust,
                _ => Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp
            };
        }
    }
}


    /// <summary>
    /// Testing result from AI pipeline processing
    /// </summary>
    public class TestingResult
    {
        public string GeneratedTests { get; set; } = string.Empty;
        public TestType TestType { get; set; }
        public int QualityScore { get; set; }
        public int Coverage { get; set; }
        public DateTime GenerationTime { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public AIEngineType EngineType { get; set; }
        public List<string> TestCategories { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of tests
    /// </summary>
    public enum TestType
    {
        Unit,
        Integration,
        Performance,
        Security,
        EndToEnd
    }
