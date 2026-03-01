using FluentAssertions;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Trust;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

public class InMemoryUserKnowledgeLogStoreTests
{
    [Fact]
    public async Task UpsertAsync_NewEntry_StoresAndRetrieves()
    {
        var store = new InMemoryUserKnowledgeLogStore();
        var entry = new UserKnowledgeLogEntry
        {
            Id = "test-1",
            DataType = "inferred-preferences",
            Content = "Prefers dark mode",
        };

        await store.UpsertAsync(entry);

        var retrieved = await store.GetByIdAsync("test-1");
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("test-1");
        retrieved.DataType.Should().Be("inferred-preferences");
        retrieved.Content.Should().Be("Prefers dark mode");
        retrieved.Version.Should().Be(1);
    }

    [Fact]
    public async Task UpsertAsync_ExistingEntry_IncrementsVersion()
    {
        var store = new InMemoryUserKnowledgeLogStore();
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "v1", DataType = "x", Content = "v1" });
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "v1", DataType = "x", Content = "v2" });

        var retrieved = await store.GetByIdAsync("v1");
        retrieved.Should().NotBeNull();
        retrieved!.Version.Should().Be(2);
        retrieved.Content.Should().Be("v2");
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesEntry()
    {
        var store = new InMemoryUserKnowledgeLogStore();
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "del", DataType = "x", Content = "y" });

        await store.DeleteAsync("del");

        (await store.GetByIdAsync("del")).Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_FiltersByDataType()
    {
        var store = new InMemoryUserKnowledgeLogStore();
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "a", DataType = "type-a", Content = "a" });
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "b", DataType = "type-b", Content = "b" });

        var results = await store.GetAsync("type-a", maxCount: 10);

        results.Should().HaveCount(1);
        results[0].Id.Should().Be("a");
    }

    [Fact]
    public async Task ExportToJsonAsync_SerializesEntries()
    {
        var store = new InMemoryUserKnowledgeLogStore();
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

    [Fact]
    public async Task ExportToMarkdownAsync_FormatsEntries()
    {
        var store = new InMemoryUserKnowledgeLogStore();
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "md", DataType = "md", Content = "markdown body" });

        var md = await store.ExportToMarkdownAsync(100);

        md.Should().Contain("# Nexo User Knowledge Log");
        md.Should().Contain("## md");
        md.Should().Contain("markdown body");
    }
}
