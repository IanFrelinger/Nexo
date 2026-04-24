using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.ModelArtifacts;
using Nexo.Core.Application.ModelArtifacts.Ports;
using Nexo.Infrastructure.ModelArtifacts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.ModelArtifacts;

public sealed class ModelArtifactCatalogServiceTests
{
    [Fact]
    public async Task ListAllAsync_MergesAllAvailableSources()
    {
        var a = new StubSource("a", true, [new ModelArtifactRecord("m1", "a", ModelArtifactKind.OllamaModel, 1)]);
        var b = new StubSource("b", false, [new ModelArtifactRecord("m2", "b", ModelArtifactKind.OllamaModel, 2)]);
        var c = new StubSource("c", true, [new ModelArtifactRecord("m3", "c", ModelArtifactKind.OllamaModel, 3)]);

        var sut = new ModelArtifactCatalogService(
            new IModelArtifactCatalogSource[] { a, b, c },
            NullLogger<ModelArtifactCatalogService>.Instance);

        var all = await sut.ListAllAsync();

        all.Should().HaveCount(2);
        all.Select(x => x.Id).Should().BeEquivalentTo("m1", "m3");
    }

    private sealed class StubSource : IModelArtifactCatalogSource
    {
        private readonly bool _available;
        private readonly IReadOnlyList<ModelArtifactRecord> _items;

        public StubSource(string sourceId, bool available, IReadOnlyList<ModelArtifactRecord> items)
        {
            SourceId = sourceId;
            _available = available;
            _items = items;
        }

        public string SourceId { get; }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_available);

        public Task<IReadOnlyList<ModelArtifactRecord>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_items);
    }
}
