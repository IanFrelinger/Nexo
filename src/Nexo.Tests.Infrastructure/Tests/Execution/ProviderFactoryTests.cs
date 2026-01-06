using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for ProviderFactory.
/// </summary>
public class ProviderFactoryTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestIsProviderAvailable();
            await TestExecuteLLMAsync();
            
            return new TestResult
            {
                TestName = nameof(ProviderFactoryTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All ProviderFactory tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ProviderFactoryTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(ProviderFactoryTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestIsProviderAvailable()
    {
        var mockLogger = new Mock<ILogger<ProviderFactory>>();
        var factory = new ProviderFactory(mockLogger.Object);
        
        AssertTrue(factory.IsProviderAvailable("openai"));
        AssertTrue(factory.IsProviderAvailable("azure"));
        AssertTrue(factory.IsProviderAvailable("ollama"));
        AssertTrue(factory.IsProviderAvailable("mock"));
        AssertFalse(factory.IsProviderAvailable("unknown"));
        
        await Task.CompletedTask;
    }
    
    private async Task TestExecuteLLMAsync()
    {
        var mockLogger = new Mock<ILogger<ProviderFactory>>();
        var factory = new ProviderFactory(mockLogger.Object);
        
        var result = await factory.ExecuteLLMAsync(
            "mock",
            "You are a test",
            "Test prompt",
            new { },
            CancellationToken.None);
        
        AssertNotNull(result);
        AssertTrue(result.Contains("Mock LLM response"));
        
        await AssertThrowsAsync<NotImplementedException>(async () =>
        {
            await factory.ExecuteLLMAsync(
                "openai",
                "Test",
                "Test",
                new { },
                CancellationToken.None);
        });
        
        await Task.CompletedTask;
    }
}

