using FluentAssertions;
using Nexo.BackgroundAgents.DataSensitivity;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.DataSensitivity;

/// <summary>Tests for data sensitivity registry gap coverage.</summary>
public sealed class DataSensitivityRegistryGapCoverageTests
{
    [Fact]
    public void Register_rejects_null_and_primitive_conflicts()
    {
        var registry = new DataSensitivityRegistry();

        var actNull = () => registry.Register(null!);
        actNull.Should().Throw<ArgumentNullException>();

        var actPrimitive = () => registry.Register(new ConfigurableSensitivityLevel(
            "Public", "Public", 0, false, false, false, false, "dup"));
        actPrimitive.Should().Throw<ArgumentException>().WithMessage("*primitive*");
    }

    [Fact]
    public void Register_rejects_duplicate_custom_levels()
    {
        var registry = new DataSensitivityRegistry();
        registry.Register(new ConfigurableSensitivityLevel(
            "PartnerA", "Partner A", 50, false, false, false, false, "partner"));

        var act = () => registry.Register(new ConfigurableSensitivityLevel(
            "PartnerA", "Partner A", 55, false, false, false, false, "dup"));
        act.Should().Throw<ArgumentException>().WithMessage("*already registered*");
    }

    [Fact]
    public void Unregister_and_GetByName_handle_custom_and_primitive_names()
    {
        var registry = new DataSensitivityRegistry();
        registry.Register(new ConfigurableSensitivityLevel(
            "PartnerA", "Partner A", 50, false, false, false, false, "partner"));

        registry.Unregister("Public").Should().BeFalse();
        registry.Unregister("").Should().BeFalse();
        registry.Unregister("PartnerA").Should().BeTrue();
        registry.GetByName("PartnerA").Should().BeNull();
        registry.GetByName("public").Should().NotBeNull();
    }

    [Fact]
    public void CanAccess_requires_both_levels_and_compares_sensitivity_values()
    {
        var registry = new DataSensitivityRegistry();
        var publicLevel = registry.GetByName("Public")!;
        var internalLevel = registry.GetByName("Internal")!;

        registry.CanAccess(publicLevel, publicLevel).Should().BeTrue();
        registry.CanAccess(publicLevel, internalLevel).Should().BeFalse();

        var actAgent = () => registry.CanAccess(null!, publicLevel);
        actAgent.Should().Throw<ArgumentNullException>();

        var actData = () => registry.CanAccess(publicLevel, null!);
        actData.Should().Throw<ArgumentNullException>();
    }
}
