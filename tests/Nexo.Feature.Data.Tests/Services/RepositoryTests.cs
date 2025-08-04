using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    /// Tests for repository functionality
    /// </summary>
    public class RepositoryTests
    {
        private readonly Mock<IDatabaseProvider> _mockDatabaseProvider;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<QueryBuilder> _mockQueryBuilder;
        private readonly Mock<ILogger<Repository<TestEntity, Guid>>> _mockLogger;
        private readonly Repository<TestEntity, Guid> _repository;

        public RepositoryTests()
        {
            _mockDatabaseProvider = new Mock<IDatabaseProvider>();
            _mockCacheService = new Mock<ICacheService>();
            _mockQueryBuilder = new Mock<QueryBuilder>(Mock.Of<ILogger<QueryBuilder>>());
            _mockLogger = new Mock<ILogger<Repository<TestEntity, Guid>>>();
            _repository = new Repository<TestEntity, Guid>(
                _mockDatabaseProvider.Object, 
                _mockCacheService.Object,
                _mockQueryBuilder.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Repository_Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var repository = new Repository<TestEntity, Guid>(
                _mockDatabaseProvider.Object, 
                _mockCacheService.Object,
                _mockQueryBuilder.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(repository);
        }

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
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedEntity = new TestEntity { Id = id, Name = "Test" };
            var queryResults = new List<TestEntity> { expectedEntity };

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<TestEntity>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResults);

            // Act
            var result = await _repository.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            var queryResults = new List<TestEntity>();

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<TestEntity>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResults);

            // Act
            var result = await _repository.GetByIdAsync(id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var expectedEntities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" }
            };

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<TestEntity>(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEntities);

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task FindAsync_WithPredicate_ReturnsMatchingEntities()
        {
            // Arrange
            var expectedEntities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "MatchingEntity" }
            };

            _mockQueryBuilder
                .Setup(x => x.BuildWhereClause(It.IsAny<Expression<Func<TestEntity, bool>>>()))
                .Returns(("Name = @p1", new Dictionary<string, object> { ["@p1"] = "MatchingEntity" }));

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<TestEntity>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEntities);

            // Act
            var result = await _repository.FindAsync(e => e.Name == "MatchingEntity");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ReturnsPagedResult()
        {
            // Arrange
            var page = 1;
            var pageSize = 10;
            var expectedEntities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" }
            };

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<TestEntity>(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEntities);

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<object>(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<object> { 2L });

            // Act
            var result = await _repository.GetPagedAsync(page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(page, result.Page);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task AddAsync_WithValidEntity_ReturnsEntity()
        {
            // Arrange
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "NewEntity" };

            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _repository.AddAsync(entity);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entity.Id, result.Id);
            Assert.Equal(entity.Name, result.Name);
        }

        [Fact]
        public async Task UpdateAsync_WithValidEntity_ReturnsEntity()
        {
            // Arrange
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "UpdatedEntity" };

            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _repository.UpdateAsync(entity);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entity.Id, result.Id);
            Assert.Equal(entity.Name, result.Name);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _repository.DeleteAsync(id);

            // Assert
            Assert.True(result);
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

        [Fact]
        public async Task DeleteAsync_WithValidEntity_ReturnsTrue()
        {
            // Arrange
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "EntityToDelete" };

            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _repository.DeleteAsync(entity);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<object>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<object> { 1 });

            // Act
            var result = await _repository.ExistsAsync(id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<object>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<object> { 0 });

            // Act
            var result = await _repository.ExistsAsync(id);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CountAsync_ReturnsTotalCount()
        {
            // Arrange
            var expectedCount = 5L;

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<object>(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<object> { expectedCount });

            // Act
            var result = await _repository.CountAsync();

            // Assert
            Assert.Equal(expectedCount, result);
        }

        [Fact]
        public async Task CountAsync_WithPredicate_ReturnsFilteredCount()
        {
            // Arrange
            var expectedCount = 3L;

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<object>(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<object> { expectedCount });

            // Act
            var result = await _repository.CountAsync(e => e.Name == "Test");

            // Assert
            Assert.Equal(expectedCount, result);
        }

        [Fact]
        public async Task GetOrderedAsync_WithAscendingOrder_ReturnsOrderedEntities()
        {
            // Arrange
            var expectedEntities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "A" },
                new TestEntity { Id = Guid.NewGuid(), Name = "B" }
            };

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<TestEntity>(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEntities);

            // Act
            var result = await _repository.GetOrderedAsync(e => e.Name, true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetOrderedAsync_WithDescendingOrder_ReturnsOrderedEntities()
        {
            // Arrange
            var expectedEntities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "B" },
                new TestEntity { Id = Guid.NewGuid(), Name = "A" }
            };

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<TestEntity>(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEntities);

            // Act
            var result = await _repository.GetOrderedAsync(e => e.Name, false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetPagedOrderedAsync_WithValidParameters_ReturnsPagedOrderedResult()
        {
            // Arrange
            var page = 1;
            var pageSize = 10;
            var expectedEntities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" }
            };

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<TestEntity>(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEntities);

            _mockDatabaseProvider
                .Setup(x => x.QueryAsync<object>(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<object> { 2L });

            // Act
            var result = await _repository.GetPagedOrderedAsync(e => e.Name, page, pageSize, true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(page, result.Page);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task GetByIdAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.GetByIdAsync(id, cts.Token));
        }

        [Fact]
        public async Task GetAllAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.GetAllAsync(cts.Token));
        }

        [Fact]
        public async Task AddAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.AddAsync(entity, cts.Token));
        }

        [Fact]
        public async Task UpdateAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.UpdateAsync(entity, cts.Token));
        }

        [Fact]
        public async Task DeleteAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.DeleteAsync(id, cts.Token));
        }

        [Fact]
        public async Task CountAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _repository.CountAsync(cts.Token));
        }
    }

    /// <summary>
    /// Test entity for repository tests
    /// </summary>
    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
} 