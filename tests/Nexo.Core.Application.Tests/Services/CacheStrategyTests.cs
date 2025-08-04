using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Core.Application.Services;

namespace Nexo.Core.Application.Tests.Services
{
    public class CacheStrategyTests
    {
        [Fact]
        public async Task SetAsync_WithTTL_ExpiresCorrectly()
        {
            var cache = new CacheStrategy<string, int>();
            await cache.SetAsync("foo", 123, TimeSpan.FromMilliseconds(50));
            var value1 = await cache.GetAsync("foo");
            Assert.Equal(123, value1);
            await Task.Delay(60);
            var value2 = await cache.GetAsync("foo");
            Assert.Equal(0, value2); // default(int) is 0
        }

        [Fact]
        public async Task RemoveAsync_And_InvalidateAsync_RemovesEntry()
        {
            var cache = new CacheStrategy<string, int>();
            await cache.SetAsync("bar", 456);
            var value1 = await cache.GetAsync("bar");
            Assert.Equal(456, value1);
            await cache.RemoveAsync("bar");
            var value2 = await cache.GetAsync("bar");
            Assert.Equal(0, value2);
            await cache.SetAsync("baz", 789);
            await cache.InvalidateAsync("baz");
            var value3 = await cache.GetAsync("baz");
            Assert.Equal(0, value3);
        }

        [Fact]
        public async Task ClearAsync_RemovesAllEntries()
        {
            var cache = new CacheStrategy<string, int>();
            await cache.SetAsync("a", 1);
            await cache.SetAsync("b", 2);
            await cache.SetAsync("c", 3);
            await cache.ClearAsync();
            Assert.Equal(0, await cache.GetAsync("a"));
            Assert.Equal(0, await cache.GetAsync("b"));
            Assert.Equal(0, await cache.GetAsync("c"));
        }
    }
} 