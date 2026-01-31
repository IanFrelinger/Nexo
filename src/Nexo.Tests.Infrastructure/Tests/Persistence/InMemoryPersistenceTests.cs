using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Persistence.Models;
using Nexo.Core.Application.Persistence.Ports;
using Nexo.Infrastructure.Persistence;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Persistence;

/// <summary>
/// Tests for in-memory persistence: InMemoryUnitOfWork and InMemoryRepository.
/// Validates the persistence port contract and default implementation.
/// </summary>
public class InMemoryPersistenceTests
{
    private sealed class TestEntity : IEntity<Guid>
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task GetByIdAsync_WhenEmpty_ReturnsNull()
    {
        var uow = new InMemoryUnitOfWork();
        var repo = uow.GetRepository<TestEntity, Guid>();

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsEntity()
    {
        var uow = new InMemoryUnitOfWork();
        var repo = uow.GetRepository<TestEntity, Guid>();
        var id = Guid.NewGuid();
        var entity = new TestEntity { Id = id, Name = "Test" };

        await repo.AddAsync(entity);
        var found = await repo.GetByIdAsync(id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(id);
        found.Name.Should().Be("Test");
    }

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_ReturnsSingleItem()
    {
        var uow = new InMemoryUnitOfWork();
        var repo = uow.GetRepository<TestEntity, Guid>();
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Only" };

        await repo.AddAsync(entity);
        var all = await repo.GetAllAsync();

        all.Should().HaveCount(1);
        all[0].Name.Should().Be("Only");
    }

    [Fact]
    public async Task UpdateAsync_ThenGetByIdAsync_ReturnsUpdatedEntity()
    {
        var uow = new InMemoryUnitOfWork();
        var repo = uow.GetRepository<TestEntity, Guid>();
        var id = Guid.NewGuid();
        await repo.AddAsync(new TestEntity { Id = id, Name = "Original" });

        var updated = new TestEntity { Id = id, Name = "Updated" };
        await repo.UpdateAsync(updated);
        var found = await repo.GetByIdAsync(id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task RemoveAsync_ThenGetByIdAsync_ReturnsNull()
    {
        var uow = new InMemoryUnitOfWork();
        var repo = uow.GetRepository<TestEntity, Guid>();
        var id = Guid.NewGuid();
        await repo.AddAsync(new TestEntity { Id = id, Name = "ToRemove" });

        await repo.RemoveAsync(id);
        var found = await repo.GetByIdAsync(id);

        found.Should().BeNull();
    }

    [Fact]
    public void GetRepository_SameEntityKeyType_ReturnsSameInstanceWithinUow()
    {
        var uow = new InMemoryUnitOfWork();
        var repo1 = uow.GetRepository<TestEntity, Guid>();
        var repo2 = uow.GetRepository<TestEntity, Guid>();

        repo1.Should().BeSameAs(repo2);
    }

    [Fact]
    public void GetRepository_DifferentEntityType_ReturnsDifferentInstances()
    {
        var uow = new InMemoryUnitOfWork();
        var repoGuid = uow.GetRepository<TestEntity, Guid>();
        var repoInt = uow.GetRepository<IntEntity, int>();

        repoGuid.Should().NotBeSameAs(repoInt);
    }

    [Fact]
    public async Task CommitAsync_DoesNotThrow()
    {
        var uow = new InMemoryUnitOfWork();
        await uow.Invoking(x => x.CommitAsync()).Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_DoesNotThrow()
    {
        var uow = new InMemoryUnitOfWork();
        await uow.Invoking(x => x.DisposeAsync().AsTask()).Should().NotThrowAsync();
    }

    [Fact]
    public async Task TwoUnitOfWorkInstances_HaveIsolatedStores()
    {
        var uow1 = new InMemoryUnitOfWork();
        var uow2 = new InMemoryUnitOfWork();
        var id = Guid.NewGuid();
        var entity = new TestEntity { Id = id, Name = "InUow1" };

        await uow1.GetRepository<TestEntity, Guid>().AddAsync(entity);
        var foundInUow2 = await uow2.GetRepository<TestEntity, Guid>().GetByIdAsync(id);

        foundInUow2.Should().BeNull("each UoW has its own store");
    }

    [Fact]
    public async Task AddAsync_WhenEntityDoesNotImplementIEntity_Throws()
    {
        var uow = new InMemoryUnitOfWork();
        var repo = uow.GetRepository<BadEntity, string>();

        await repo.Invoking(r => r.AddAsync(new BadEntity { Name = "NoId" }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must implement IEntity*");
    }

    [Fact]
    public async Task AddAsync_DuplicateKey_Throws()
    {
        var uow = new InMemoryUnitOfWork();
        var repo = uow.GetRepository<TestEntity, Guid>();
        var id = Guid.NewGuid();
        await repo.AddAsync(new TestEntity { Id = id, Name = "First" });

        await repo.Invoking(r => r.AddAsync(new TestEntity { Id = id, Name = "Duplicate" }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CancellationToken_WhenCanceled_Throws()
    {
        var uow = new InMemoryUnitOfWork();
        var repo = uow.GetRepository<TestEntity, Guid>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await repo.Invoking(r => r.GetByIdAsync(Guid.NewGuid(), cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void AddNexoPersistence_RegistersIUnitOfWorkScoped()
    {
        var services = new ServiceCollection();
        services.AddNexoPersistence();
        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var uow1a = scope1.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var uow1b = scope1.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var uow2 = scope2.ServiceProvider.GetRequiredService<IUnitOfWork>();

        uow1a.Should().BeSameAs(uow1b, "scoped registration returns same instance per scope");
        uow1a.Should().NotBeSameAs(uow2, "different scopes get different instances");
    }

    /// <summary>
    /// Entity without IEntity for testing validation.
    /// </summary>
    private sealed class BadEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class IntEntity : IEntity<int>
    {
        public int Id { get; init; }
        public string Value { get; set; } = string.Empty;
    }
}
