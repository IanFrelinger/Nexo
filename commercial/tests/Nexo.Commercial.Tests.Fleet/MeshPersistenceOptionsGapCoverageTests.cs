using FluentAssertions;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for mesh persistence options gap coverage.</summary>
public sealed class MeshPersistenceOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_mesh_persistence_configuration()
    {
        var options = new MeshPersistenceOptions();

        MeshPersistenceOptions.SectionPath.Should().Be("Nexo:Mesh:Persistence");
        options.Provider.Should().Be("InMemory");
        options.DatabasePath.Should().Be("mesh-director.db");
    }

    [Theory]
    [InlineData("LiteDb", true)]
    [InlineData("litedb", true)]
    [InlineData("InMemory", false)]
    [InlineData("unknown", false)]
    public void IsLiteDb_recognizes_provider_names(string provider, bool expected)
    {
        MeshPersistenceOptions.IsLiteDb(provider).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("InMemory", true)]
    [InlineData("LiteDb", true)]
    [InlineData("postgres", false)]
    public void IsKnownProvider_accepts_supported_values(string? provider, bool expected)
    {
        MeshPersistenceOptions.IsKnownProvider(provider).Should().Be(expected);
    }
}
