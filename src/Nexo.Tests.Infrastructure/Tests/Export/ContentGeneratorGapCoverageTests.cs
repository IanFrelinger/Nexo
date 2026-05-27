using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Export;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Export;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Export;

public sealed class ContentGeneratorGapCoverageTests
{
    [Fact]
    public async Task GenerateAsync_creates_configured_variation_count()
    {
        var provider = new Mock<IProviderFactory>();
        provider.Setup(p => p.ExecuteLLMAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("generated-content");

        var generator = new ContentGenerator(provider.Object, NullLogger<ContentGenerator>.Instance);
        var brick = new TestBrick("gen-b1", "Generator Brick");

        var result = await generator.GenerateAsync(
            brick,
            new GenerationConfig
            {
                VariationsPerItem = 3,
                Provider = "openai",
                Model = "gpt-4",
            });

        result.Variations.Should().HaveCount(3);
        result.Variations.Should().OnlyContain(v => v.Content == "generated-content");
        result.Variations.Select(v => v.Metadata!["brickId"]).Should().AllBeEquivalentTo("gen-b1");
        provider.Verify(
            p => p.ExecuteLLMAsync(
                "openai",
                It.Is<string>(s => s.Contains("Generation")),
                It.Is<string>(s => s.Contains("Generator Brick") && s.Contains("ISO-9001")),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task GenerateAsync_includes_variation_index_in_metadata()
    {
        var provider = new Mock<IProviderFactory>();
        provider.Setup(p => p.ExecuteLLMAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("v");

        var generator = new ContentGenerator(provider.Object, NullLogger<ContentGenerator>.Instance);

        var result = await generator.GenerateAsync(
            new TestBrick("idx-b1", "Indexed"),
            new GenerationConfig { VariationsPerItem = 2 });

        result.Variations[0].Metadata!["index"].Should().Be(0);
        result.Variations[1].Metadata!["index"].Should().Be(1);
    }

    private sealed class TestBrick : Brick
    {
        public TestBrick(string id, string name)
        {
            Id = id;
            Name = name;
            Description = "content generator test";
            Category = BrickCategory.Generation;
            DomainKnowledge = new DomainKnowledge { Standards = ["ISO-9001"] };
            Implementations = new BrickImplementations();
        }

        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new BrickOutput());
    }
}
