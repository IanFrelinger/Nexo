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

    /// <summary>
    /// xunit enforces Timeout by racing the returned Task against a delay, so it can only do it for a
    /// Task-returning test; a void one is rejected at run time with "Tests marked with Timeout are only
    /// supported for async tests". The two halves of the convention would otherwise contradict each other:
    /// the timeout is required, and adding it to a void test turns a passing test into a failing one.
    /// </summary>
    private static bool ReturnsTask(MethodInfo method)
    {
        var returnType = method.ReturnType;
        if (returnType == typeof(Task) || returnType == typeof(ValueTask)) return true;

        return returnType.IsGenericType &&
               (returnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                returnType.GetGenericTypeDefinition() == typeof(ValueTask<>));
    }

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
                // TheoryAttribute derives from FactAttribute, so one lookup covers both; the name is
                // only used to quote the right attribute back at whoever has to fix it.
                var fact = method.GetCustomAttribute<FactAttribute>();
                if (fact is null) continue;

                var attribute = fact is TheoryAttribute ? "Theory" : "Fact";

                if (fact.Timeout == 0)
                {
                    violations.Add($"{type.Name}.{method.Name}: {category} test lacks [{attribute}(Timeout = N)]");
                }
                else if (!ReturnsTask(method))
                {
                    violations.Add($"{type.Name}.{method.Name}: {category} test has [{attribute}(Timeout = N)] but returns {method.ReturnType.Name}; make it async Task (xunit only honours Timeout on Task-returning tests and fails the test at run time otherwise)");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Host-touching tests must have explicit Timeout:\n" + string.Join("\n", violations));
    }
}
