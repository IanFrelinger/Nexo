using System.Text;
using FluentAssertions;
using Ashlar.Manifest.Signing;
using Xunit;

namespace Ashlar.Tests.Kernel.Signing;

/// <summary>
/// The canonical form is the whole ballgame: a signature is a promise about EXACTLY these
/// bytes, so signer and verifier must derive identical bytes from identical values regardless
/// of how the value's keys happened to be ordered in memory. If canonicalisation is not
/// deterministic and order-free, every signature is theatre — it verifies on the machine that
/// wrote it and fails everywhere else.
/// </summary>
public sealed class CanonicalJsonTests
{
    private static string Str<T>(T value) => Encoding.UTF8.GetString(CanonicalJson.Bytes(value));

    [Fact]
    public void Object_keys_are_sorted_ordinally_regardless_of_declaration_order()
    {
        // Two anonymous types with the SAME content but OPPOSITE key order must canonicalise
        // to the same bytes. This is the property that lets a verifier on a different machine
        // agree with the signer.
        Str(new { b = 1, a = 2 }).Should().Be("{\"a\":2,\"b\":1}");
        Str(new { a = 2, b = 1 }).Should().Be("{\"a\":2,\"b\":1}");

        CanonicalJson.Bytes(new { z = 1, m = 2, a = 3 })
            .Should().Equal(CanonicalJson.Bytes(new { a = 3, m = 2, z = 1 }));
    }

    [Fact]
    public void Keys_are_sorted_at_every_depth()
    {
        Str(new { outer = new { b = 1, a = 2 }, a = new { d = 4, c = 3 } })
            .Should().Be("{\"a\":{\"c\":3,\"d\":4},\"outer\":{\"a\":2,\"b\":1}}");
    }

    [Fact]
    public void Array_order_is_preserved_not_sorted()
    {
        // Object keys are a set (order is not meaning); array elements are a sequence (order
        // IS meaning). Sorting an array would change the value, not just its spelling.
        Str(new { items = new[] { 3, 1, 2 } }).Should().Be("{\"items\":[3,1,2]}");
    }

    [Fact]
    public void Objects_inside_arrays_are_also_canonicalised()
    {
        Str(new { xs = new object[] { new { b = 1, a = 2 } } })
            .Should().Be("{\"xs\":[{\"a\":2,\"b\":1}]}");
    }

    [Fact]
    public void Null_valued_properties_are_omitted()
    {
        // A property that is present-but-null and a property that is absent must produce the
        // same bytes, so adding a nullable field that stays null does not invalidate old
        // signatures. This is exactly how the two signature fields are excluded from what
        // they sign.
        Str(new { a = 1, b = (string?)null }).Should().Be("{\"a\":1}");
        CanonicalJson.Bytes(new { a = 1, b = (string?)null })
            .Should().Equal(CanonicalJson.Bytes(new { a = 1 }));
    }

    [Fact]
    public void No_insignificant_whitespace()
    {
        Str(new { a = 1, b = new { c = 2 } }).Should().NotContain(" ").And.NotContain("\n");
    }

    [Fact]
    public void Identical_values_canonicalise_identically_every_time()
    {
        var value = new { name = "gate", n = 3, tags = new[] { "x", "y" } };
        CanonicalJson.Bytes(value).Should().Equal(CanonicalJson.Bytes(value));
    }

    [Fact]
    public void Enums_serialise_as_their_string_name()
    {
        // The store's records carry enum states; a numeric enum would be brittle across
        // renames, so the canonical form spells them.
        Str(new { state = DayOfWeek.Tuesday }).Should().Be("{\"state\":\"Tuesday\"}");
    }
}
