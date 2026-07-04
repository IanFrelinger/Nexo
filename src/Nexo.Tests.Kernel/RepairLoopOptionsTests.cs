using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Nexo.Core.Configuration;
using Nexo.Core.Specs;
using Xunit;

namespace Nexo.Tests.Kernel;

/// <summary>Tests for repair loop options.</summary>
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
