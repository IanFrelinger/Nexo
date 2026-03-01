using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI.Commands.BackgroundAgent;
using Nexo.BackgroundAgents.RAG;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

public class RAGCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestStatsSucceeds();
            await TestStatsFormatJson();
            await TestSearchEmpty();
            return new TestResult
            {
                Name = nameof(RAGCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All RAGCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(RAGCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(RAGCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private static (IRAGService rag, IKnowledgeBaseIndexer indexer) CreateRAG()
    {
        var embedding = new TokenEmbeddingGenerator();
        var store = new InMemoryVectorStore(null);
        var rag = new RAGService(store, embedding);
        var indexer = new KnowledgeBaseIndexer(rag, null);
        return (rag, indexer);
    }

    private async Task TestStatsSucceeds()
    {
        var (rag, indexer) = CreateRAG();
        var logger = new Mock<ILogger<RAGCommand>>();
        var command = new RAGCommand(rag, indexer, logger.Object);
        var exitCode = await command.StatsAsync(false);
        AssertEqual(0, exitCode);
    }

    private async Task TestStatsFormatJson()
    {
        var (rag, indexer) = CreateRAG();
        var logger = new Mock<ILogger<RAGCommand>>();
        var command = new RAGCommand(rag, indexer, logger.Object);
        var exitCode = await command.StatsAsync(true);
        AssertEqual(0, exitCode);
    }

    private async Task TestSearchEmpty()
    {
        var (rag, indexer) = CreateRAG();
        var logger = new Mock<ILogger<RAGCommand>>();
        var command = new RAGCommand(rag, indexer, logger.Object);
        var exitCode = await command.SearchAsync("test query", 5, 0.0, null, false);
        AssertEqual(0, exitCode);
    }
}
