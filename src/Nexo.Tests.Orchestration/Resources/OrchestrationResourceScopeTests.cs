using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nexo.Abstractions.Agents;
using Nexo.Abstractions.Database;
using Nexo.Abstractions.Resources;
using Nexo.Orchestration.Resources;
using Xunit;

namespace Nexo.Tests.Orchestration.Resources;

public sealed class OrchestrationResourceScopeTests
{
    [Fact]
    public async Task DisposeAsync_AgentsBeforeDatabases_DisposesInReverseRegistrationOrderPerPartition()
    {
        var order = new List<string>();
        var sut = new OrchestrationResourceScope(ResourceTeardownPolicy.AgentsBeforeDatabases);

        sut.Track(new RecordingResource(ProvisionedResourceKind.AgentHandle, "a1", order));
        sut.Track(new RecordingResource(ProvisionedResourceKind.AgentHandle, "a2", order));
        sut.Track(new RecordingResource(ProvisionedResourceKind.IsolatedDatabase, "d1", order));
        sut.Track(new RecordingResource(ProvisionedResourceKind.IsolatedDatabase, "d2", order));

        await sut.DisposeAsync();

        order.Should().Equal("a2", "a1", "d2", "d1");
    }

    [Fact]
    public async Task ResolveScope_FromDi_UsesOrchestrationResourceOptions()
    {
        var services = new ServiceCollection();
        services.AddOptions<OrchestrationResourceOptions>()
            .Configure(o => o.TeardownPolicy = ResourceTeardownPolicy.DatabasesBeforeAgents);
        services.AddLogging();
        services.AddScoped<OrchestrationResourceScope>(sp => new OrchestrationResourceScope(
            sp.GetRequiredService<IOptions<OrchestrationResourceOptions>>().Value.TeardownPolicy,
            sp.GetService<ILogger<OrchestrationResourceScope>>()));

        await using var sp = services.BuildServiceProvider();
        var scope = sp.GetRequiredService<OrchestrationResourceScope>();
        var order = new List<string>();
        scope.Track(new RecordingResource(ProvisionedResourceKind.AgentHandle, "a1", order));
        scope.Track(new RecordingResource(ProvisionedResourceKind.IsolatedDatabase, "d1", order));

        await scope.DisposeAsync();

        order.Should().Equal("d1", "a1");
    }

    [Fact]
    public async Task DisposeAsync_DatabasesBeforeAgents_DisposesDatabasesFirst()
    {
        var order = new List<string>();
        var sut = new OrchestrationResourceScope(ResourceTeardownPolicy.DatabasesBeforeAgents);

        sut.Track(new RecordingResource(ProvisionedResourceKind.AgentHandle, "a1", order));
        sut.Track(new RecordingResource(ProvisionedResourceKind.IsolatedDatabase, "d1", order));

        await sut.DisposeAsync();

        order.Should().Equal("d1", "a1");
    }

    [Fact]
    public async Task ProvisionedDatabaseResource_ForwardsDispose()
    {
        var db = new Mock<IIsolatedDatabase>(MockBehavior.Strict);
        db.SetupGet(x => x.ConnectionString).Returns("cs");
        db.SetupGet(x => x.Isolation).Returns(DatabaseIsolationLevel.DedicatedContainer);
        db.SetupGet(x => x.BackendResourceId).Returns((string?)null);
        db.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var sut = new ProvisionedDatabaseResource(db.Object);
        sut.Kind.Should().Be(ProvisionedResourceKind.IsolatedDatabase);

        await sut.DisposeAsync();
        db.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task ProvisionedAgentResource_ShutdownsHandle()
    {
        var handle = new Mock<IAgentHandle>(MockBehavior.Strict);
        handle.SetupGet(x => x.AgentId).Returns("x");
        handle.Setup(x => x.ShutdownAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await using var sut = new ProvisionedAgentResource(handle.Object, NullLogger<ProvisionedAgentResource>.Instance);
        await sut.DisposeAsync();

        handle.Verify(x => x.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class RecordingResource : IProvisionedResource
    {
        private readonly ProvisionedResourceKind _kind;
        private readonly string _id;
        private readonly List<string> _order;

        public RecordingResource(ProvisionedResourceKind kind, string id, List<string> order)
        {
            _kind = kind;
            _id = id;
            _order = order;
        }

        public ProvisionedResourceKind Kind => _kind;

        public ValueTask DisposeAsync()
        {
            _order.Add(_id);
            return ValueTask.CompletedTask;
        }
    }
}
