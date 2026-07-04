using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Nexo.Core.Configuration;
using Nexo.Core.Specs;
using Xunit;

namespace Nexo.Tests.Kernel;

/// <summary>Tests for feature spec.</summary>
public class FeatureSpecTests
{
    [Fact]
    public void Record_round_trips_all_properties()
    {
        var caps = new[] { "c1", "c2" };
        var parms = new Dictionary<string, string> { ["k"] = "v" };
        var policies = new[] { "p1" };

        var spec = new FeatureSpec("name", "1.0.0", caps, parms, policies);

        spec.Name.Should().Be("name");
        spec.Version.Should().Be("1.0.0");
        spec.Capabilities.Should().BeSameAs(caps);
        spec.Parameters.Should().BeSameAs(parms);
        spec.Policies.Should().BeSameAs(policies);
    }

    [Fact]
    public void Records_with_same_data_are_value_equal()
    {
        var a = new FeatureSpec("n", "v", new[] { "c" }, new Dictionary<string, string>(), Array.Empty<string>());
        var b = a with { Name = "n2" };
        a.Should().NotBe(b);
        b.Name.Should().Be("n2");
    }
}
