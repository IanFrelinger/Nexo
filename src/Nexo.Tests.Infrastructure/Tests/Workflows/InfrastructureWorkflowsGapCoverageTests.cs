using FluentAssertions;
using Nexo.Core.Domain.Clusters;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Workflows;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Workflows;

/// <summary>Tests for infrastructure workflows gap coverage.</summary>
public class InfrastructureWorkflowsGapCoverageTests
{
    [Fact]
    public async Task ClusterStoreAdapter_delegates_to_cluster_registry()
    {
        var registry = new InMemoryClusterRegistry();
        var cluster = new Cluster { Id = "c1", Name = "Combat", Description = "test" };
        registry.Register(cluster);

        var store = new ClusterStoreAdapter(registry);
        (await store.GetByIdAsync("c1"))!.Name.Should().Be("Combat");
        (await store.GetByIdAsync("missing")).Should().BeNull();
    }

    [Fact]
    public async Task ClusterStoreAdapter_uses_case_insensitive_registry_lookup()
    {
        var registry = new InMemoryClusterRegistry();
        registry.Register(new Cluster { Id = "C1", Name = "Combat", Description = "test" });

        var store = new ClusterStoreAdapter(registry);
        (await store.GetByIdAsync("c1"))!.Name.Should().Be("Combat");
    }

    [Fact]
    public async Task ClusterStoreAdapter_forwards_cancellation_token()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var registry = new InMemoryClusterRegistry();
        var store = new ClusterStoreAdapter(registry);

        var act = async () => await store.GetByIdAsync("any", cts.Token);

        await act.Should().NotThrowAsync();
    }

    /// <summary>Tests for in memory cluster registry.</summary>
    private sealed class InMemoryClusterRegistry : IClusterRegistry
    {
        private readonly Dictionary<string, Cluster> _clusters = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Gets the value.</summary>
        /// <param name="id">Id.</param>
        public Cluster? Get(string id) => _clusters.GetValueOrDefault(id);

        /// <summary>Gets all.</summary>
        public IReadOnlyList<Cluster> GetAll() => _clusters.Values.ToList();

        /// <summary>Register.</summary>
        /// <param name="cluster">Cluster.</param>
        public void Register(Cluster cluster) => _clusters[cluster.Id] = cluster;

        /// <summary>Unregister.</summary>
        /// <param name="id">Id.</param>
        public void Unregister(string id) => _clusters.Remove(id);
    }
}
