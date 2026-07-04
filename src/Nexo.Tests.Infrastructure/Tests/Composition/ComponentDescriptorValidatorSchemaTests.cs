using FluentAssertions;
using Nexo.Core.Application.Composition.Models;
using Nexo.Infrastructure.Composition;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Composition;

/// <summary>Tests for component descriptor validator schema.</summary>
[Trait("Category", "Integration")]
public sealed class ComponentDescriptorValidatorSchemaTests
{
    [Fact]
    public void Validate_StableWithSchemas_NoSchemaIssues()
    {
        var registry = new CapabilityComponentRegistry();
        var descriptor = new ComponentDescriptor
        {
            Id = "test-stable-with-schemas",
            Capability = "test-cap",
            DisplayName = "Test Stable",
            Summary = "A stable component with both schemas.",
            ImplementationType = "Nexo.Tests.FakeType, Nexo.Tests",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\"}",
            OutputSchema = "{\"type\":\"object\"}"
        };

        var act = () => registry.Register(descriptor);

        act.Should().NotThrow("stable component with both schemas should pass validation");
        registry.GetById("test-stable-with-schemas").Should().NotBeNull();
    }

    [Fact]
    public void Validate_StableWithoutInputSchema_ReportsIssue()
    {
        var registry = new CapabilityComponentRegistry();
        var descriptor = new ComponentDescriptor
        {
            Id = "test-stable-no-input",
            Capability = "test-cap",
            DisplayName = "Test Stable No Input",
            Summary = "A stable component without input schema.",
            ImplementationType = "Nexo.Tests.FakeType, Nexo.Tests",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = null,
            OutputSchema = "{\"type\":\"object\"}"
        };

        var act = () => registry.Register(descriptor);

        act.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("missing-input-schema");
    }

    [Fact]
    public void Validate_StableWithoutOutputSchema_ReportsIssue()
    {
        var registry = new CapabilityComponentRegistry();
        var descriptor = new ComponentDescriptor
        {
            Id = "test-stable-no-output",
            Capability = "test-cap",
            DisplayName = "Test Stable No Output",
            Summary = "A stable component without output schema.",
            ImplementationType = "Nexo.Tests.FakeType, Nexo.Tests",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Stable,
            InputSchema = "{\"type\":\"object\"}",
            OutputSchema = null
        };

        var act = () => registry.Register(descriptor);

        act.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("missing-output-schema");
    }

    [Fact]
    public void Validate_ExperimentalWithoutSchemas_NoSchemaIssues()
    {
        var registry = new CapabilityComponentRegistry();
        var descriptor = new ComponentDescriptor
        {
            Id = "test-experimental-no-schemas",
            Capability = "test-cap-exp",
            DisplayName = "Test Experimental",
            Summary = "An experimental component without schemas.",
            ImplementationType = "Nexo.Tests.FakeType, Nexo.Tests",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Experimental,
            InputSchema = null,
            OutputSchema = null
        };

        var act = () => registry.Register(descriptor);

        act.Should().NotThrow("experimental components do not require schemas");
        registry.GetById("test-experimental-no-schemas").Should().NotBeNull();
    }

    [Fact]
    public void Validate_PlaceholderWithoutSchemas_NoSchemaIssues()
    {
        var registry = new CapabilityComponentRegistry();
        var descriptor = new ComponentDescriptor
        {
            Id = "test-placeholder-no-schemas",
            Capability = "test-cap-placeholder",
            DisplayName = "Test Placeholder",
            Summary = "A placeholder component without schemas.",
            ImplementationType = "Nexo.Tests.FakeType, Nexo.Tests",
            Version = "1.0.0",
            SupportLevel = ComponentSupportLevel.Placeholder,
            InputSchema = null,
            OutputSchema = null
        };

        var act = () => registry.Register(descriptor);

        act.Should().NotThrow("placeholder components do not require schemas");
        registry.GetById("test-placeholder-no-schemas").Should().NotBeNull();
    }
}
