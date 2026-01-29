using System.Text.Json;
using FluentAssertions;
using Moq;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.RAG;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.RAG;

public class RAGToolTests
{
    [Fact]
    public void Id_IsRagSearch()
    {
        var tool = new RAGTool(Mock.Of<IRAGService>());
        tool.Id.Should().Be("rag_search");
    }

    [Fact]
    public void Schema_DescribesQuery()
    {
        var tool = new RAGTool(Mock.Of<IRAGService>());
        tool.Schema.Id.Should().Be("rag_search");
        tool.Schema.Description.Should().Contain("RAG");
    }

    [Fact]
    public async Task InvokeAsync_CallsRAGService_AndReturnsPayload()
    {
        var service = new Mock<IRAGService>();
        service.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new VectorSearchResult("doc1", "found text", 0.9, null) });
        var tool = new RAGTool(service.Object);
        var args = JsonSerializer.SerializeToElement(new { query = "test query" });
        var call = new ToolCall("rag_search", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().Contain(l => l.Contains("RAG search"));
        result.Payload.Should().NotBeNull();
        service.Verify(s => s.SearchAsync("test query", 5, 0.7, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
