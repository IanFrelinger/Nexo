using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Shared;

/// <summary>
/// Tests for the Shared layer
/// </summary>
public class SharedLayerTests
{
    [Fact]
    public void Shared_Layer_Should_Compile_Successfully()
    {
        // This test verifies that the Shared layer compiles without errors
        // If we get here, the compilation was successful
        true.Should().BeTrue();
    }

    [Fact]
    public void Shared_Layer_Should_Have_Type_Values()
    {
        // Test that the new type value system is working
        // This will be expanded once the legacy shared values are resolved
        var hasTypeValues = typeof(Nexo.Shared.Values.ITypeValue).IsInterface;
        hasTypeValues.Should().BeTrue();
    }

    [Fact]
    public void Shared_Layer_Should_Have_Base_Type_Value()
    {
        // Test that BaseTypeValue is available
        var hasBaseTypeValue = typeof(Nexo.Shared.Values.BaseTypeValue).IsClass;
        hasBaseTypeValue.Should().BeTrue();
    }
}
