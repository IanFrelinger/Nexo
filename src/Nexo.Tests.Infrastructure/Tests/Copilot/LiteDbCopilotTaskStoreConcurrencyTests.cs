using FluentAssertions;
using Nexo.Core.Application.Copilot.Models;
using Nexo.Infrastructure.Copilot;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Copilot;

/// <summary>
/// Concurrent writes to the copilot task store must all land.
/// </summary>
/// <remarks>
/// Found by UAT tier 10. LiteDB's default connection mode is Direct, which takes an EXCLUSIVE file
/// lock for the lifetime of a <c>LiteDatabase</c> instance, and every method on the store opens one
/// per call. Two API submissions in flight at once therefore raced: the second threw
/// <c>IOException("The process cannot access the file … because it is being used by another
/// process")</c> out of a request that had ALREADY RUN the task. The work happened and its record was
/// lost — the one failure "every action on the record" cannot absorb, and one that no check counting
/// HTTP 200s would notice.
/// </remarks>
[Trait("Category", "Integration")]
public sealed class LiteDbCopilotTaskStoreConcurrencyTests : TempDirTestBase
{
    private readonly string _dbPath;

    public LiteDbCopilotTaskStoreConcurrencyTests() : base("nexo-copilot-task-store-concurrency")
    {
        _dbPath = Path.Combine(TempDir, "copilot-tasks-concurrent.db");
    }

    private static CopilotTaskRecord Record(string id) => new()
    {
        TaskId = id,
        TenantId = "default",
        Task = $"concurrent probe {id}",
        SubmittedAt = DateTimeOffset.UtcNow,
        CompletedAt = DateTimeOffset.UtcNow,
        Success = true,
        Summary = "1 agent(s) executed",
    };

    [Fact]
    public async Task Concurrent_writers_all_land_and_none_throw()
    {
        var store = new LiteDbCopilotTaskStore(_dbPath);
        var ids = Enumerable.Range(0, 16).Select(i => $"task-{i:D2}").ToArray();

        // Same shape as the API under load: independent writers, no coordination between them.
        var writes = ids.Select(id => Task.Run(() => store.StoreAsync(Record(id))));
        var act = async () => await Task.WhenAll(writes);

        await act.Should().NotThrowAsync(
            "concurrent submissions must not collide on the store's file lock");

        var stored = await store.QueryAsync(maxCount: ids.Length * 4);
        stored.Select(r => r.TaskId).Should().BeEquivalentTo(
            ids,
            "every task that ran must be on the record exactly once, however many arrived at once");
    }

    [Fact]
    public async Task A_second_store_on_the_same_file_can_write_too()
    {
        // The CLI and the API can both hold a store over the same file. Direct mode made the second
        // one fail to open at all; shared mode serialises them.
        var first = new LiteDbCopilotTaskStore(_dbPath);
        var second = new LiteDbCopilotTaskStore(_dbPath);

        await first.StoreAsync(Record("from-first"));
        var act = async () => await second.StoreAsync(Record("from-second"));

        await act.Should().NotThrowAsync("a second store over the same file must not be locked out");

        var stored = await second.QueryAsync(maxCount: 20);
        stored.Select(r => r.TaskId).Should().Contain(new[] { "from-first", "from-second" });
    }
}
