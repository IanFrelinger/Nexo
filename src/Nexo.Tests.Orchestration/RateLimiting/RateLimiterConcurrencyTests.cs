using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.RateLimiting;
using Xunit;

namespace Nexo.Tests.Orchestration.RateLimiting;

public class RateLimiterConcurrencyTests
{
    [Fact]
    public async Task AcquireAsync_ShouldRespectLimit_UnderConcurrentRequests()
    {
        // Arrange
        var logger = new Mock<ILogger<RateLimiter>>();
        var rateLimiter = new RateLimiter("Test", maxRequests: 10, TimeSpan.FromSeconds(10), logger.Object);
        var key = "concurrent-test";

        // Act - Fire 50 concurrent requests
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => rateLimiter.AcquireAsync(key))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - Exactly 10 should be allowed
        var allowed = results.Count(r => r.Allowed);
        var denied = results.Count(r => !r.Allowed);

        allowed.Should().Be(10, "only 10 requests should be allowed within the limit");
        denied.Should().Be(40, "40 requests should be denied");
    }

    [Fact]
    public async Task AcquireAsync_ShouldNotExceedLimit_EvenWithRaceConditions()
    {
        // Arrange
        var logger = new Mock<ILogger<RateLimiter>>();
        var rateLimiter = new RateLimiter("Test", maxRequests: 5, TimeSpan.FromSeconds(10), logger.Object);
        var key = "race-test";
        var allowedCount = 0;

        // Act - Fire many concurrent requests in tight loop
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var result = await rateLimiter.AcquireAsync(key);
                if (result.Allowed)
                {
                    Interlocked.Increment(ref allowedCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should never exceed limit
        allowedCount.Should().BeLessOrEqualTo(5, "rate limiter should never exceed its limit");
    }
}
