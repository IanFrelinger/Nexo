using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Services;
using Xunit;
using System.Collections.Generic;
using Nexo.Shared;

namespace Nexo.Core.Application.Tests.Services
{
    public class CachingAsyncProcessorTests
    {
        public struct TestKey
        {
            public string Value { get; }
            public TestKey(string value)
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("TestKey.Value cannot be null or empty", nameof(value));
                Value = value;
            }
            public override bool Equals(object? obj) => obj is TestKey other && Value == other.Value;
            public override int GetHashCode() => Value.GetHashCode();
            public override string ToString() => Value;
            public static implicit operator TestKey(string value) => new TestKey(value);
            public static implicit operator string(TestKey key) => key.Value;
        }

        [Fact]
        public async Task CacheStrategy_StringKey_Works()
        {
            var cache = new CacheStrategy<string, int>();
            await cache.SetAsync("foo", 42);
            var value = await cache.GetAsync("foo");
            Assert.Equal(42, value);
        }

        [Fact]
        public async Task CacheStrategy_StructKey_Works()
        {
            var cache = new CacheStrategy<TestKey, int>();
            var key = new TestKey("bar");
            await cache.SetAsync(key, 99);
            var value = await cache.GetAsync(key);
            Assert.Equal(99, value);
        }

        [Fact]
        public async Task CacheStrategy_StructKey_TryGetValue_And_GetAsync_Work()
        {
            var cache = new CacheStrategy<TestKey, int>();
            var key = new TestKey("baz");
            await cache.SetAsync(key, 123);
            int value;
            var found = cache.TryGetValue(key, out value);
            var asyncValue = await cache.GetAsync(key);
            Console.WriteLine($"[Test] TryGetValue: found={found}, value={value}");
            Console.WriteLine($"[Test] GetAsync: value={asyncValue}");
            Assert.True(found);
            Assert.Equal(123, value);
            Assert.Equal(123, asyncValue);
        }

        [Fact]
        public async Task CacheStrategy_StructKey_DifferentInstances_SameValue_Works()
        {
            var cache = new CacheStrategy<TestKey, int>();
            var key1 = new TestKey("test");
            var key2 = new TestKey("test");
            Assert.True(key1.Equals(key2)); // Should have same value
            await cache.SetAsync(key1, 555);
            int value;
            var found = cache.TryGetValue(key2, out value);
            var asyncValue = await cache.GetAsync(key2);
            Console.WriteLine($"[Test] TryGetValue: found={found}, value={value}");
            Console.WriteLine($"[Test] GetAsync: value={asyncValue}");
            Assert.True(found);
            Assert.Equal(555, value);
            Assert.Equal(555, asyncValue);
        }

        [Fact]
        public async Task Minimal_CachingAsyncProcessor_StructKey_DifferentInstances_SameValue_Works()
        {
            var callCount = 0;
            var innerProcessor = new AsyncProcessor<TestKey, int>((input, ct) =>
            {
                callCount++;
                return Task.FromResult(777);
            });

            var cache = new CacheStrategy<TestKey, int>();
            var processor = new CachingAsyncProcessor<TestKey, TestKey, int>(
                innerProcessor,
                cache,
                keySelector: input => input
            );

            var key1 = new TestKey("test");
            var key2 = new TestKey("test");
            Assert.True(key1.Equals(key2));

            // First call should set the cache
            var result1 = await processor.ProcessAsync(key1);
            Assert.Equal(777, result1);
            Assert.Equal(1, callCount);

            // Second call with different instance but same value should use cache
            var result2 = await processor.ProcessAsync(key2);
            Assert.Equal(777, result2);
            Assert.Equal(1, callCount); // Should not have called inner processor again
        }
    }

    public class CachingAsyncProcessor_SemanticKey_IntegrationTests
    {
        [Fact]
        public async Task IdenticalSemanticKeys_ResultsAreCached()
        {
            int callCount = 0;
            var inner = new AsyncProcessor<string, int>((input, ct) => { callCount++; return Task.FromResult(42); });
            var cache = new CacheStrategy<string, int>();
            var processor = new CachingAsyncProcessor<string, string, int>(
                inner,
                cache,
                keySelector: input => SemanticCacheKeyGenerator.Generate(input)
            );

            var input1 = "  public void Foo() { return 1; }  ";
            var input2 = "public   void foo( ) {   return 1; }";
            var result1 = await processor.ProcessAsync(input1);
            var result2 = await processor.ProcessAsync(input2);
            Assert.Equal(result1, result2);
            Assert.Equal(1, callCount); // Only called once due to cache
        }

        [Fact]
        public async Task DifferentSemanticKeys_NotCachedTogether()
        {
            int callCount = 0;
            var inner = new AsyncProcessor<string, int>((input, ct) => { callCount++; return Task.FromResult(input.Length); });
            var cache = new CacheStrategy<string, int>();
            var processor = new CachingAsyncProcessor<string, string, int>(
                inner,
                cache,
                keySelector: input => SemanticCacheKeyGenerator.Generate(input)
            );

            var input1 = "foo";
            var input2 = "baz";
            var result1 = await processor.ProcessAsync(input1);
            var result2 = await processor.ProcessAsync(input2);
            // Both should call inner processor because they have different semantic keys
            Assert.Equal(2, callCount);
            // The results should be the same (both length 3) but processed separately
            Assert.Equal(3, result1);
            Assert.Equal(3, result2);
        }

        [Fact]
        public async Task ContextAndModelParameters_AffectCacheKey()
        {
            int callCount = 0;
            var inner = new AsyncProcessor<string, int>((input, ct) => { callCount++; return Task.FromResult(99); });
            var cache = new CacheStrategy<string, int>();
            var processor = new CachingAsyncProcessor<string, string, int>(
                inner,
                cache,
                keySelector: input => SemanticCacheKeyGenerator.Generate(
                    input,
                    new Dictionary<string, object> { { "user", "alice" } },
                    new Dictionary<string, object> { { "model", "gpt-4" } }
                )
            );
            var processor2 = new CachingAsyncProcessor<string, string, int>(
                inner,
                cache,
                keySelector: input => SemanticCacheKeyGenerator.Generate(
                    input,
                    new Dictionary<string, object> { { "user", "bob" } },
                    new Dictionary<string, object> { { "model", "gpt-4" } }
                )
            );
            var result1 = await processor.ProcessAsync("code");
            var result2 = await processor2.ProcessAsync("code");
            Assert.Equal(2, callCount); // Different context, not cached together
        }
    }
} 