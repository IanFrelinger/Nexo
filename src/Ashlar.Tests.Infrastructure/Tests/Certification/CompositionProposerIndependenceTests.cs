using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification.Composition;
using Ashlar.Tests.Infrastructure.Certification.Dogfood;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>Tests for composition proposer independence.</summary>
[Trait("Category", "Certification")]
public sealed class CompositionProposerIndependenceTests
{
    [Fact]
    public void ProposerInput_StructurallyCannotCarryWitnessCases()
    {
        var forbidden = ForbiddenWitnessTypeNames();
        var violations = new List<string>();
        ScanType(typeof(CompositionProposerInput), forbidden, violations, "CompositionProposerInput");

        violations.Should().BeEmpty(
            "proposer input must not contain witness-bearing fields: {0}",
            string.Join(", ", violations));
    }

    [Fact]
    public void CompositionGeneratorModel_InputPathCannotCarryWitnessCases()
    {
        var method = typeof(ICompositionGeneratorModel).GetMethod(nameof(ICompositionGeneratorModel.ProposeAsync))
            /// <summary>Invalid operation exception.</summary>
            /// <param name="found"">Found".</param>
            ?? throw new InvalidOperationException("ICompositionGeneratorModel.ProposeAsync not found");

        method.GetParameters().Should().ContainSingle(p =>
            p.ParameterType == typeof(CompositionProposerInput),
            "real model proposer must accept CompositionProposerInput only");

        var forbidden = ForbiddenWitnessTypeNames();
        var violations = new List<string>();
        foreach (var parameter in method.GetParameters())
            /// <summary>Scan type.</summary>
            /// <param name=""param"">"param".</param>
            ScanType(parameter.ParameterType, forbidden, violations, parameter.Name ?? "param");

        violations.Should().BeEmpty(
            "composition generator model input path must not carry witness cases: {0}",
            string.Join(", ", violations));
    }

    [Fact]
    public void RealProposerPrompt_IsBuiltFromProposerInputOnly_WithNoWitnessValues()
    {
        var ctx = CompositionDogfoodHarness.BuildProposerInputForIndependenceCheck();
        var prompt = CompositionProposerPromptBuilder.BuildUserPrompt(ctx);

        prompt.Should().Contain(ctx.Target.CompositionId);
        prompt.Should().Contain("damage-resolver");
        prompt.Should().Contain("health-applier");
        prompt.Should().NotContain("WitnessCase");
        prompt.Should().NotContain("expectedOutputs");

        foreach (var witnessCase in CompositionDogfoodHarness.RequireHumanWitness().Cases)
        {
            var serialized = JsonSerializer.Serialize(witnessCase);
            prompt.Should().NotContain(serialized,
                "serialized witness cases must never reach the model prompt");
        }
    }

    /// <summary>Forbidden witness type names.</summary>
    private static HashSet<string> ForbiddenWitnessTypeNames() =>
        new(StringComparer.Ordinal)
        {
            nameof(WitnessCase),
            nameof(WitnessSpec),
            nameof(CompositionWitnessSpec)
        };

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
                /// <summary>Scan type.</summary>
                ScanType(arg, forbiddenTypeNames, violations, $"{path}<{arg.Name}>");
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            /// <summary>Scan type.</summary>
            ScanType(property.PropertyType, forbiddenTypeNames, violations, $"{path}.{property.Name}");
        }
    }
}
