using FluentAssertions;
using Nexo.Core.Application.Copilot.Models;
using Nexo.Infrastructure.Copilot;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Copilot;

[Trait("Category", "Integration")]
public sealed class CopilotTaskStoreTests : TempDirTestBase
{
    private readonly string _dbPath;

    public CopilotTaskStoreTests() : base("nexo-copilot-task-store")
    {
        _dbPath = Path.Combine(TempDir, "copilot-tasks.db");
    }

    [Fact]
    public async Task StoreAndQuery_RoundTrip()
    {
        var store = new LiteDbCopilotTaskStore(_dbPath);
        var now = DateTimeOffset.UtcNow;
        var record = new CopilotTaskRecord
        {
            TaskId = "task-1",
            Task = "Refactor controller",
            SubmittedAt = now,
            CompletedAt = now.AddMinutes(5),
            Success = true,
            Summary = "Refactored successfully",
            Error = null
        };

        await store.StoreAsync(record);

        var results = await store.QueryAsync(maxCount: 10);
        results.Should().ContainSingle();
        var retrieved = results[0];
        retrieved.TaskId.Should().Be("task-1");
        retrieved.Task.Should().Be("Refactor controller");
        retrieved.SubmittedAt.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(1));
        retrieved.CompletedAt.Should().BeCloseTo(now.AddMinutes(5), TimeSpan.FromMilliseconds(1));
        retrieved.Success.Should().BeTrue();
        retrieved.Summary.Should().Be("Refactored successfully");
        retrieved.Error.Should().BeNull();
    }

    [Fact]
    public async Task Query_Since_FiltersOldTasks()
    {
        var store = new LiteDbCopilotTaskStore(_dbPath);
        var old = DateTimeOffset.UtcNow.AddHours(-3);
        var recent = DateTimeOffset.UtcNow;

        await store.StoreAsync(new CopilotTaskRecord
        {
            TaskId = "old-task",
            Task = "Old task",
            SubmittedAt = old,
            Success = false
        });
        await store.StoreAsync(new CopilotTaskRecord
        {
            TaskId = "recent-task",
            Task = "Recent task",
            SubmittedAt = recent,
            Success = true
        });

        var results = await store.QueryAsync(maxCount: 10, since: DateTimeOffset.UtcNow.AddHours(-1));
        results.Should().ContainSingle();
        results[0].TaskId.Should().Be("recent-task");
    }

    [Fact]
    public async Task Store_UpdatesExistingTask()
    {
        var store = new LiteDbCopilotTaskStore(_dbPath);
        var now = DateTimeOffset.UtcNow;

        await store.StoreAsync(new CopilotTaskRecord
        {
            TaskId = "upsert-task",
            Task = "Initial task",
            SubmittedAt = now,
            Success = false,
            Summary = null,
            Error = "in progress"
        });

        await store.StoreAsync(new CopilotTaskRecord
        {
            TaskId = "upsert-task",
            Task = "Initial task",
            SubmittedAt = now,
            CompletedAt = now.AddMinutes(10),
            Success = true,
            Summary = "Done",
            Error = null
        });

        var results = await store.QueryAsync(maxCount: 10);
        results.Should().ContainSingle("upsert should replace, not duplicate");
        results[0].Success.Should().BeTrue();
        results[0].Summary.Should().Be("Done");
        results[0].Error.Should().BeNull();
    }

    [Fact]
    public async Task Query_MaxCount_LimitsResults()
    {
        var store = new LiteDbCopilotTaskStore(_dbPath);
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 5; i++)
        {
            await store.StoreAsync(new CopilotTaskRecord
            {
                TaskId = $"task-{i}",
                Task = $"Task {i}",
                SubmittedAt = now.AddMinutes(i),
                Success = true
            });
        }

        var results = await store.QueryAsync(maxCount: 2);
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsCorrectTask()
    {
        var store = new LiteDbCopilotTaskStore(_dbPath);
        var now = DateTimeOffset.UtcNow;

        await store.StoreAsync(new CopilotTaskRecord
        {
            TaskId = "alpha",
            Task = "Alpha task",
            SubmittedAt = now,
            Success = true
        });
        await store.StoreAsync(new CopilotTaskRecord
        {
            TaskId = "beta",
            Task = "Beta task",
            SubmittedAt = now.AddMinutes(1),
            Success = false,
            Error = "Something went wrong"
        });

        var alpha = await store.GetByIdAsync("alpha");
        alpha.Should().NotBeNull();
        alpha!.TaskId.Should().Be("alpha");
        alpha.Task.Should().Be("Alpha task");

        var beta = await store.GetByIdAsync("beta");
        beta.Should().NotBeNull();
        beta!.Error.Should().Be("Something went wrong");

        var missing = await store.GetByIdAsync("nonexistent");
        missing.Should().BeNull();
    }

    [Fact]
    public async Task Query_FiltersByTenant()
    {
        var store = new LiteDbCopilotTaskStore(_dbPath);
        var now = DateTimeOffset.UtcNow;

        await store.StoreAsync(new CopilotTaskRecord
        {
            TenantId = "tenant-a",
            TaskId = "a1",
            Task = "A task",
            SubmittedAt = now,
            Success = true
        });
        await store.StoreAsync(new CopilotTaskRecord
        {
            TenantId = "tenant-b",
            TaskId = "b1",
            Task = "B task",
            SubmittedAt = now.AddMinutes(1),
            Success = true
        });

        var aOnly = await store.QueryAsync(maxCount: 10, since: null, tenantId: "tenant-a");
        aOnly.Should().ContainSingle();
        aOnly[0].TaskId.Should().Be("a1");

        var defaultTenant = await store.QueryAsync(maxCount: 10, since: null, tenantId: "default");
        defaultTenant.Should().BeEmpty();
    }
}
