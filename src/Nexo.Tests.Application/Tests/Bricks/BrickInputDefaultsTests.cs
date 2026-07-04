using FluentAssertions;
using Nexo.Core.Application.Bricks;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.Application.Tests.Bricks;

/// <summary>Tests for brick input defaults.</summary>
public sealed class BrickInputDefaultsTests
{
    [Fact]
    public void Apply_populates_missing_optional_input_from_declared_default()
    {
        var brick = new DefaultNameBrick();
        var input = new BrickInput();

        BrickInputDefaults.Apply(brick, input);

        input.Get<string>("name").Should().Be("world");
    }

    [Fact]
    public void Apply_does_not_overwrite_supplied_input_value()
    {
        var brick = new DefaultNameBrick();
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["name"] = "Nexo"
        });

        BrickInputDefaults.Apply(brick, input);

        input.Get<string>("name").Should().Be("Nexo");
    }

    /// <summary>Tests for default name brick.</summary>
    private sealed class DefaultNameBrick : DomainBrick
    {
        public DefaultNameBrick()
        {
            Id = "default-name";
            Name = "Default Name";
            Category = BrickCategory.Transform;
            Description = "Test brick with optional defaulted input.";
            Interface = new BrickInterface
            {
                Inputs =
                [
                    new BrickInputDefinition("name", "string", required: false, defaultValue: "world")
                ],
                Outputs =
                [
                    new BrickOutputDefinition("message", "string")
                ]
            };
        }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new BrickOutput());
    }
}
