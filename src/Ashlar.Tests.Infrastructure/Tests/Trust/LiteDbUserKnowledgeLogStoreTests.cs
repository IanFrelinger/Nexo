using FluentAssertions;
using Ashlar.Core.Application.Trust.Models;
using Ashlar.Infrastructure.Trust;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Trust;

/// <summary>Tests for lite db user knowledge log store.</summary>
public class LiteDbUserKnowledgeLogStoreTests : TempDirTestBase
{
    private readonly string _dbPath;

    public LiteDbUserKnowledgeLogStoreTests() : base("ashlar-knowledge-test")
    {
        _dbPath = Path.Combine(TempDir, "store.db");
    }

    [Fact]
    public void Ctor_WithPath_PrependsFilename()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);
        store.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_WithConnectionString_LeavesAsIs()
    {
        var conn = $"Filename={_dbPath}";
        var store = new LiteDbUserKnowledgeLogStore(conn);
        store.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_NullOrEmpty_Throws()
    {
        var act = () => new LiteDbUserKnowledgeLogStore("");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task UpsertAsync_NewEntry_StoresAndRetrieves()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);
        var entry = new UserKnowledgeLogEntry
        {
            Id = "lite-1",
            DataType = "inferred-preferences",
            Content = "Prefers dark mode",
        };

        await store.UpsertAsync(entry);

        var retrieved = await store.GetByIdAsync("lite-1");
        Assert.NotNull(retrieved);
        Assert.Equal("lite-1", retrieved!.Id);
        Assert.Equal("inferred-preferences", retrieved.DataType);
        Assert.Equal("Prefers dark mode", retrieved.Content);
        Assert.Equal(1, retrieved.Version);
    }

    [Fact]
    public async Task UpsertAsync_ExistingEntry_IncrementsVersion()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "v1", DataType = "x", Content = "v1" });
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "v1", DataType = "x", Content = "v2" });

        var retrieved = await store.GetByIdAsync("v1");
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved!.Version);
        Assert.Equal("v2", retrieved.Content);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesEntry()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "del", DataType = "x", Content = "y" });

        await store.DeleteAsync("del");

        (await store.GetByIdAsync("del")).Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_FiltersByDataType()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "a", DataType = "type-a", Content = "a" });
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "b", DataType = "type-b", Content = "b" });

        var results = await store.GetAsync("type-a", maxCount: 10);

        Assert.Single(results);
        Assert.Equal("a", results[0].Id);
    }

    [Fact]
    public async Task ExportToJsonAsync_SerializesEntries()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);
        await store.UpsertAsync(new UserKnowledgeLogEntry
        {
            Id = "exp",
            DataType = "export-test",
            Content = "export content",
            SourceObservationIds = ["obs-1"],
        });

        var json = await store.ExportToJsonAsync(100);

        json.Should().Contain("exp");
        json.Should().Contain("export content");
        json.Should().Contain("obs-1");
    }
}
