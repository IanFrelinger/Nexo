using FluentAssertions;
using Ashlar.Core.Application.Adaptation;
using Ashlar.Core.Application.Certification.Models;
using Xunit;

namespace Ashlar.Tests.Application.Tests.Adaptation;

/// <summary>
/// <see cref="WitnessSignatureBuilder"/> derives the I/O SHAPE a generator is
/// allowed to see from a witness spec.
///
/// The security-relevant property is what it must NOT carry: a witness holds the
/// expected output VALUES that a generated brick is graded against, and handing
/// those to the generator would let it hard-code the answers instead of
/// implementing the behaviour. The signature is names and types only, and that is
/// what these tests pin.
/// </summary>
public sealed class WitnessSignatureBuilderTests
{
    [Fact]
    public void Derives_input_and_output_field_names_from_the_witness()
    {
        var witness = Witness(
            input: new Dictionary<string, object> { ["left"] = 1, ["right"] = 2 },
            expected: new Dictionary<string, object> { ["sum"] = 3 });

        var signature = WitnessSignatureBuilder.FromWitness(witness);

        signature.BrickId.Should().Be("adder");
        signature.Inputs.Select(f => f.Name).Should().Equal("left", "right");
        signature.Outputs.Select(f => f.Name).Should().Equal("sum");
    }

    [Fact]
    public void Does_not_carry_expected_output_values()
    {
        // The whole point of the type: the generator learns that there is an int
        // named "sum", never that the answer is 3.
        var witness = Witness(
            input: new Dictionary<string, object> { ["left"] = 1 },
            expected: new Dictionary<string, object> { ["sum"] = 999 });

        var signature = WitnessSignatureBuilder.FromWitness(witness);

        var rendered = string.Join(
            "|",
            signature.Outputs.Select(f => $"{f.Name}:{f.Type}"));
        rendered.Should().Be("sum:int");
        rendered.Should().NotContain("999", "a signature that leaks expected values lets a generator cheat the witness");
    }

    [Theory]
    [InlineData(42, "int")]
    [InlineData(42L, "int")]
    [InlineData((short)42, "int")]
    [InlineData((byte)42, "int")]
    [InlineData(true, "bool")]
    [InlineData("text", "string")]
    [InlineData(1.5d, "double")]
    [InlineData(1.5f, "double")]
    public void Infers_the_declared_type_name_for_each_primitive(object value, string expectedType)
    {
        var witness = Witness(
            input: new Dictionary<string, object> { ["field"] = value },
            expected: new Dictionary<string, object> { ["out"] = 0 });

        WitnessSignatureBuilder.FromWitness(witness)
            .Inputs.Single().Type.Should().Be(expectedType);
    }

    [Fact]
    public void Infers_decimal_as_double()
    {
        // decimal cannot be an InlineData constant, so it gets its own case rather
        // than being quietly left untested.
        var witness = Witness(
            input: new Dictionary<string, object> { ["price"] = 1.5m },
            expected: new Dictionary<string, object> { ["out"] = 0 });

        WitnessSignatureBuilder.FromWitness(witness)
            .Inputs.Single().Type.Should().Be("double");
    }

    [Fact]
    public void Falls_back_to_object_for_unrecognised_types()
    {
        var witness = Witness(
            input: new Dictionary<string, object> { ["payload"] = new[] { 1, 2, 3 } },
            expected: new Dictionary<string, object> { ["out"] = 0 });

        WitnessSignatureBuilder.FromWitness(witness)
            .Inputs.Single().Type.Should().Be("object");
    }

    [Fact]
    public void Uses_only_the_first_case_to_derive_the_shape()
    {
        // Documented behaviour worth pinning: later cases are graded against, but
        // the SHAPE comes from case one. A second case with extra fields must not
        // widen the signature, or the generator would see a contract the first
        // case never establishes.
        var witness = new WitnessSpec("multi", new[]
        {
            new WitnessCase(
                new Dictionary<string, object> { ["a"] = 1 },
                new Dictionary<string, object> { ["r"] = 1 }),
            new WitnessCase(
                new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 },
                new Dictionary<string, object> { ["r"] = 2, ["extra"] = 3 })
        });

        var signature = WitnessSignatureBuilder.FromWitness(witness);

        signature.Inputs.Select(f => f.Name).Should().Equal("a");
        signature.Outputs.Select(f => f.Name).Should().Equal("r");
    }

    [Fact]
    public void A_witness_with_no_cases_is_rejected()
    {
        var witness = new WitnessSpec("empty", Array.Empty<WitnessCase>());

        var act = () => WitnessSignatureBuilder.FromWitness(witness);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one case*",
                "an empty witness cannot describe a shape, and silently returning an empty signature would let an ungraded brick through");
    }

    [Fact]
    public void An_empty_input_map_yields_an_empty_input_list_not_a_failure()
    {
        // A parameterless brick is legitimate; only a case-less witness is not.
        var witness = Witness(
            input: new Dictionary<string, object>(),
            expected: new Dictionary<string, object> { ["out"] = 1 });

        var signature = WitnessSignatureBuilder.FromWitness(witness);

        signature.Inputs.Should().BeEmpty();
        signature.Outputs.Should().ContainSingle();
    }

    private static WitnessSpec Witness(
        Dictionary<string, object> input,
        Dictionary<string, object> expected) =>
        new("adder", new[] { new WitnessCase(input, expected) });
}
