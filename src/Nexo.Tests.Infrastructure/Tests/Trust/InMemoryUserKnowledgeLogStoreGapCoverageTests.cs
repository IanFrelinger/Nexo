using FluentAssertions;
using Nexo.Core.Application.Trust.Models;
using Nexo.Infrastructure.Trust;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

public sealed class InMemoryUserKnowledgeLogStoreGapCoverageTests
{
    [Fact]
    public async Task GetByIdAsync_is_case_insensitive()
    {
        var store = new InMemoryUserKnowledgeLogStore();
        await store.UpsertAsync(new UserKnowledgeLogEntry
        {
            Id = "Case-Id",
            DataType = "x",
            Content = "value",
        });

        (await store.GetByIdAsync("case-id")).Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_missing_id_is_noop()
    {
        var store = new InMemoryUserKnowledgeLogStore();

        var act = async () => await store.DeleteAsync("missing");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_respects_max_count()
    {
        var store = new InMemoryUserKnowledgeLogStore();
        for (var i = 0; i < 5; i++)
        {
            await store.UpsertAsync(new UserKnowledgeLogEntry
            {
                Id = $"mem-{i}",
                DataType = "batch",
                Content = $"body-{i}",
            });
        }

        var results = await store.GetAsync(maxCount: 2);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExportToJsonAsync_excludes_soft_deleted_entries()
    {
        var store = new InMemoryUserKnowledgeLogStore();
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "keep", DataType = "x", Content = "keep-me" });
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "drop", DataType = "x", Content = "drop-me" });
        await store.DeleteAsync("drop");

        var json = await store.ExportToJsonAsync(10);

        json.Should().Contain("keep");
        json.Should().NotContain("drop-me");
    }

    [Fact]
    public async Task UpsertAsync_preserves_created_at_on_update()
    {
        var store = new InMemoryUserKnowledgeLogStore();
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await store.UpsertAsync(new UserKnowledgeLogEntry
        {
            Id = "created",
            DataType = "x",
            Content = "v1",
            CreatedAt = createdAt,
        });
        await store.UpsertAsync(new UserKnowledgeLogEntry
        {
            Id = "created",
            DataType = "x",
            Content = "v2",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var retrieved = await store.GetByIdAsync("created");
        retrieved!.CreatedAt.Should().Be(createdAt);
        retrieved.Content.Should().Be("v2");
    }
}
