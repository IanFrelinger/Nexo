using FluentAssertions;
using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Exceptions;
using Ashlar.Core.Domain.Execution.Events;
using Ashlar.Core.Domain.Values;
using Ashlar.Core.Domain.Workflows;
using Xunit;

namespace Ashlar.Tests.Domain;

/// <summary>Tests for domain convention architecture.</summary>
public sealed class DomainConventionArchitectureTests
{
    private static readonly Type[] SanctionedBaseTypes =
    [
        // Closed execution-event hierarchy for behavior/step runtime telemetry.
        typeof(ExecutionEvent),

        // Closed domain exception family. DomainException itself derives from System.Exception.
        typeof(DomainException),
        typeof(Exception),

        // Type-value objects centralize string/value-object equality semantics.
        typeof(BaseTypeValue),

        // Visual workflow node hierarchy is a closed domain model for composer graph nodes.
        typeof(WorkflowNode)
    ];

    [Fact]
    public void CoreDomain_MustNotDeclare_GenericClassesRecordsOrStructs()
    {
        var offenders = CoreDomainTypes()
            .Where(t => (t.IsClass || t.IsValueType) && t.IsGenericTypeDefinition)
            .Select(t => t.FullName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        offenders.Should().BeEmpty("Core.Domain currently has no generic class/record/struct declarations; keep domain contracts simple and concrete.");
    }

    [Fact]
    public void CoreDomain_ClassInheritance_MustStayWithin_SanctionedFamilies()
    {
        var sanctioned = SanctionedBaseTypes.ToHashSet();
        var offenders = CoreDomainTypes()
            .Where(t => t.IsClass)
            .Where(t => !IsCompilerGenerated(t))
            .Select(t => new { Type = t, Base = t.BaseType })
            .Where(x => x.Base is not null && x.Base != typeof(object))
            .Where(x => !sanctioned.Contains(x.Type) && !sanctioned.Contains(x.Base!))
            .Select(x => $"{x.Type.FullName} : {x.Base!.FullName}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        offenders.Should().BeEmpty(
            "Core.Domain inheritance is intentionally limited to execution events, domain exceptions, type-value objects, and visual workflow nodes.");
    }

    private static IReadOnlyList<Type> CoreDomainTypes()
    {
        return typeof(AshlarDefaults).Assembly
            .GetTypes()
            .Where(t => t.Namespace?.StartsWith("Ashlar.Core.Domain", StringComparison.Ordinal) == true)
            .ToArray();
    }

    /// <summary>Returns whether  compiler generated.</summary>
    /// <param name="type">Type.</param>
    private static bool IsCompilerGenerated(Type type) =>
        type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false).Length > 0;
}
