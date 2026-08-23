using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Execution;

namespace Ashlar.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for SemanticCache.
/// </summary>
public class SemanticCacheTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestGetAndSet();
            await TestCacheMiss();
            
            return new TestResult
            {
                Name = nameof(SemanticCacheTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All SemanticCache tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(SemanticCacheTests),
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
                Name = nameof(SemanticCacheTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestGetAndSet()
    {
        var mockLogger = new Mock<ILogger<SemanticCache>>();
        var cache = new SemanticCache(mockLogger.Object);
        
        var output = new BrickOutput
        {
            ["result"] = "test",
            Summary = "Test output"
        };
        
        await cache.SetAsync("test-key", output);
        var retrieved = await cache.GetAsync("test-key");
        
        AssertNotNull(retrieved);
        AssertEqual("test", retrieved!.Get<string>("result"));
        AssertEqual("Test output", retrieved.Summary);
        
        await Task.CompletedTask;
    }
    
    private async Task TestCacheMiss()
    {
        var mockLogger = new Mock<ILogger<SemanticCache>>();
        var cache = new SemanticCache(mockLogger.Object);
        
        var result = await cache.GetAsync("missing-key");
        AssertNull(result);
        
        await Task.CompletedTask;
    }
}

