using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Abstractions.Database;
using Nexo.Abstractions.Transport;
using Xunit;

namespace Nexo.Tests.Kernel;

/// <summary>Tests for jwt claim parser.</summary>
public class JwtClaimParserTests
{
    private static string Base64Url(string text)
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return b64;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    [InlineData("a")]
    [InlineData("a.b")]
    public void ParseClaims_returns_empty_for_invalid_tokens(string? token)
    {
        JwtClaimParser.ParseClaims(token).Should().BeEmpty();
    }

    [Fact]
    public void ParseClaims_parses_base64url_payload()
    {
        var payload = Base64Url("""{"sub":"user","role":"admin","active":true,"count":3}""");
        var jwt = $"header.{payload}.sig";
        var claims = JwtClaimParser.ParseClaims(jwt);
        claims["sub"].Should().Be("user");
        claims["role"].Should().Be("admin");
        claims["active"].Should().Be("true");
        claims["count"].Should().Be("3");
    }

    [Fact]
    public void ParseClaims_parses_false_boolean_claim_values()
    {
        var payload = Base64Url("""{"enabled":false,"active":true}""");
        var claims = JwtClaimParser.ParseClaims($"h.{payload}.s");
        claims["enabled"].Should().Be("false");
        claims["active"].Should().Be("true");
    }

    [Fact]
    public void ParseClaims_parses_raw_json_payload_when_not_base64()
    {
        var jwt = $"header.{{\"sub\":\"x\"}}.sig";
        JwtClaimParser.ParseClaims(jwt)["sub"].Should().Be("x");
    }

    [Fact]
    public void ParseClaims_returns_empty_for_non_object_payload()
    {
        var payload = Base64Url("[1,2,3]");
        JwtClaimParser.ParseClaims($"h.{payload}.s").Should().BeEmpty();
    }

    [Fact]
    public void ParseClaims_handles_base64_padding_cases()
    {
        // length % 4 == 2 -> adds ==
        var payload2 = Base64Url("{\"a\":\"b\"}");
        JwtClaimParser.ParseClaims($"h.{payload2}.s").Should().ContainKey("a");

        // invalid padding length % 4 == 1
        JwtClaimParser.ParseClaims("h.a.s").Should().BeEmpty();
    }

    [Fact]
    public void ParseClaims_swallows_format_and_json_errors()
    {
        JwtClaimParser.ParseClaims("h.%%%invalid%%%.s").Should().BeEmpty();
        JwtClaimParser.ParseClaims("h.{not json}.s").Should().BeEmpty();
    }

    [Fact]
    public void ParseClaims_handles_padding_length_mod_3_and_null_claim_values()
    {
        // payload length % 4 == 3 triggers single '=' padding branch
        var payload = Base64Url("{\"n\":null,\"x\":1}");
        JwtClaimParser.ParseClaims($"h.{payload}.s")["n"].Should().BeEmpty();
        JwtClaimParser.ParseClaims($"h.{payload}.s")["x"].Should().Be("1");
    }

    [Fact]
    public void ParseClaims_returns_empty_for_empty_base64_payload()
    {
        JwtClaimParser.ParseClaims("h..s").Should().BeEmpty();
    }

    [Fact]
    public void ParseClaims_returns_empty_on_invalid_base64_payload()
    {
        // length % 4 == 1 hits default branch; invalid base64 triggers FormatException path
        JwtClaimParser.ParseClaims("h.!!!.s").Should().BeEmpty();
    }

    [Fact]
    public void ParseClaims_returns_empty_when_base64_length_mod4_is_one()
    {
        JwtClaimParser.ParseClaims("h.YQ.s").Should().BeEmpty();
    }

    [Fact]
    public void ParseClaims_handles_object_and_array_claim_values_via_default_branch()
    {
        var payload = Base64Url("""{"obj":{"a":1},"arr":[1,2]}""");
        var claims = JwtClaimParser.ParseClaims($"h.{payload}.s");
        claims["obj"].Should().Contain("a");
        claims["arr"].Should().StartWith("[");
    }

    [Fact]
    public void ParseClaims_maps_null_claim_values_to_empty_string()
    {
        var payload = Base64Url("""{"only":null}""");
        JwtClaimParser.ParseClaims($"h.{payload}.s")["only"].Should().BeEmpty();
        JwtClaimParser.ParseClaims("h.{\"only\":null}.s")["only"].Should().BeEmpty();
    }

    [Fact]
    public void ClaimValueToString_maps_null_json_values_to_empty_string()
    {
        using var doc = System.Text.Json.JsonDocument.Parse("{\"only\":null}");
        var value = doc.RootElement.GetProperty("only");
        var method = typeof(JwtClaimParser).GetMethod(
            "ClaimValueToString",
            BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var text = method!.Invoke(null, [value]);
        text.Should().Be(string.Empty);
    }

    [Fact]
    public void ParseClaims_swallows_argument_exception_from_invalid_utf16_json()
    {
        // Unpaired surrogate in raw JSON payload triggers ArgumentException during parse.
        var jwt = "h.{\"" + "\uD800" + "\":1}.s";
        JwtClaimParser.ParseClaims(jwt).Should().BeEmpty();
    }
}
