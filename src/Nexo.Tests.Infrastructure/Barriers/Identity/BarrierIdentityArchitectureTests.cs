using FluentAssertions;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Orchestration.Coordination;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

public sealed class BarrierIdentityArchitectureTests
{
    [Fact]
    public void IBarrierIdentityResolver_Implementations_MustResideInRuntime()
    {
        var resolverType = typeof(IBarrierIdentityResolver);

        var abstractionsImplementations = resolverType.Assembly.GetTypes()
            .Where(type => IsConcreteResolver(type, resolverType))
            .ToList();
        abstractionsImplementations.Should().BeEmpty();

        var orchestrationImplementations = typeof(Orchestrator).Assembly.GetTypes()
            .Where(type => IsConcreteResolver(type, resolverType))
            .ToList();
        orchestrationImplementations.Should().BeEmpty();

        var runtimeImplementations = typeof(JwtClaimBarrierResolver).Assembly.GetTypes()
            .Where(type => IsConcreteResolver(type, resolverType))
            .ToList();

        runtimeImplementations.Should().Contain(type => type == typeof(PkiCertificateBarrierResolver));
        runtimeImplementations.Should().Contain(type => type == typeof(JwtClaimBarrierResolver));
        runtimeImplementations.Should().Contain(type => type == typeof(ApiKeyBarrierResolver));
    }

    [Fact]
    public void Abstractions_Barriers_Identity_Namespace_MustOnlyDependOnSystem()
    {
        var identityTypes = typeof(IBarrierIdentityResolver).Assembly.GetTypes()
            .Where(type => string.Equals(type.Namespace, "Nexo.Abstractions.Barriers.Identity", StringComparison.Ordinal))
            .ToList();

        identityTypes.Should().NotBeEmpty();
        foreach (var type in identityTypes)
        {
            foreach (var referencedType in GetReferencedTypes(type))
            {
                if (referencedType.Namespace is null)
                    continue;
                (referencedType.Namespace.StartsWith("System", StringComparison.Ordinal) ||
                 referencedType.Namespace.StartsWith("Nexo.Abstractions.Barriers.Identity", StringComparison.Ordinal))
                    .Should().BeTrue(
                        $"type '{type.FullName}' should only depend on System.* but referenced '{referencedType.FullName}'");
            }
        }
    }

    [Fact]
    public void JwtClaimBarrierResolver_MustNotReferenceJwtValidationLibraries()
    {
        var refs = typeof(JwtClaimBarrierResolver).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        refs.Should().NotContain(name => name.Contains("System.IdentityModel", StringComparison.OrdinalIgnoreCase));
        refs.Should().NotContain(name => name.Contains("Microsoft.IdentityModel", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsConcreteResolver(Type candidate, Type resolverType)
        => resolverType.IsAssignableFrom(candidate)
           && candidate.IsClass
           && !candidate.IsAbstract;

    private static IReadOnlyList<Type> GetReferencedTypes(Type type)
    {
        var refs = new List<Type>();
        refs.AddRange(type.GetProperties().Select(x => x.PropertyType));
        refs.AddRange(type.GetFields().Select(x => x.FieldType));
        refs.AddRange(type.GetMethods()
            .SelectMany(x => x.GetParameters().Select(p => p.ParameterType))
            .Concat(type.GetMethods().Select(x => x.ReturnType)));

        return refs
            .Select(StripGeneric)
            .Where(x => x != typeof(void))
            .Distinct()
            .ToList();
    }

    private static Type StripGeneric(Type type)
    {
        if (type.IsGenericType)
            return type.GetGenericTypeDefinition();
        return type;
    }
}
