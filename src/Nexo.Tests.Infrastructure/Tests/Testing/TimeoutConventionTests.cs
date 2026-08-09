using System.Reflection;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Testing;

/// <summary>
/// Sanity test: Integration and E2E tests must have explicit [Fact(Timeout = N)] to prevent blame-hang.
/// </summary>
public sealed class TimeoutConventionTests
{
    private static bool HasTrait(Type type, string name, string value)
    {
        foreach (var attr in type.GetCustomAttributesData())
        {
            if (attr.AttributeType.Name != "TraitAttribute") continue;
            if (attr.ConstructorArguments.Count >= 2 &&
                attr.ConstructorArguments[0].Value?.ToString() == name &&
                attr.ConstructorArguments[1].Value?.ToString() == value)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Categories whose tests touch real hosts, sockets, processes or containers, and
    /// can therefore block indefinitely rather than fail.
    /// </summary>
    /// <remarks>
    /// ProdStyle was added after a test in that category wedged the entire suite. It
    /// stood up a full API host through WebApplicationFactory, whose Services property
    /// blocks on host.StartAsync(); a hosted service never finished starting, so the
    /// test never finished either. With no timeout there was nothing to fail — the run
    /// simply stopped making progress, and two CI runs burned 30 and 60 minutes before
    /// being cancelled without ever naming a culprit.
    ///
    /// The guard existed at the time and did not catch it, purely because the class was
    /// traited ProdStyle rather than E2E. Both categories carry the same risk, so both
    /// are gated now.
    /// </remarks>
    private static readonly string[] TimeoutRequiredCategories = ["E2E", "ProdStyle"];

    [Fact]
    public void HostTouchingTests_MustHaveExplicitTimeout()
    {
        var assembly = typeof(TimeoutConventionTests).Assembly;
        var violations = new List<string>();

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;

            var category = TimeoutRequiredCategories.FirstOrDefault(c => HasTrait(type, "Category", c));
            if (category is null) continue;

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var fact = method.GetCustomAttribute<FactAttribute>();
                if (fact != null && fact.Timeout == 0)
                {
                    violations.Add($"{type.Name}.{method.Name}: {category} test lacks [Fact(Timeout = N)]");
                }

                var theory = method.GetCustomAttribute<TheoryAttribute>();
                if (theory != null && theory.Timeout == 0)
                {
                    violations.Add($"{type.Name}.{method.Name}: {category} test lacks [Theory(Timeout = N)]");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Host-touching tests must have explicit Timeout:\n" + string.Join("\n", violations));
    }
}
