using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Text;
using FluentAssertions;
using Ashlar.Infrastructure.Testing;
using Ashlar.Infrastructure.Testing.Docker;
using Ashlar.Infrastructure.Testing.ExecutionPlatform;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Testing;

/// <summary>Tests for unit test framework bridge gap coverage.</summary>
public sealed class UnitTestFrameworkBridgeGapCoverageTests
{
    [Fact]
    public void DiscoverUnitTestTypesFromAssembly_throws_for_null_assembly()
    {
        var act = () => UnitTestFrameworkBridge.DiscoverUnitTestTypesFromAssembly(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DiscoverUnitTestTypesFromAssembly_excludes_infrastructure_helper_types()
    {
        var assembly = typeof(UnitTestFrameworkBridgeGapCoverageTests).Assembly;
        var types = UnitTestFrameworkBridge.DiscoverUnitTestTypesFromAssembly(assembly);

        types.Should().NotContain(t => t.Name == "SimpleTestForRunner");
        types.Should().NotContain(t => t.Name == "DependencyWrappingArchitectureTests");
    }

    [Fact]
    public async Task ExecuteUnitTestAsync_throws_for_invalid_type()
    {
        var act = async () => await UnitTestFrameworkBridge.ExecuteUnitTestAsync(typeof(string));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("testType");
    }
}
