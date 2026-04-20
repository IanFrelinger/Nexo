using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Abstractions.Database;
using Nexo.Core.Application.Persistence.Ports;
using Nexo.Infrastructure.Persistence;
using Xunit;

namespace Nexo.Tests.Orchestration.Database;

public sealed class PostgresDatabaseProvisionerTests
{
    [Fact]
    public async Task CreateAsync_DedicatedContainer_UsesEphemeralLifecycle_AndDisposesStopsContainer()
    {
        var lifecycle = new Mock<IEphemeralDatabaseLifecycle>(MockBehavior.Strict);
        lifecycle
            .Setup(x => x.StartAsync(It.IsAny<EphemeralDbOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EphemeralDbResult("Host=localhost;Port=1;Database=nexo", "cid-42", 1));

        lifecycle
            .Setup(x => x.StopAsync("cid-42", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new PostgresDatabaseProvisioner(NullLogger<PostgresDatabaseProvisioner>.Instance, lifecycle.Object);

        await using var db = await sut.CreateAsync(
            new DatabaseProvisionRequest(DatabaseIsolationLevel.DedicatedContainer, DatabaseName: "nexo_test"));

        db.Isolation.Should().Be(DatabaseIsolationLevel.DedicatedContainer);
        db.BackendResourceId.Should().Be("cid-42");
        db.ConnectionString.Should().Contain("Host=localhost");

        await db.DisposeAsync();

        lifecycle.Verify(x => x.StopAsync("cid-42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DedicatedContainer_WithoutLifecycle_Throws()
    {
        var sut = new PostgresDatabaseProvisioner(NullLogger<PostgresDatabaseProvisioner>.Instance, ephemeralLifecycle: null);

        var act = () => sut.CreateAsync(new DatabaseProvisionRequest(DatabaseIsolationLevel.DedicatedContainer));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IEphemeralDatabaseLifecycle*");
    }
}
