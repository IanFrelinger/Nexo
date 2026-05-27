using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Nexo.Core.Configuration;
using Nexo.Core.Specs;
using Xunit;

namespace Nexo.Tests.Kernel;

public class RepairLoopOptionsTests
{
    [Fact]
    public void Defaults_are_within_documented_ranges()
    {
        var opts = new RepairLoopOptions();
        opts.MaxRepairIterations.Should().Be(2);
        opts.EnableCanaryDeployment.Should().BeTrue();
        opts.EnableAutomaticRollback.Should().BeTrue();
        opts.RepairTimeoutMs.Should().Be(30000);
        opts.CanaryTimeoutMs.Should().Be(60000);
    }

    [Fact]
    public void Setters_round_trip()
    {
        var opts = new RepairLoopOptions
        {
            MaxRepairIterations = 5,
            EnableCanaryDeployment = false,
            EnableAutomaticRollback = false,
            RepairTimeoutMs = 1500,
            CanaryTimeoutMs = 2500,
        };

        opts.MaxRepairIterations.Should().Be(5);
        opts.EnableCanaryDeployment.Should().BeFalse();
        opts.EnableAutomaticRollback.Should().BeFalse();
        opts.RepairTimeoutMs.Should().Be(1500);
        opts.CanaryTimeoutMs.Should().Be(2500);
    }

    [Fact]
    public void Validation_rejects_out_of_range_values()
    {
        var opts = new RepairLoopOptions
        {
            MaxRepairIterations = 999,
            RepairTimeoutMs = 1,
            CanaryTimeoutMs = 1,
        };

        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(opts, ctx, results, true).Should().BeFalse();
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void Validation_accepts_in_range_values()
    {
        var opts = new RepairLoopOptions
        {
            MaxRepairIterations = 0,
            RepairTimeoutMs = 1000,
            CanaryTimeoutMs = 1000,
        };

        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(opts, ctx, results, true).Should().BeTrue();
        results.Should().BeEmpty();
    }
}

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
