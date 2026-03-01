using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Caching;

namespace Nexo.Tests.Infrastructure.Tests.Caching;

public class MemoryCacheStrategyTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestGetAsyncCacheHit();
            await TestGetAsyncCacheMiss();
            await TestGetAsyncExpiredEntry();
            await TestSetAsync();
            await TestSetAsyncWithExpiration();
            await TestRemoveAsync();
            await TestClearAsync();
            await TestConcurrentAccess();

            return new TestResult
            {
                Name = nameof(MemoryCacheStrategyTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All MemoryCacheStrategy tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(MemoryCacheStrategyTests),
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
                Name = nameof(MemoryCacheStrategyTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestGetAsyncCacheHit()
    {
        var mockLogger = new Mock<ILogger<MemoryCacheStrategy>>();
        var cache = new MemoryCacheStrategy(mockLogger.Object);

        var testValue = new TestClass { Id = 1, Name = "Test" };
        await cache.SetAsync("test-key", testValue, TimeSpan.FromMinutes(5));

        var result = await cache.GetAsync<TestClass>("test-key");

        AssertNotNull(result);
        AssertEqual(1, result!.Id);
        AssertEqual("Test", result.Name);
    }

    private async Task TestGetAsyncCacheMiss()
    {
        var mockLogger = new Mock<ILogger<MemoryCacheStrategy>>();
        var cache = new MemoryCacheStrategy(mockLogger.Object);

        var result = await cache.GetAsync<TestClass>("non-existent-key");

        AssertNull(result);
    }

    private async Task TestGetAsyncExpiredEntry()
    {
        var mockLogger = new Mock<ILogger<MemoryCacheStrategy>>();
        var cache = new MemoryCacheStrategy(mockLogger.Object);

        var testValue = new TestClass { Id = 1, Name = "Test" };
        // Set with very short expiration
        await cache.SetAsync("expired-key", testValue, TimeSpan.FromMilliseconds(10));

        // Wait for expiration
        await Task.Delay(50);

        var result = await cache.GetAsync<TestClass>("expired-key");

        AssertNull(result);
    }

    private async Task TestSetAsync()
    {
        var mockLogger = new Mock<ILogger<MemoryCacheStrategy>>();
        var cache = new MemoryCacheStrategy(mockLogger.Object);

        var testValue = new TestClass { Id = 2, Name = "Test2" };
        await cache.SetAsync("set-key", testValue);

        var result = await cache.GetAsync<TestClass>("set-key");
        AssertNotNull(result);
        AssertEqual(2, result!.Id);
    }

    private async Task TestSetAsyncWithExpiration()
    {
        var mockLogger = new Mock<ILogger<MemoryCacheStrategy>>();
        var cache = new MemoryCacheStrategy(mockLogger.Object);

        var testValue = new TestClass { Id = 3, Name = "Test3" };
        await cache.SetAsync("expiring-key", testValue, TimeSpan.FromSeconds(1));

        var result = await cache.GetAsync<TestClass>("expiring-key");
        AssertNotNull(result);

        await Task.Delay(1100);

        var expiredResult = await cache.GetAsync<TestClass>("expiring-key");
        AssertNull(expiredResult);
    }

    private async Task TestRemoveAsync()
    {
        var mockLogger = new Mock<ILogger<MemoryCacheStrategy>>();
        var cache = new MemoryCacheStrategy(mockLogger.Object);

        var testValue = new TestClass { Id = 4, Name = "Test4" };
        await cache.SetAsync("remove-key", testValue);
        await cache.RemoveAsync("remove-key");

        var result = await cache.GetAsync<TestClass>("remove-key");
        AssertNull(result);
    }

    private async Task TestClearAsync()
    {
        var mockLogger = new Mock<ILogger<MemoryCacheStrategy>>();
        var cache = new MemoryCacheStrategy(mockLogger.Object);

        await cache.SetAsync("key1", new TestClass { Id = 1 });
        await cache.SetAsync("key2", new TestClass { Id = 2 });
        await cache.SetAsync("key3", new TestClass { Id = 3 });

        await cache.ClearAsync();

        AssertNull(await cache.GetAsync<TestClass>("key1"));
        AssertNull(await cache.GetAsync<TestClass>("key2"));
        AssertNull(await cache.GetAsync<TestClass>("key3"));
    }

    private async Task TestConcurrentAccess()
    {
        var mockLogger = new Mock<ILogger<MemoryCacheStrategy>>();
        var cache = new MemoryCacheStrategy(mockLogger.Object);

        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                await cache.SetAsync($"key{index}", new TestClass { Id = index });
                var result = await cache.GetAsync<TestClass>($"key{index}");
                AssertNotNull(result);
                AssertEqual(index, result!.Id);
            }));
        }

        await Task.WhenAll(tasks);
    }

    private class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

