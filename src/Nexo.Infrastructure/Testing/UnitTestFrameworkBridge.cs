using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Ports;
using Xunit;

namespace Nexo.Infrastructure.Testing;

/// <summary>
/// Runs concrete <see cref="UnitTestBase"/> types through <see cref="ITestRunner"/> so they appear in VSTest/xUnit.
/// </summary>
public static class UnitTestFrameworkBridge
{
    /// <summary>
    /// Discovers concrete <see cref="UnitTestBase"/> types in <paramref name="assembly"/> (public only; skips nested private types).
    /// </summary>
    public static IReadOnlyList<Type> DiscoverUnitTestTypesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var types = assembly
            .GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                t.IsSubclassOf(typeof(UnitTestBase)) &&
                (!t.IsNested || t.IsNestedPublic))
            .AsEnumerable();

        if (string.Equals(assembly.GetName().Name, "Nexo.Tests.Infrastructure", StringComparison.Ordinal))
        {
            types = types.Where(t =>
                !string.Equals(t.Name, "SimpleTestForRunner", StringComparison.Ordinal) &&
                !string.Equals(t.Name, "DependencyWrappingArchitectureTests", StringComparison.Ordinal));
        }

        if (string.Equals(assembly.GetName().Name, "Nexo.Tests.CLI", StringComparison.Ordinal))
        {
            types = types.Where(t => !string.Equals(t.Name, "DoctorCommandTests", StringComparison.Ordinal));
        }

        return types.OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Executes one <see cref="UnitTestBase"/> type using the same path as the CLI-hosted <see cref="ITestRunner"/>.
    /// </summary>
    public static async Task ExecuteUnitTestAsync(Type testType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(testType);
        if (!testType.IsSubclassOf(typeof(UnitTestBase)) || testType.IsAbstract)
        {
            throw new ArgumentException($"Type must be a concrete subclass of {nameof(UnitTestBase)}.", nameof(testType));
        }

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<ITestRunner, TestRunnerAdapter>();
        await using var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<ITestRunner>();

        var filter = testType.Name;
        var execution = await runner.RunTestsAsync(filter, cancellationToken: cancellationToken);

        var match = execution.Results.FirstOrDefault(r =>
            string.Equals(r.Name, testType.Name, StringComparison.Ordinal));

        if (match is null)
        {
            var ran = string.Join(", ", execution.Results.Select(r => r.Name));
            Assert.Fail(
                $"No result for '{testType.Name}'. TotalTests={execution.TotalTests}. Ran: [{ran}]");
        }

        AssertPassed(match);
    }

    private static void AssertPassed(Nexo.Core.Application.Common.Models.TestResult match)
    {
        if (match.Passed)
        {
            return;
        }

        var detail = string.IsNullOrEmpty(match.ErrorMessage)
            ? match.Message
            : match.ErrorMessage;
        var trace = string.IsNullOrEmpty(match.StackTrace) ? "" : Environment.NewLine + match.StackTrace;
        Assert.Fail($"Framework test '{match.Name}' failed: {detail}{trace}");
    }
}
