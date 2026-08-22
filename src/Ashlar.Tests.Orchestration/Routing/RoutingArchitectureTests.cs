using FluentAssertions;
using System.Reflection;
using System.Xml.Linq;
using Ashlar.Abstractions.Barriers;
using Xunit;

namespace Ashlar.Tests.Orchestration.Routing;

/// <summary>Tests for routing architecture.</summary>
public sealed class RoutingArchitectureTests
{
    [Fact]
    public void AshlarOrchestrationRouting_MustNotReference_AshlarRuntime()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../Ashlar.Orchestration/Ashlar.Orchestration.csproj"));
        File.Exists(projectPath).Should().BeTrue();

        var doc = XDocument.Load(projectPath);
        var references = doc
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include"))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        references.Should().NotContain(x => x!.Contains("Ashlar.Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AshlarAbstractionsBarriers_MustOnlyUse_System_And_BarriersNamespaces()
    {
        var assembly = typeof(BarrierContext).Assembly;
        var barrierTypes = assembly.GetTypes()
            .Where(t => t.Namespace == "Ashlar.Abstractions.Barriers")
            .ToList();

        barrierTypes.Should().NotBeEmpty();
        foreach (var type in barrierTypes)
        {
            foreach (var referencedType in GetReferencedTypes(type))
            {
                if (referencedType.Namespace is null)
                    continue;
                if (referencedType.Namespace.StartsWith("System", StringComparison.Ordinal))
                    continue;
                if (referencedType.Namespace.StartsWith("Ashlar.Abstractions.Barriers", StringComparison.Ordinal))
                    continue;

                throw new Xunit.Sdk.XunitException(
                    $"Type {type.FullName} references non-system dependency {referencedType.FullName}.");
            }
        }
    }

    [Fact]
    public void BarrierContext_MustOnlyBeConstructedVia_Create()
    {
        var publicConstructors = typeof(BarrierContext).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        publicConstructors.Should().BeEmpty();

        var create = typeof(BarrierContext).GetMethod(
            "Create",
            BindingFlags.Public | BindingFlags.Static);
        create.Should().NotBeNull();
    }

    private static IReadOnlyList<Type> GetReferencedTypes(Type type)
    {
        var refs = new List<Type>();
        refs.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(x => x.PropertyType));
        refs.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(x => x.FieldType));
        refs.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
            .SelectMany(x => x.GetParameters().Select(p => p.ParameterType))
            .Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                .Select(x => x.ReturnType)));

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
