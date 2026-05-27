using FluentAssertions;
using Nexo.Core.Application.Copilot.Models;
using Nexo.Infrastructure.Copilot;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Copilot;

public sealed class LiteDbCopilotTaskStoreGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_blank_database_path()
    {
        var act = () => new LiteDbCopilotTaskStore("   ");

        act.Should().Throw<ArgumentNullException>().WithParameterName("pathOrConnectionString");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_blank_task_id()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = new LiteDbCopilotTaskStore(path);

            (await store.GetByIdAsync("  ")).Should().BeNull();
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task QueryAsync_clamps_max_count_and_filters_by_since()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = new LiteDbCopilotTaskStore(path);
            var now = DateTimeOffset.UtcNow;
            await store.StoreAsync(new CopilotTaskRecord
            {
                TaskId = "recent",
                Task = "recent task",
                SubmittedAt = now,
                Success = true,
            });
            await store.StoreAsync(new CopilotTaskRecord
            {
                TaskId = "old",
                Task = "old task",
                SubmittedAt = now.AddDays(-2),
                Success = false,
            });

            var results = await store.QueryAsync(maxCount: 0, since: now.AddHours(-1));

            results.Should().ContainSingle(r => r.TaskId == "recent");
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task QueryAsync_includes_default_tenant_records_with_null_or_empty_tenant()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = new LiteDbCopilotTaskStore(path);
            await store.StoreAsync(new CopilotTaskRecord
            {
                TaskId = "default-row",
                Task = "task",
                SubmittedAt = DateTimeOffset.UtcNow,
                TenantId = "",
            });

            var results = await store.QueryAsync(tenantId: "default");

            results.Should().ContainSingle(r => r.TaskId == "default-row");
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task StoreAsync_trims_long_tenant_id()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = new LiteDbCopilotTaskStore(path);
            var longTenant = new string('t', 200);
            var record = new CopilotTaskRecord
            {
                TaskId = "tenant-trim",
                Task = "task",
                SubmittedAt = DateTimeOffset.UtcNow,
                TenantId = longTenant,
            };

            await store.StoreAsync(record);

            (await store.GetByIdAsync("tenant-trim"))!.TenantId.Should().HaveLength(128);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void Accepts_filename_connection_string()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = new LiteDbCopilotTaskStore($"Filename={path}");

            store.Should().NotBeNull();
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string CreateTempDbPath()
        => Path.Combine(Path.GetTempPath(), "nexo-copilot-" + Guid.NewGuid().ToString("N") + ".db");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
