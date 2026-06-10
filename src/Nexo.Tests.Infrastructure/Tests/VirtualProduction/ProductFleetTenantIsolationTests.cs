using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.API.Security;
using Nexo.Core.Application.Copilot.Models;
using Nexo.Core.Application.Copilot.Ports;
using Nexo.Core.Application.Product.Models;
using Nexo.Core.Application.Product.Ports;
using Nexo.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// Product Fleet Phase 0.1 — cross-tenant copilot task access must fail at the HTTP layer.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class ProductFleetTenantIsolationTests : IClassFixture<NexoApiWebApplicationFactory>
{
    private readonly NexoApiWebApplicationFactory _factory;

    public ProductFleetTenantIsolationTests(NexoApiWebApplicationFactory factory) => _factory = factory;

    [Fact(Timeout = 120000)]
    public async Task GET_copilot_task_returns_not_found_for_other_tenant()
    {
        const string taskId = "phase0-cross-tenant-task";
        using (var scope = _factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ICopilotTaskStore>();
            await store.StoreAsync(new CopilotTaskRecord
            {
                TaskId = taskId,
                TenantId = "tenant-a",
                Task = "secret",
                SubmittedAt = DateTimeOffset.UtcNow,
                Success = true,
            });
        }

        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/copilot/tasks/{taskId}");
        request.Headers.TryAddWithoutValidation(NexoHttpTenant.TenantHeaderName, "tenant-b");

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 120000)]
    public async Task GET_copilot_tasks_lists_only_resolved_tenant()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ICopilotTaskStore>();
            var now = DateTimeOffset.UtcNow;
            await store.StoreAsync(new CopilotTaskRecord
            {
                TaskId = "list-a",
                TenantId = "tenant-a",
                Task = "a",
                SubmittedAt = now,
                Success = true,
            });
            await store.StoreAsync(new CopilotTaskRecord
            {
                TaskId = "list-b",
                TenantId = "tenant-b",
                Task = "b",
                SubmittedAt = now,
                Success = true,
            });
        }

        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/copilot/tasks?maxCount=50");
        request.Headers.TryAddWithoutValidation(NexoHttpTenant.TenantHeaderName, "tenant-a");

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var tasks = await response.Content.ReadFromJsonAsync<List<CopilotTaskRecord>>();
        tasks.Should().NotBeNull();
        tasks!.Should().Contain(t => t.TaskId == "list-a");
        tasks.Should().NotContain(t => t.TaskId == "list-b");
    }

    [Fact(Timeout = 120000)]
    public async Task GET_usage_summary_returns_counters_for_resolved_tenant_only()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var usage = scope.ServiceProvider.GetRequiredService<ITenantUsageStore>();
            usage.RecordJobSubmitted("tenant-a");
            usage.RecordJobCompleted("tenant-a", success: true);
            usage.RecordJobSubmitted("tenant-b");
            usage.RecordJobCompleted("tenant-b", success: false);
        }

        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/usage/summary?hours=24");
        request.Headers.TryAddWithoutValidation(NexoHttpTenant.TenantHeaderName, "tenant-a");

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<TenantUsageSummary>();
        summary.Should().NotBeNull();
        summary!.TenantId.Should().Be("tenant-a");
        summary.JobsSubmitted.Should().BeGreaterThanOrEqualTo(1);
        summary.JobsSucceeded.Should().BeGreaterThanOrEqualTo(1);
    }
}
