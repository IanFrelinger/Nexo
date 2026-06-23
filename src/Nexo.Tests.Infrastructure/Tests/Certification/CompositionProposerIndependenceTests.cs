using System.Reflection;
using FluentAssertions;
using Nexo.Core.Application.Certification.Models;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class CompositionProposerIndependenceTests
{
    [Fact]
    public void ProposerInput_StructurallyCannotCarryWitnessCases()
    {
        var forbidden = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(WitnessCase),
            nameof(WitnessSpec),
            nameof(CompositionWitnessSpec)
        };

        var violations = new List<string>();
        ScanType(typeof(CompositionProposerInput), forbidden, violations, "CompositionProposerInput");

        violations.Should().BeEmpty(
            "proposer input must not contain witness-bearing fields: {0}",
            string.Join(", ", violations));
    }

    private static void ScanType(
        Type type,
        IReadOnlySet<string> forbiddenTypeNames,
        ICollection<string> violations,
        string path)
    {
        if (forbiddenTypeNames.Contains(type.Name))
        {
            violations.Add(path);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
                ScanType(arg, forbiddenTypeNames, violations, $"{path}<{arg.Name}>");
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            ScanType(property.PropertyType, forbiddenTypeNames, violations, $"{path}.{property.Name}");
        }
    }
}
