using FluentAssertions;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for BrickValueSerializer: wire format for BrickInput/BrickOutput (including byte[] as base64).
/// </summary>
public class BrickValueSerializerTests
{
    [Fact]
    public void ToWireDictionary_WithByteArray_WrapsAsBase64()
    {
        var input = new BrickInput();
        input.Set("key", new byte[] { 1, 2, 3 });

        var wire = BrickValueSerializer.ToWireDictionary(input);

        wire.Should().ContainKey("key");
        var value = wire["key"];
        value.Should().BeOfType<Dictionary<string, object>>();
        var dict = (Dictionary<string, object>)value;
        dict.Should().ContainKey("__type");
        dict["__type"].Should().Be("bytes");
        dict.Should().ContainKey("base64");
        Convert.FromBase64String(dict["base64"].ToString()!).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void FromWireToBrickInput_RoundTrip_PreservesData()
    {
        var input = new BrickInput();
        input.Set("a", "hello");
        input.Set("b", 42);
        input.Set("c", new byte[] { 10, 20 });

        var wire = BrickValueSerializer.ToWireDictionary(input);
        var json = BrickValueSerializer.ToJson(wire);
        var wireBack = BrickValueSerializer.FromJsonToWireDictionary(json);
        var back = BrickValueSerializer.FromWireToBrickInput(wireBack);

        back.Get<string>("a").Should().Be("hello");
        back.Get<long>("b").Should().Be(42L); // JSON numbers deserialize as long
        back.Get<byte[]>("c").Should().Equal(10, 20);
    }

    [Fact]
    public void FromWireToBrickOutput_WithSummary_SetsSummary()
    {
        var wire = new Dictionary<string, object>
        {
            ["out1"] = "value1"
        };
        var output = BrickValueSerializer.FromWireToBrickOutput(wire, "Done");

        output.Get<string>("out1").Should().Be("value1");
        output.Summary.Should().Be("Done");
    }
}
