using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Data.Interfaces;
using Nexo.Feature.Data.Services;
using Xunit;

namespace Nexo.Feature.Data.Tests.Services
{
    /// <summary>
    /// Error handling tests for Repository
    /// </summary>
    public partial class RepositoryTests
    {
        [Fact]
        public void Repository_Constructor_WithNullDatabaseProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Repository<TestEntity, Guid>(null, _mockCacheService.Object, _mockQueryBuilder.Object, _mockLogger.Object));
        }

        [Fact]
        public void Repository_Constructor_WithNullCacheService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Repository<TestEntity, Guid>(_mockDatabaseProvider.Object, null, _mockQueryBuilder.Object, _mockLogger.Object));
        }

        [Fact]
        public void Repository_Constructor_WithNullQueryBuilder_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Repository<TestEntity, Guid>(_mockDatabaseProvider.Object, _mockCacheService.Object, null, _mockLogger.Object));
        }

        [Fact]
        public void Repository_Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Repository<TestEntity, Guid>(_mockDatabaseProvider.Object, _mockCacheService.Object, _mockQueryBuilder.Object, null));
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await _repository.DeleteAsync(id);

            // Assert
            Assert.False(result);
        }
    }
}