using System.Reflection;
using Ashlar.Infrastructure.Testing;
using Xunit;

namespace Ashlar.Tests.Application.Framework;

/// <summary>
/// Bridges <c>UnitTestBase</c> suites in this assembly to xUnit / VSTest via <see cref="UnitTestFrameworkBridge"/>.
/// </summary>
[Trait("Category", "ProdStyle")]
public sealed class UnitTestBridgeTests
{
    public static TheoryData<Type> UnitTestTypes { get; } = BuildTheoryData();

    private static TheoryData<Type> BuildTheoryData()
    {
        var data = new TheoryData<Type>();
        foreach (var t in UnitTestFrameworkBridge.DiscoverUnitTestTypesFromAssembly(
                     Assembly.GetExecutingAssembly()))
        {
            data.Add(t);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(UnitTestTypes))]
    public async Task Framework_unit_test_passes(Type testType)
    {
        await UnitTestFrameworkBridge.ExecuteUnitTestAsync(testType);
    }
}
