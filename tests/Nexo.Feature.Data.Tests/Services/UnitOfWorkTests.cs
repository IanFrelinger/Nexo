using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Data.Interfaces;
using Nexo.Feature.Data.Services;
using Xunit;

namespace Nexo.Feature.Data.Tests.Services;

/// <summary>
/// Tests for the Unit of Work service
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly Mock<ILogger<UnitOfWork>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _mockLogger = new Mock<ILogger<UnitOfWork>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _unitOfWork = new UnitOfWork(_context, _mockLogger.Object, _mockServiceProvider.Object);
    }

    [Fact]
    public void GetRepository_ShouldReturnRepositoryInstance()
    {
        // Act
        var repository = _unitOfWork.GetRepository<TestEntity, int>();

        // Assert
        Assert.NotNull(repository);
        Assert.IsType<Repository<TestEntity, int>>(repository);
    }

    [Fact]
    public void GetRepository_SameType_ShouldReturnSameInstance()
    {
        // Act
        var repository1 = _unitOfWork.GetRepository<TestEntity, int>();
        var repository2 = _unitOfWork.GetRepository<TestEntity, int>();

        // Assert
        Assert.Same(repository1, repository2);
    }

    [Fact]
    public void GetRepository_DifferentTypes_ShouldReturnDifferentInstances()
    {
        // Act
        var repository1 = _unitOfWork.GetRepository<TestEntity, int>();
        var repository2 = _unitOfWork.GetRepository<AnotherTestEntity, string>();

        // Assert
        Assert.NotSame(repository1, repository2);
    }

    [Fact]
    public void BeginTransaction_ShouldStartTransaction()
    {
        // Act
        var transaction = _unitOfWork.BeginTransaction();

        // Assert
        Assert.NotNull(transaction);
    }

    [Fact]
    public async Task CommitAsync_WithChanges_ShouldCommitSuccessfully()
    {
        // Arrange
        var repository = _unitOfWork.GetRepository<TestEntity, int>();
        var entity = new TestEntity
        {
            Name = "Test Entity",
            Description = "Test Description",
            IsActive = true
        };

        await repository.AddAsync(entity);

        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public async Task CommitAsync_NoChanges_ShouldReturnZero()
    {
        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Rollback_WithTransaction_ShouldRollbackChanges()
    {
        // Arrange
        var transaction = _unitOfWork.BeginTransaction();
        var repository = _unitOfWork.GetRepository<TestEntity, int>();
        var entity = new TestEntity
        {
            Name = "Test Entity",
            Description = "Test Description",
            IsActive = true
        };

        await repository.AddAsync(entity);
        // Don't commit - just add to the transaction

        // Act
        _unitOfWork.Rollback();

        // Assert
        var savedEntity = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "Test Entity");
        Assert.Null(savedEntity);
    }

    [Fact]
    public void HasChanges_WithChanges_ShouldReturnTrue()
    {
        // Arrange
        var repository = _unitOfWork.GetRepository<TestEntity, int>();
        var entity = new TestEntity
        {
            Name = "Test Entity",
            Description = "Test Description",
            IsActive = true
        };

        repository.AddAsync(entity);

        // Act
        var hasChanges = _unitOfWork.HasChanges();

        // Assert
        Assert.True(hasChanges);
    }

    [Fact]
    public void HasChanges_NoChanges_ShouldReturnFalse()
    {
        // Act
        var hasChanges = _unitOfWork.HasChanges();

        // Assert
        Assert.False(hasChanges);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    // Test entity for unit of work tests
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Another test entity for unit of work tests
    public class AnotherTestEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Test DbContext for unit of work tests
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        public DbSet<AnotherTestEntity> AnotherTestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<AnotherTestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Content).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
} 