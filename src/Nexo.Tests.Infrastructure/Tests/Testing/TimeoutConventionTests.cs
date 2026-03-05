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

    [Fact]
    public void E2ETests_MustHaveExplicitTimeout()
    {
        var assembly = typeof(TimeoutConventionTests).Assembly;
        var violations = new List<string>();

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;

            var hasE2E = HasTrait(type, "Category", "E2E");
            if (!hasE2E) continue;

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var fact = method.GetCustomAttribute<FactAttribute>();
                if (fact == null) continue;

                if (fact.Timeout == 0)
                {
                    violations.Add($"{type.Name}.{method.Name}: E2E test lacks [Fact(Timeout = N)]");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "E2E tests must have [Fact(Timeout = N)]:\n" + string.Join("\n", violations));
    }
}
