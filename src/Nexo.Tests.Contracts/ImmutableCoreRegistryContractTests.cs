using FluentAssertions;
using Nexo.Core.Application.Adaptation.Ports;
using Xunit;

namespace Nexo.Tests.Contracts;

/// <summary>
/// Contract tests for IImmutableCoreRegistry implementations.
/// Every implementation must pass these tests to ensure consistent behavior.
/// </summary>
public abstract class ImmutableCoreRegistryContractTests
{
    protected abstract IImmutableCoreRegistry CreateInstance();

    [Fact]
    public void IsInImmutableCore_CorePath_ReturnsTrue()
    {
        var registry = CreateInstance();
        var corePath = "src/Nexo.Infrastructure/Trust/AccessBoundary.cs";

        registry.IsInImmutableCore(corePath).Should().BeTrue();
    }

    [Fact]
    public void IsInImmutableCore_NonCorePath_ReturnsFalse()
    {
        var registry = CreateInstance();
        var nonCorePath = "application/src/Nexo.CLI/Commands/ImproveCommand.cs";

        registry.IsInImmutableCore(nonCorePath).Should().BeFalse();
    }

    [Fact]
    public void IsInImmutableCore_NullOrEmpty_ReturnsFalse()
    {
        var registry = CreateInstance();

        registry.IsInImmutableCore(null!).Should().BeFalse();
        registry.IsInImmutableCore("").Should().BeFalse();
    }

    [Fact]
    public void IsCoreNamespace_CoreNamespace_ReturnsTrue()
    {
        var registry = CreateInstance();

        registry.IsCoreNamespace("Nexo.Infrastructure/Trust").Should().BeTrue();
    }

    [Fact]
    public void IsCoreNamespace_NonCoreNamespace_ReturnsFalse()
    {
        var registry = CreateInstance();

        registry.IsCoreNamespace("Nexo.CLI/Commands").Should().BeFalse();
    }
}
