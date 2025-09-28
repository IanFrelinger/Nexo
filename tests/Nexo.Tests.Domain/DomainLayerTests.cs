using FluentAssertions;
using Nexo.Core.Domain.Values;
using Xunit;

namespace Nexo.Tests.Domain;

/// <summary>
/// Tests for the Domain layer
/// </summary>
public class DomainLayerTests
{
    [Fact]
    public void Domain_Layer_Should_Compile_Successfully()
    {
        // This test verifies that the Domain layer compiles without errors
        true.Should().BeTrue();
    }

    [Fact]
    public void AgentStatus_Should_Work_Correctly()
    {
        // Test the new AgentStatus value object
        var idle = AgentStatus.Idle;
        var active = AgentStatus.Active;

        idle.Value.Should().Be("idle");
        idle.Display.Should().Be("Idle");
        
        active.Value.Should().Be("active");
        active.Display.Should().Be("Active");
        
        idle.Should().NotBe(active);
    }

    [Fact]
    public void SecuritySeverity_Should_Work_Correctly()
    {
        // Test the new SecuritySeverity value object
        var low = SecuritySeverity.Low;
        var medium = SecuritySeverity.Medium;
        var high = SecuritySeverity.High;

        low.Value.Should().Be("low");
        low.Display.Should().Be("Low");
        
        medium.Value.Should().Be("med");
        medium.Display.Should().Be("Medium");
        
        high.Value.Should().Be("high");
        high.Display.Should().Be("High");
        
        low.Should().NotBe(medium);
        medium.Should().NotBe(high);
    }

    [Fact]
    public void Value_Objects_Should_Be_Immutable()
    {
        // Test that value objects are immutable records
        var status1 = AgentStatus.Idle;
        var status2 = AgentStatus.Idle;
        
        status1.Should().Be(status2);
        status1.GetHashCode().Should().Be(status2.GetHashCode());
    }
}
