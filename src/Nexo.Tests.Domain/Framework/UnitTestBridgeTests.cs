using Xunit;

namespace Nexo.Tests.Domain.Framework;

/// <summary>
/// Bridges Nexo <c>UnitTestBase</c> tests to xUnit so <c>dotnet test src/Nexo.Tests.Domain</c> runs the same
/// logic as the infrastructure <c>ITestRunner</c> used by the CLI.
/// </summary>
public sealed class UnitTestBridgeTests
{
    public static TheoryData<Type> UnitTestTypes { get; } = BuildTheoryData();

    private static TheoryData<Type> BuildTheoryData()
    {
        var data = new TheoryData<Type>();
        foreach (var t in FrameworkTestExecutor.DiscoverUnitTestTypes())
        {
            data.Add(t);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(UnitTestTypes))]
    public async Task Framework_unit_test_passes(Type testType)
    {
        await FrameworkTestExecutor.ExecuteAsync(testType);
    }
}
