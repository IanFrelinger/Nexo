using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Nexo.API.Endpoints;
using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.Objectives;
using System.Reflection;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

public sealed class RuntimeStudioMetricsEndpointTests : IDisposable
{
    private readonly string _root;

    public RuntimeStudioMetricsEndpointTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nexo-rs-metrics-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task GetRuntimeStudioMetrics_Returns_counts_and_sla_hints()
    {
        var objRoot = Path.Combine(_root, "objectives");
        var objectives = new ObjectiveStore(objRoot);
        objectives.Add(new ObjectiveDocument
        {
            Id = "old",
            Title = "Stale",
            Body = "x",
            Priority = 1,
            CreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });

        var forgeRoot = Path.Combine(_root, "forge");
        var proposals = new ChangeProposalStore(forgeRoot);
        proposals.Add(new ChangeProposal
        {
            Id = "p1",
            TargetPath = "src/X.cs",
            NewContent = "//",
            Summary = "s"
        });

        var host = new StubHostEnvironment { ContentRootPath = _root };

        var method = typeof(NexoEndpoints).GetMethod(
            "GetRuntimeStudioMetricsAsync",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<IResult>)method!.Invoke(
            null,
            [objectives, proposals, host, CancellationToken.None])!;
        var result = await task;

        var dto = ExtractValue<RuntimeStudioMetricsResponse>(result);
        dto.ObjectivesRootLocation.Should().Be(objRoot);
        dto.ObjectiveSla.PendingCount.Should().Be(1);
        dto.ObjectiveSla.OldestPendingAgeHours.Should().NotBeNull();
        dto.ObjectiveSla.OldestPendingAgeHours!.Value.Should().BeGreaterThan(24_000);
        dto.ProposalsByStatus.Should().ContainKey("Proposed");
        dto.ProposalsByStatus["Proposed"].Should().Be(1);
        dto.ObservationsPath.Should().Contain(_root);
    }

    private static T ExtractValue<T>(IResult result) where T : class
    {
        var valueProperty = result.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        valueProperty.Should().NotBeNull();
        var value = valueProperty!.GetValue(result);
        value.Should().BeOfType<T>();
        return (T)value!;
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
