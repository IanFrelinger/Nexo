using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Abstractions.Database;
using Nexo.Abstractions.Transport;
using Xunit;

namespace Nexo.Tests.Kernel;

/// <summary>Tests for transport and database abstractions.</summary>
public class TransportAndDatabaseAbstractionsTests
{
    [Fact]
    public void AgentInvocationOptions_Default_has_expected_values()
    {
        var d = AgentInvocationOptions.Default;
        d.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        d.MaxRetries.Should().Be(3);
        d.TargetEndpoint.Should().BeNull();
    }

    [Fact]
    public void AgentInvocationOptions_record_round_trips()
    {
        var o = new AgentInvocationOptions(TimeSpan.FromMinutes(1), 5, "grpc://host");
        o.Timeout.Should().Be(TimeSpan.FromMinutes(1));
        o.MaxRetries.Should().Be(5);
        o.TargetEndpoint.Should().Be("grpc://host");
    }

    [Fact]
    public void DatabaseProvisionRequest_supports_all_optional_fields()
    {
        var req = new DatabaseProvisionRequest(
            DatabaseIsolationLevel.DedicatedContainer,
            DatabaseName: "db",
            ImageTag: "15",
            Password: "pw",
            AdminConnectionString: "Host=localhost",
            PostgresReadinessTimeout: TimeSpan.Zero,
            PostProvisionSqlBatches: new[] { "select 1" });
        req.Isolation.Should().Be(DatabaseIsolationLevel.DedicatedContainer);
        req.DatabaseName.Should().Be("db");
        req.ImageTag.Should().Be("15");
        req.Password.Should().Be("pw");
        req.AdminConnectionString.Should().Be("Host=localhost");
        req.PostgresReadinessTimeout.Should().Be(TimeSpan.Zero);
        req.PostProvisionSqlBatches.Should().ContainSingle();
    }
}
