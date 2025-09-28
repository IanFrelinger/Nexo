using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Nexo.Shared.Operations;

namespace Nexo.Tests.Composable
{
    /// <summary>
    /// Tests for dependency-wrapped operations
    /// </summary>
    public class OperationTests
    {
        private readonly ICollectionOperations _collectionOps;
        private readonly ILoopOperations _loopOps;
        private readonly IStringOperations _stringOps;
        
        public OperationTests()
        {
            _collectionOps = new CollectionOperations();
            _loopOps = new LoopOperations();
            _stringOps = new StringOperations();
        }
        
        [Fact]
        public async Task CollectionOperations_WhereAsync_ShouldFilter()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            // Act
            var evenNumbers = await _collectionOps.WhereAsync(items, async x => await Task.FromResult(x % 2 == 0));
            
            // Assert
            Assert.Equal(5, evenNumbers.Count);
            Assert.All(evenNumbers, x => Assert.True(x % 2 == 0));
        }
        
        [Fact]
        public async Task CollectionOperations_SelectAsync_ShouldTransform()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5 };
            
            // Act
            var doubled = await _collectionOps.SelectAsync(items, async x => await Task.FromResult(x * 2));
            
            // Assert
            Assert.Equal(5, doubled.Count);
            Assert.Equal(new[] { 2, 4, 6, 8, 10 }, doubled);
        }
        
        [Fact]
        public async Task CollectionOperations_AnyAsync_ShouldReturnTrue()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5 };
            
            // Act
            var hasEven = await _collectionOps.AnyAsync(items, async x => await Task.FromResult(x % 2 == 0));
            var hasGreaterThan10 = await _collectionOps.AnyAsync(items, async x => await Task.FromResult(x > 10));
            
            // Assert
            Assert.True(hasEven);
            Assert.False(hasGreaterThan10);
        }
        
        [Fact]
        public async Task CollectionOperations_AllAsync_ShouldReturnCorrectResult()
        {
            // Arrange
            var positiveNumbers = new List<int> { 1, 2, 3, 4, 5 };
            var mixedNumbers = new List<int> { 1, 2, -3, 4, 5 };
            
            // Act
            var allPositive1 = await _collectionOps.AllAsync(positiveNumbers, async x => await Task.FromResult(x > 0));
            var allPositive2 = await _collectionOps.AllAsync(mixedNumbers, async x => await Task.FromResult(x > 0));
            
            // Assert
            Assert.True(allPositive1);
            Assert.False(allPositive2);
        }
        
        [Fact]
        public async Task CollectionOperations_FirstOrDefaultAsync_ShouldReturnFirstMatch()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5 };
            
            // Act
            var firstEven = await _collectionOps.FirstOrDefaultAsync(items, async x => await Task.FromResult(x % 2 == 0));
            var firstGreaterThan10 = await _collectionOps.FirstOrDefaultAsync(items, async x => await Task.FromResult(x > 10));
            
            // Assert
            Assert.Equal(2, firstEven);
            Assert.Equal(0, firstGreaterThan10); // Default value for int
        }
        
        [Fact]
        public async Task CollectionOperations_CountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            // Act
            var evenCount = await _collectionOps.CountAsync(items, async x => await Task.FromResult(x % 2 == 0));
            var oddCount = await _collectionOps.CountAsync(items, async x => await Task.FromResult(x % 2 == 1));
            
            // Assert
            Assert.Equal(5, evenCount);
            Assert.Equal(5, oddCount);
        }
        
        [Fact]
        public async Task CollectionOperations_GroupByAsync_ShouldGroup()
        {
            // Arrange
            var items = new List<string> { "apple", "banana", "cherry", "date", "elderberry" };
            
            // Act
            var grouped = await _collectionOps.GroupByAsync(items, async x => await Task.FromResult(x.Length));
            
            // Assert
            Assert.True(grouped.ContainsKey(5)); // apple, cherry
            Assert.True(grouped.ContainsKey(6)); // banana, cherry
            Assert.True(grouped.ContainsKey(4)); // date
            Assert.True(grouped.ContainsKey(10)); // elderberry
        }
        
        [Fact]
        public async Task CollectionOperations_OrderByAsync_ShouldSort()
        {
            // Arrange
            var items = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };
            
            // Act
            var sorted = await _collectionOps.OrderByAsync(items, async x => await Task.FromResult(x));
            
            // Assert
            Assert.Equal(new[] { 1, 1, 2, 3, 4, 5, 6, 9 }, sorted);
        }
        
        [Fact]
        public async Task CollectionOperations_TakeAsync_ShouldTakeFirstN()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            // Act
            var firstThree = await _collectionOps.TakeAsync(items, 3);
            
            // Assert
            Assert.Equal(3, firstThree.Count);
            Assert.Equal(new[] { 1, 2, 3 }, firstThree);
        }
        
        [Fact]
        public async Task CollectionOperations_SkipAsync_ShouldSkipFirstN()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            // Act
            var afterFirstThree = await _collectionOps.SkipAsync(items, 3);
            
            // Assert
            Assert.Equal(7, afterFirstThree.Count);
            Assert.Equal(new[] { 4, 5, 6, 7, 8, 9, 10 }, afterFirstThree);
        }
        
        [Fact]
        public async Task LoopOperations_ForAsync_ShouldExecute()
        {
            // Arrange
            var executedValues = new List<int>();
            
            // Act
            await _loopOps.ForAsync(0, 5, async i => 
            {
                executedValues.Add(i);
                await Task.CompletedTask;
            });
            
            // Assert
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, executedValues);
        }
        
        [Fact]
        public async Task LoopOperations_ForAsync_WithStep_ShouldExecute()
        {
            // Arrange
            var executedValues = new List<int>();
            
            // Act
            await _loopOps.ForAsync(0, 10, 2, async i => 
            {
                executedValues.Add(i);
                await Task.CompletedTask;
            });
            
            // Assert
            Assert.Equal(new[] { 0, 2, 4, 6, 8 }, executedValues);
        }
        
        [Fact]
        public async Task LoopOperations_WhileAsync_ShouldExecute()
        {
            // Arrange
            var counter = 0;
            var executedCount = 0;
            
            // Act
            await _loopOps.WhileAsync(
                async () => await Task.FromResult(counter < 3),
                async () => 
                {
                    counter++;
                    executedCount++;
                    await Task.CompletedTask;
                });
            
            // Assert
            Assert.Equal(3, counter);
            Assert.Equal(3, executedCount);
        }
        
        [Fact]
        public async Task LoopOperations_DoWhileAsync_ShouldExecute()
        {
            // Arrange
            var counter = 0;
            var executedCount = 0;
            
            // Act
            await _loopOps.DoWhileAsync(
                async () => 
                {
                    counter++;
                    executedCount++;
                    await Task.CompletedTask;
                },
                async () => await Task.FromResult(counter < 3));
            
            // Assert
            Assert.Equal(3, counter);
            Assert.Equal(3, executedCount);
        }
        
        [Fact]
        public async Task LoopOperations_ForEachAsync_ShouldExecute()
        {
            // Arrange
            var items = new List<string> { "a", "b", "c" };
            var processedItems = new List<string>();
            
            // Act
            await _loopOps.ForEachAsync(items, async (item, index) => 
            {
                processedItems.Add($"{index}:{item}");
                await Task.CompletedTask;
            });
            
            // Assert
            Assert.Equal(new[] { "0:a", "1:b", "2:c" }, processedItems);
        }
        
        [Fact]
        public async Task StringOperations_IsNullOrEmptyAsync_ShouldReturnCorrectResult()
        {
            // Act & Assert
            Assert.True(await _stringOps.IsNullOrEmptyAsync(null));
            Assert.True(await _stringOps.IsNullOrEmptyAsync(""));
            Assert.False(await _stringOps.IsNullOrEmptyAsync("test"));
        }
        
        [Fact]
        public async Task StringOperations_IsNullOrWhiteSpaceAsync_ShouldReturnCorrectResult()
        {
            // Act & Assert
            Assert.True(await _stringOps.IsNullOrWhiteSpaceAsync(null));
            Assert.True(await _stringOps.IsNullOrWhiteSpaceAsync(""));
            Assert.True(await _stringOps.IsNullOrWhiteSpaceAsync("   "));
            Assert.False(await _stringOps.IsNullOrWhiteSpaceAsync("test"));
        }
        
        [Fact]
        public async Task StringOperations_TrimAsync_ShouldTrim()
        {
            // Act & Assert
            Assert.Equal("test", await _stringOps.TrimAsync("  test  "));
            Assert.Equal("", await _stringOps.TrimAsync("   "));
            Assert.Equal("", await _stringOps.TrimAsync(null));
        }
        
        [Fact]
        public async Task StringOperations_ToUpperAsync_ShouldConvert()
        {
            // Act & Assert
            Assert.Equal("TEST", await _stringOps.ToUpperAsync("test"));
            Assert.Equal("", await _stringOps.ToUpperAsync(null));
        }
        
        [Fact]
        public async Task StringOperations_ToLowerAsync_ShouldConvert()
        {
            // Act & Assert
            Assert.Equal("test", await _stringOps.ToLowerAsync("TEST"));
            Assert.Equal("", await _stringOps.ToLowerAsync(null));
        }
        
        [Fact]
        public async Task StringOperations_SplitAsync_ShouldSplit()
        {
            // Act
            var result = await _stringOps.SplitAsync("a,b,c", ",");
            
            // Assert
            Assert.Equal(new[] { "a", "b", "c" }, result);
        }
        
        [Fact]
        public async Task StringOperations_JoinAsync_ShouldJoin()
        {
            // Arrange
            var items = new List<string> { "a", "b", "c" };
            
            // Act
            var result = await _stringOps.JoinAsync(items, ",");
            
            // Assert
            Assert.Equal("a,b,c", result);
        }
        
        [Fact]
        public async Task StringOperations_ReplaceAsync_ShouldReplace()
        {
            // Act
            var result = await _stringOps.ReplaceAsync("Hello World", "World", "Universe");
            
            // Assert
            Assert.Equal("Hello Universe", result);
        }
        
        [Fact]
        public async Task StringOperations_ContainsAsync_ShouldReturnCorrectResult()
        {
            // Act & Assert
            Assert.True(await _stringOps.ContainsAsync("Hello World", "World"));
            Assert.False(await _stringOps.ContainsAsync("Hello World", "Universe"));
            Assert.False(await _stringOps.ContainsAsync(null, "test"));
        }
        
        [Fact]
        public async Task StringOperations_StartsWithAsync_ShouldReturnCorrectResult()
        {
            // Act & Assert
            Assert.True(await _stringOps.StartsWithAsync("Hello World", "Hello"));
            Assert.False(await _stringOps.StartsWithAsync("Hello World", "World"));
            Assert.False(await _stringOps.StartsWithAsync(null, "test"));
        }
        
        [Fact]
        public async Task StringOperations_EndsWithAsync_ShouldReturnCorrectResult()
        {
            // Act & Assert
            Assert.True(await _stringOps.EndsWithAsync("Hello World", "World"));
            Assert.False(await _stringOps.EndsWithAsync("Hello World", "Hello"));
            Assert.False(await _stringOps.EndsWithAsync(null, "test"));
        }
        
        [Fact]
        public async Task StringOperations_SubstringAsync_ShouldExtract()
        {
            // Act & Assert
            Assert.Equal("World", await _stringOps.SubstringAsync("Hello World", 6));
            Assert.Equal("lo", await _stringOps.SubstringAsync("Hello World", 3, 2));
            Assert.Equal("", await _stringOps.SubstringAsync(null, 0));
        }
    }
}
