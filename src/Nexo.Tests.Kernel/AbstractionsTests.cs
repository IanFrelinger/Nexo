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

public class AbstractionsCoreTests
{
    [Fact]
    public void ActionDelta_Merge_empty_returns_empty_delta()
    {
        var merged = ActionDelta.Merge(Array.Empty<IActionDelta>());
        merged.TickFrom.Should().Be(0);
        merged.TickTo.Should().Be(0);
        merged.Log.Should().BeEmpty();
    }

    [Fact]
    public void ActionDelta_Merge_combines_ticks_and_logs()
    {
        var d1 = new ActionDelta(1, 3, new[] { "a" });
        var d2 = new ActionDelta(2, 5, new[] { "b", "c" });
        var merged = ActionDelta.Merge(new IActionDelta[] { d1, d2 });
        merged.TickFrom.Should().Be(1);
        merged.TickTo.Should().Be(5);
        merged.Log.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void ActionDelta_signature_round_trips()
    {
        var delta = new ActionDelta(0, 1, Array.Empty<string>()) { Signature = new byte[] { 1, 2 } };
        delta.Signature.Should().Equal(new byte[] { 1, 2 });
    }

    [Fact]
    public void AgentActions_None_is_empty()
    {
        AgentActions.None.ToolCalls.Should().BeEmpty();
    }

    [Fact]
    public void WorldSnapshot_ForRepo_sets_repo_and_output_roots()
    {
        var snap = WorldSnapshot.ForRepo("/repo");
        snap.Data["RepoRoot"].Should().Be("/repo");
        snap.Data["OutputRoot"].Should().Be(Path.Combine("/repo", "out"));

        var custom = WorldSnapshot.ForRepo("/repo", "/out", tick: 7);
        custom.Tick.Should().Be(7);
        custom.Data["OutputRoot"].Should().Be("/out");
    }

    [Fact]
    public void ToolCall_ParseArgs_deserializes_arguments()
    {
        var json = JsonDocument.Parse("""{"n":42}""").RootElement;
        var call = new ToolCall("t", json);
        var args = call.ParseArgs<Dictionary<string, int>>();
        args["n"].Should().Be(42);
    }

    [Fact]
    public void CapabilityAttribute_exposes_capabilities()
    {
        var attr = new CapabilityAttribute("a", "b");
        attr.Capabilities.Should().Equal("a", "b");
    }
}

public class BarrierLevelTests
{
    [Fact]
    public void Constructor_rejects_blank_name()
    {
        var act = () => new BarrierLevel("  ", 1);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void CompareTo_and_operators_work()
    {
        var low = new BarrierLevel("low", 1);
        var high = new BarrierLevel("high", 5);

        low.CompareTo(high).Should().BeNegative();
        low.CompareTo(null).Should().BePositive();
        (low < high).Should().BeTrue();
        (low <= high).Should().BeTrue();
        (high > low).Should().BeTrue();
        (high >= low).Should().BeTrue();
        low.Equals(high).Should().BeFalse();
        low.GetHashCode().Should().NotBe(high.GetHashCode());
    }
}

public class BarrierHierarchyTests
{
    private static BarrierHierarchy TwoLevel() => new(new[]
    {
        new BarrierLevel("public", 0),
        new BarrierLevel("secret", 10),
    });

    [Fact]
    public void Constructor_orders_by_rank_and_exposes_floor_ceiling()
    {
        var h = TwoLevel();
        h.Floor.Name.Should().Be("public");
        h.Ceiling.Name.Should().Be("secret");
        h.Select(x => x).Should().Equal("public", "secret");
    }

    [Fact]
    public void Non_generic_IEnumerable_enumerates_level_names()
    {
        var h = TwoLevel();
        var names = new List<string>();
        System.Collections.IEnumerable untyped = h;
        foreach (string name in untyped) names.Add(name);
        names.Should().Equal("public", "secret");
    }

    [Fact]
    public void Constructor_validates_input()
    {
        Assert.Throws<ArgumentNullException>(() => new BarrierHierarchy(null!));
        Assert.Throws<ArgumentException>(() => new BarrierHierarchy(Array.Empty<BarrierLevel>()));
        Assert.Throws<ArgumentException>(() => new BarrierHierarchy(new[]
        {
            new BarrierLevel("a", 1),
            new BarrierLevel("A", 2),
        }));
        Assert.Throws<ArgumentException>(() => new BarrierHierarchy(new[]
        {
            new BarrierLevel("a", 1),
            new BarrierLevel("b", 1),
        }));
    }

    [Fact]
    public void Get_IsKnown_IsAtOrBelow_IsAbove_Highest()
    {
        var h = TwoLevel();
        h.IsKnown("public").Should().BeTrue();
        h.IsKnown("PUBLIC").Should().BeTrue();
        h.IsKnown("").Should().BeFalse();
        h.IsKnown("missing").Should().BeFalse();

        h.Get("secret").Rank.Should().Be(10);
        Assert.Throws<ArgumentException>(() => h.Get(""));
        Assert.Throws<ArgumentException>(() => h.Get("nope"));

        h.IsAtOrBelow("public", "secret").Should().BeTrue();
        h.IsAtOrBelow("secret", "public").Should().BeFalse();
        h.IsAbove("secret", "public").Should().BeTrue();
        h.Highest("public", "secret").Name.Should().Be("secret");
        h.Highest("secret", "public").Name.Should().Be("secret");
    }
}

public class BarrierContextTests
{
    [Fact]
    public void Create_validates_level_and_sets_fields()
    {
        var h = new BarrierHierarchy(new[] { new BarrierLevel("public", 0) });
        var ctx = BarrierContext.Create("public", "jwt", "user", "corr", h, "detail");
        ctx.Level.Should().Be("public");
        ctx.AuthoritySource.Should().Be("jwt");
        ctx.IssuedTo.Should().Be("user");
        ctx.CorrelationId.Should().Be("corr");
        ctx.ResolutionDetail.Should().Be("detail");
        ctx.IssuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_rejects_unknown_level_and_null_hierarchy()
    {
        var h = new BarrierHierarchy(new[] { new BarrierLevel("public", 0) });
        Assert.Throws<ArgumentNullException>(() =>
            BarrierContext.Create("public", "jwt", "user", "corr", null!));
        Assert.Throws<ArgumentException>(() =>
            BarrierContext.Create("secret", "jwt", "user", "corr", h));
    }

    [Fact]
    public void ForAgent_updates_issued_to()
    {
        var h = new BarrierHierarchy(new[] { new BarrierLevel("public", 0) });
        var ctx = BarrierContext.Create("public", "jwt", "user", "corr", h);
        ctx.ForAgent("agent-1").IssuedTo.Should().Be("agent-1");
    }
}

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

public class TransportAndDatabaseAbstractionsTests
{
    [Fact]
    public void AgentInvocationOptions_Default_has_expected_values()
    {
        var d = AgentInvocationOptions.Default;
        d.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        d.MaxRetries.Should().Be(3);
        d.TargetEndpoint.Should().BeNull();
    }

    [Fact]
    public void AgentInvocationOptions_record_round_trips()
    {
        var o = new AgentInvocationOptions(TimeSpan.FromMinutes(1), 5, "grpc://host");
        o.Timeout.Should().Be(TimeSpan.FromMinutes(1));
        o.MaxRetries.Should().Be(5);
        o.TargetEndpoint.Should().Be("grpc://host");
    }

    [Fact]
    public void DatabaseProvisionRequest_supports_all_optional_fields()
    {
        var req = new DatabaseProvisionRequest(
            DatabaseIsolationLevel.DedicatedContainer,
            DatabaseName: "db",
            ImageTag: "15",
            Password: "pw",
            AdminConnectionString: "Host=localhost",
            PostgresReadinessTimeout: TimeSpan.Zero,
            PostProvisionSqlBatches: new[] { "select 1" });
        req.Isolation.Should().Be(DatabaseIsolationLevel.DedicatedContainer);
        req.DatabaseName.Should().Be("db");
        req.ImageTag.Should().Be("15");
        req.Password.Should().Be("pw");
        req.AdminConnectionString.Should().Be("Host=localhost");
        req.PostgresReadinessTimeout.Should().Be(TimeSpan.Zero);
        req.PostProvisionSqlBatches.Should().ContainSingle();
    }
}
