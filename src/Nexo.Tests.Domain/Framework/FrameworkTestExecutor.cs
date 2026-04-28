using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Ports;
using Nexo.Infrastructure.Testing;
using Xunit;

namespace Nexo.Tests.Domain.Framework;

/// <summary>
/// Runs <see cref="UnitTestBase"/> tests through <see cref="ITestRunner"/> so they participate in VSTest/xUnit.
/// </summary>
public static class FrameworkTestExecutor
{
    /// <summary>
    /// Discovers concrete <see cref="UnitTestBase"/> types in this assembly (excludes nested helper types).
    /// </summary>
    public static IReadOnlyList<Type> DiscoverUnitTestTypes()
    {
        return typeof(FrameworkTestExecutor).Assembly
            .GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                t.IsSubclassOf(typeof(UnitTestBase)) &&
                (!t.IsNested || t.IsNestedPublic))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Executes a single framework test type using the same discovery path as the CLI <c>ITestRunner</c>.
    /// </summary>
    public static async Task ExecuteAsync(Type testType, CancellationToken cancellationToken = default)
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
