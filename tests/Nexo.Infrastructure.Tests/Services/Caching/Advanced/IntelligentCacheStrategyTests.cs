using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.Caching;
using Nexo.Infrastructure.Services.Caching.Advanced;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.Caching.Advanced
{
    /// <summary>
    /// Tests for intelligent cache eviction policy.
    /// Part of Phase 3.3 testing and validation.
    /// </summary>
    public class IntelligentCacheStrategyTests
    {
        private readonly Mock<ICacheEvictionStrategy> _mockStrategy1;
        private readonly Mock<ICacheEvictionStrategy> _mockStrategy2;
        private readonly CacheEvictionConfiguration _configuration;

        public IntelligentCacheStrategyTests()
        {
            _mockStrategy1 = new Mock<ICacheEvictionStrategy>();
            _mockStrategy1.Setup(s => s.Priority).Returns(1);
            _mockStrategy1.Setup(s => s.Name).Returns("Strategy1");

            _mockStrategy2 = new Mock<ICacheEvictionStrategy>();
            _mockStrategy2.Setup(s => s.Priority).Returns(2);
            _mockStrategy2.Setup(s => s.Name).Returns("Strategy2");

            _configuration = new CacheEvictionConfiguration
            {
                AgeWeight = 1.0,
                AccessWeight = 2.0,
                PriorityWeight = 3.0,
                SizeWeight = 0.5,
                LastAccessWeight = 1.5
            };
        }

        [Fact]
        public void SelectForEviction_WithEmptyItems_ReturnsEmpty()
        {
            // Arrange
            var policy = new IntelligentEvictionPolicy(new[] { _mockStrategy1.Object }, _configuration);
            var items = new List<CacheItem>();

            // Act
            var result = policy.SelectForEviction(items, 5);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void SelectForEviction_WithFewerItemsThanRequested_ReturnsAllItems()
        {
            // Arrange
            var policy = new IntelligentEvictionPolicy(new[] { _mockStrategy1.Object }, _configuration);
            var items = CreateTestItems(3);

            // Act
            var result = policy.SelectForEviction(items, 5);

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void SelectForEviction_WithMoreItemsThanRequested_ReturnsCorrectCount()
        {
            // Arrange
            var policy = new IntelligentEvictionPolicy(new[] { _mockStrategy1.Object }, _configuration);
            var items = CreateTestItems(10);

            // Act
            var result = policy.SelectForEviction(items, 5);

            // Assert
            Assert.Equal(5, result.Count());
        }

        [Fact]
        public void SelectForEviction_WithStrategies_AppliesStrategiesInPriorityOrder()
        {
            // Arrange
            var items = CreateTestItems(10);
            _mockStrategy1.Setup(s => s.FilterCandidates(items, 5))
                .Returns(items.Take(7));
            _mockStrategy2.Setup(s => s.FilterCandidates(It.IsAny<IEnumerable<CacheItem>>(), 5))
                .Returns(items.Take(6));

            var policy = new IntelligentEvictionPolicy(
                new[] { _mockStrategy1.Object, _mockStrategy2.Object }, 
                _configuration);

            // Act
            var result = policy.SelectForEviction(items, 5);

            // Assert
            _mockStrategy1.Verify(s => s.FilterCandidates(items, 5), Times.Once);
            _mockStrategy2.Verify(s => s.FilterCandidates(It.IsAny<IEnumerable<CacheItem>>(), 5), Times.Once);
            Assert.Equal(5, result.Count());
        }

        [Fact]
        public void SelectForEviction_WithNullItems_ThrowsArgumentNullException()
        {
            // Arrange
            var policy = new IntelligentEvictionPolicy(new[] { _mockStrategy1.Object }, _configuration);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => policy.SelectForEviction(null!, 5));
        }

        [Fact]
        public void SelectForEviction_WithZeroCount_ReturnsEmpty()
        {
            // Arrange
            var policy = new IntelligentEvictionPolicy(new[] { _mockStrategy1.Object }, _configuration);
            var items = CreateTestItems(5);

            // Act
            var result = policy.SelectForEviction(items, 0);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void SelectForEviction_WithNegativeCount_ReturnsEmpty()
        {
            // Arrange
            var policy = new IntelligentEvictionPolicy(new[] { _mockStrategy1.Object }, _configuration);
            var items = CreateTestItems(5);

            // Act
            var result = policy.SelectForEviction(items, -1);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void SelectForEviction_WithNullStrategies_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new IntelligentEvictionPolicy(null!, _configuration));
        }

        [Fact]
        public void SelectForEviction_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new IntelligentEvictionPolicy(new[] { _mockStrategy1.Object }, null!));
        }

        private List<CacheItem> CreateTestItems(int count)
        {
            var items = new List<CacheItem>();
            var now = DateTimeOffset.UtcNow;

            for (int i = 0; i < count; i++)
            {
                items.Add(new CacheItem
                {
                    Key = $"key{i}",
                    Value = $"value{i}",
                    CreatedAt = now.AddMinutes(-i),
                    LastAccessedAt = now.AddMinutes(-i),
                    AccessCount = i,
                    Size = i * 100,
                    Priority = (CacheItemPriority)(i % 4)
                });
            }

            return items;
        }
    }

    /// <summary>
    /// Tests for LRU eviction strategy.
    /// </summary>
    public class LruEvictionStrategyTests
    {
        [Fact]
        public void FilterCandidates_WithValidInput_ReturnsOrderedCandidates()
        {
            // Arrange
            var strategy = new LruEvictionStrategy();
            var items = CreateTestItems(10);
            var targetCount = 5;

            // Act
            var result = strategy.FilterCandidates(items, targetCount);

            // Assert
            Assert.Equal(targetCount * 2, result.Count());
            var orderedResult = result.OrderBy(item => item.LastAccessedAt).ToList();
            Assert.Equal(orderedResult, result);
        }

        [Fact]
        public void FilterCandidates_WithEmptyInput_ReturnsEmpty()
        {
            // Arrange
            var strategy = new LruEvictionStrategy();
            var items = new List<CacheItem>();

            // Act
            var result = strategy.FilterCandidates(items, 5);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void FilterCandidates_WithNullInput_ThrowsArgumentNullException()
        {
            // Arrange
            var strategy = new LruEvictionStrategy();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => strategy.FilterCandidates(null!, 5));
        }

        private List<CacheItem> CreateTestItems(int count)
        {
            var items = new List<CacheItem>();
            var now = DateTimeOffset.UtcNow;

            for (int i = 0; i < count; i++)
            {
                items.Add(new CacheItem
                {
                    Key = $"key{i}",
                    Value = $"value{i}",
                    LastAccessedAt = now.AddMinutes(-i)
                });
            }

            return items;
        }
    }

    /// <summary>
    /// Tests for LFU eviction strategy.
    /// </summary>
    public class LfuEvictionStrategyTests
    {
        [Fact]
        public void FilterCandidates_WithValidInput_ReturnsOrderedCandidates()
        {
            // Arrange
            var strategy = new LfuEvictionStrategy();
            var items = CreateTestItems(10);
            var targetCount = 5;

            // Act
            var result = strategy.FilterCandidates(items, targetCount);

            // Assert
            Assert.Equal(targetCount * 2, result.Count());
            var orderedResult = result.OrderBy(item => item.AccessCount)
                .ThenBy(item => item.LastAccessedAt).ToList();
            Assert.Equal(orderedResult, result);
        }

        private List<CacheItem> CreateTestItems(int count)
        {
            var items = new List<CacheItem>();
            var now = DateTimeOffset.UtcNow;

            for (int i = 0; i < count; i++)
            {
                items.Add(new CacheItem
                {
                    Key = $"key{i}",
                    Value = $"value{i}",
                    AccessCount = i,
                    LastAccessedAt = now.AddMinutes(-i)
                });
            }

            return items;
        }
    }

    /// <summary>
    /// Tests for size-based eviction strategy.
    /// </summary>
    public class SizeBasedEvictionStrategyTests
    {
        [Fact]
        public void FilterCandidates_WithValidInput_ReturnsOrderedCandidates()
        {
            // Arrange
            var strategy = new SizeBasedEvictionStrategy();
            var items = CreateTestItems(10);
            var targetCount = 5;

            // Act
            var result = strategy.FilterCandidates(items, targetCount);

            // Assert
            Assert.Equal(targetCount * 2, result.Count());
            var orderedResult = result.OrderByDescending(item => item.Size).ToList();
            Assert.Equal(orderedResult, result);
        }

        private List<CacheItem> CreateTestItems(int count)
        {
            var items = new List<CacheItem>();

            for (int i = 0; i < count; i++)
            {
                items.Add(new CacheItem
                {
                    Key = $"key{i}",
                    Value = $"value{i}",
                    Size = i * 100
                });
            }

            return items;
        }
    }

    /// <summary>
    /// Tests for priority-based eviction strategy.
    /// </summary>
    public class PriorityBasedEvictionStrategyTests
    {
        [Fact]
        public void FilterCandidates_WithValidInput_ReturnsOrderedCandidates()
        {
            // Arrange
            var strategy = new PriorityBasedEvictionStrategy();
            var items = CreateTestItems(10);
            var targetCount = 5;

            // Act
            var result = strategy.FilterCandidates(items, targetCount);

            // Assert
            Assert.Equal(targetCount * 2, result.Count());
            var orderedResult = result.OrderBy(item => item.Priority)
                .ThenBy(item => item.LastAccessedAt).ToList();
            Assert.Equal(orderedResult, result);
        }

        private List<CacheItem> CreateTestItems(int count)
        {
            var items = new List<CacheItem>();
            var now = DateTimeOffset.UtcNow;

            for (int i = 0; i < count; i++)
            {
                items.Add(new CacheItem
                {
                    Key = $"key{i}",
                    Value = $"value{i}",
                    Priority = (CacheItemPriority)(i % 4),
                    LastAccessedAt = now.AddMinutes(-i)
                });
            }

            return items;
        }
    }
}