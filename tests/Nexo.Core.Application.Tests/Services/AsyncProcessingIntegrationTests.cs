using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Services;
using Xunit;

namespace Nexo.Core.Application.Tests.Services
{
    public class AsyncProcessingIntegrationTests
    {
        [Fact]
        public async Task ProcessingQueue_Enqueue_Dequeue_Works()
        {
            var queue = new ProcessingQueue<string>();
            await queue.EnqueueAsync("test1");
            await queue.EnqueueAsync("test2");
            var item1 = await queue.DequeueAsync();
            var item2 = await queue.DequeueAsync();
            Assert.Equal("test1", item1);
            Assert.Equal("test2", item2);
        }

        [Fact]
        public async Task AsyncProcessor_Processes_Requests()
        {
            var processor = new AsyncProcessor<int, int>(async (x, ct) =>
            {
                await Task.Delay(10, ct);
                return x * 2;
            });
            var result = await processor.ProcessAsync(21);
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task CacheStrategy_Caches_And_Retrieves_Values()
        {
            var cache = new CacheStrategy<string, int>();
            await cache.SetAsync("foo", 123);
            var value = await cache.GetAsync("foo");
            Assert.Equal(123, value);
            await cache.RemoveAsync("foo");
            var missing = await cache.GetAsync("foo");
            Assert.Equal(0, missing); // default(int) is 0
        }

        [Fact]
        public async Task EndToEnd_Queue_Processor_Cache()
        {
            var queue = new ProcessingQueue<int>();
            var cache = new CacheStrategy<int, int>();
            var processor = new AsyncProcessor<int, int>(async (x, ct) =>
            {
                await Task.Delay(5, ct);
                var cached = await cache.GetAsync(x, ct);
                if (cached != 0) return cached;
                var result = x * x;
                await cache.SetAsync(x, result, ttl: null, ct);
                return result;
            });

            await queue.EnqueueAsync(3);
            await queue.EnqueueAsync(4);
            var item1 = await queue.DequeueAsync();
            var result1 = await processor.ProcessAsync(item1);
            Assert.Equal(9, result1);
            var item2 = await queue.DequeueAsync();
            var result2 = await processor.ProcessAsync(item2);
            Assert.Equal(16, result2);
            // Cached result
            var result1Cached = await processor.ProcessAsync(3);
            Assert.Equal(9, result1Cached);
        }
    }
} 