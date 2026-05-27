using FluentAssertions;
using Nexo.Core.Application.Trust.Models;
using Nexo.Infrastructure.Trust;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

public sealed class LiteDbUserKnowledgeLogStoreGapCoverageTests : TempDirTestBase
{
    private readonly string _dbPath;

    public LiteDbUserKnowledgeLogStoreGapCoverageTests()
        : base("nexo-knowledge-gap")
    {
        _dbPath = Path.Combine(TempDir, "gap.db");
    }

    [Fact]
    public void Constructor_rejects_null_or_whitespace_path()
    {
        var act = () => new LiteDbUserKnowledgeLogStore("   ");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_missing_entry()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);

        (await store.GetByIdAsync("missing")).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_missing_id_is_noop()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);

        var act = async () => await store.DeleteAsync("missing");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpsertAsync_preserves_created_at_on_update()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await store.UpsertAsync(new UserKnowledgeLogEntry
        {
            Id = "created-lite",
            DataType = "x",
            Content = "v1",
            CreatedAt = createdAt,
        });
        await store.UpsertAsync(new UserKnowledgeLogEntry
        {
            Id = "created-lite",
            DataType = "x",
            Content = "v2",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var retrieved = await store.GetByIdAsync("created-lite");
        retrieved!.CreatedAt.Should().Be(createdAt);
        retrieved.Content.Should().Be("v2");
        retrieved.Version.Should().Be(2);
    }

    [Fact]
    public async Task Constructor_accepts_filename_connection_string()
    {
        var store = new LiteDbUserKnowledgeLogStore($"Filename={_dbPath}");
        await store.UpsertAsync(new UserKnowledgeLogEntry { Id = "conn", DataType = "x", Content = "y" });

        (await store.GetByIdAsync("conn")).Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_respects_max_count_and_excludes_soft_deleted()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);
        for (var i = 0; i < 5; i++)
        {
            await store.UpsertAsync(new UserKnowledgeLogEntry
            {
                Id = $"entry-{i}",
                DataType = "batch",
                Content = $"content-{i}",
            });
        }

        await store.DeleteAsync("entry-0");

        var results = await store.GetAsync(maxCount: 2);

        results.Should().HaveCount(2);
        results.Should().NotContain(e => e.Id == "entry-0");
    }

    [Fact]
    public async Task ExportToMarkdownAsync_formats_persisted_entries()
    {
        var store = new LiteDbUserKnowledgeLogStore(_dbPath);
        await store.UpsertAsync(new UserKnowledgeLogEntry
        {
            Id = "md-lite",
            DataType = "notes",
            Content = "persisted markdown",
        });

        var markdown = await store.ExportToMarkdownAsync(10);

        markdown.Should().Contain("## md-lite");
        markdown.Should().Contain("persisted markdown");
    }
}
