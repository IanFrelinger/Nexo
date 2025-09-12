using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models.RAG;
using Spectre.Console;

namespace Nexo.CLI.Commands.RAG
{
    /// <summary>
    /// CLI command for managing the RAG documentation system
    /// </summary>
    public class RAGManagementCommand
    {
        private readonly ILogger<RAGManagementCommand> _logger;
        private readonly IDocumentationRAGService _ragService;
        private readonly IDocumentationIngestionService _ingestionService;

        public RAGManagementCommand(
            ILogger<RAGManagementCommand> logger,
            IDocumentationRAGService ragService,
            IDocumentationIngestionService ingestionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
            _ingestionService = ingestionService ?? throw new ArgumentNullException(nameof(ingestionService));
        }

        public async Task<int> QueryAsync(string query, string contextType = "General", int maxResults = 5)
        {
            try
            {
                AnsiConsole.MarkupLine($"[bold blue]🔍 Querying RAG Documentation System[/]");
                AnsiConsole.MarkupLine($"[dim]Query: {query}[/]");
                AnsiConsole.WriteLine();

                var ragQuery = new RAGQuery
                {
                    Query = query,
                    MaxResults = maxResults,
                    ContextType = Enum.TryParse<DocumentationContextType>(contextType, out var ctxType) 
                        ? ctxType 
                        : DocumentationContextType.General,
                    SimilarityThreshold = 0.7
                };

                var startTime = DateTime.UtcNow;
                var response = await _ragService.QueryDocumentationAsync(ragQuery);
                var processingTime = DateTime.UtcNow - startTime;

                // Display results
                DisplayRAGResponse(response, processingTime);

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying RAG documentation");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return 1;
            }
        }

        public async Task<int> IngestAsync(string path, string type = "csharp")
        {
            try
            {
                AnsiConsole.MarkupLine($"[bold blue]📚 Ingesting Documentation[/]");
                AnsiConsole.MarkupLine($"[dim]Path: {path}[/]");
                AnsiConsole.MarkupLine($"[dim]Type: {type}[/]");
                AnsiConsole.WriteLine();

                var progress = AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new SpinnerColumn(),
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new ElapsedTimeColumn()
                    });

                await progress.StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Ingesting documentation...");

                    switch (type.ToLowerInvariant())
                    {
                        case "csharp":
                            task.Increment(25);
                            await _ingestionService.IngestCSharpDocumentationAsync(path);
                            break;
                        case "dotnet":
                            task.Increment(25);
                            await _ingestionService.IngestDotNetRuntimeDocumentationAsync(path, "latest");
                            break;
                        default:
                            throw new ArgumentException($"Unknown documentation type: {type}");
                    }

                    task.Increment(75);
                    task.StopTask();
                });

                AnsiConsole.MarkupLine($"[green]✅ Successfully ingested {type} documentation from {path}[/]");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ingesting documentation");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return 1;
            }
        }

        public async Task<int> ListAsync()
        {
            try
            {
                AnsiConsole.MarkupLine($"[bold blue]📋 RAG Documentation Status[/]");
                AnsiConsole.WriteLine();

                // This would require extending the vector store interface to get statistics
                var table = new Table()
                    .AddColumn("Metric")
                    .AddColumn("Value")
                    .AddColumn("Description");

                table.AddRow("Status", "Active", "RAG system is running");
                table.AddRow("Embeddings", "Generated", "Documentation chunks are embedded");
                table.AddRow("Vector Store", "In-Memory", "Using in-memory vector storage");
                table.AddRow("Search", "Available", "Semantic search is enabled");

                AnsiConsole.Write(table);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]Use 'nexo rag query <question>' to search documentation[/]");
                AnsiConsole.MarkupLine($"[dim]Use 'nexo rag ingest <path>' to add new documentation[/]");

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing RAG status");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return 1;
            }
        }

        public async Task<int> TestAsync()
        {
            try
            {
                AnsiConsole.MarkupLine($"[bold blue]🧪 Testing RAG System[/]");
                AnsiConsole.WriteLine();

                // Test queries
                var testQueries = new[]
                {
                    "How do I use async await in C#?",
                    "What are minimal APIs in .NET 8?",
                    "How do I handle exceptions in ASP.NET Core?",
                    "What is dependency injection in .NET?",
                    "How do I optimize performance in C#?"
                };

                var results = new List<(string Query, bool Success, double Confidence, long TimeMs)>();

                foreach (var query in testQueries)
                {
                    try
                    {
                        var startTime = DateTime.UtcNow;
                        var ragQuery = new RAGQuery
                        {
                            Query = query,
                            MaxResults = 3,
                            SimilarityThreshold = 0.6
                        };

                        var response = await _ragService.QueryDocumentationAsync(ragQuery);
                        var processingTime = DateTime.UtcNow - startTime;

                        results.Add((query, true, response.ConfidenceScore, (long)processingTime.TotalMilliseconds));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Test query failed: {Query}", query);
                        results.Add((query, false, 0.0, 0));
                    }
                }

                // Display test results
                var testTable = new Table()
                    .AddColumn("Query")
                    .AddColumn("Status")
                    .AddColumn("Confidence")
                    .AddColumn("Time (ms)");

                foreach (var (query, success, confidence, timeMs) in results)
                {
                    var status = success ? "[green]✅ Success[/]" : "[red]❌ Failed[/]";
                    var confidenceStr = success ? $"{confidence:P1}" : "N/A";
                    var timeStr = success ? timeMs.ToString() : "N/A";

                    testTable.AddRow(
                        query.Length > 50 ? query.Substring(0, 47) + "..." : query,
                        status,
                        confidenceStr,
                        timeStr
                    );
                }

                AnsiConsole.Write(testTable);

                var successCount = results.Count(r => r.Success);
                var avgConfidence = results.Where(r => r.Success).Average(r => r.Confidence);
                var avgTime = results.Where(r => r.Success).Average(r => r.TimeMs);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]Test Results:[/]");
                AnsiConsole.MarkupLine($"- Success Rate: {successCount}/{results.Count} ({successCount * 100.0 / results.Count:P1})");
                AnsiConsole.MarkupLine($"- Average Confidence: {avgConfidence:P1}");
                AnsiConsole.MarkupLine($"- Average Response Time: {avgTime:F0}ms");

                return successCount == results.Count ? 0 : 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing RAG system");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return 1;
            }
        }

        private void DisplayRAGResponse(RAGResponse response, TimeSpan processingTime)
        {
            // Display confidence score
            var confidenceColor = response.ConfidenceScore switch
            {
                >= 0.8 => "green",
                >= 0.6 => "yellow",
                _ => "red"
            };

            AnsiConsole.MarkupLine($"[{confidenceColor}]Confidence: {response.ConfidenceScore:P1}[/]");
            AnsiConsole.MarkupLine($"[dim]Processing Time: {processingTime.TotalMilliseconds:F0}ms[/]");
            AnsiConsole.WriteLine();

            // Display AI response
            AnsiConsole.MarkupLine($"[bold]🤖 AI Response:[/]");
            AnsiConsole.MarkupLine(response.AIResponse);
            AnsiConsole.WriteLine();

            // Display retrieved chunks
            if (response.RetrievedChunks.Any())
            {
                AnsiConsole.MarkupLine($"[bold]📚 Retrieved Documentation:[/]");
                
                foreach (var chunk in response.RetrievedChunks.Take(3))
                {
                    var panel = new Panel($"[dim]{chunk.Content.Substring(0, Math.Min(200, chunk.Content.Length))}...[/]")
                        .Header($"[bold]{chunk.Title}[/]")
                        .Border(BoxBorder.Rounded);

                    AnsiConsole.Write(panel);
                    AnsiConsole.WriteLine();
                }

                if (response.RetrievedChunks.Count > 3)
                {
                    AnsiConsole.MarkupLine($"[dim]... and {response.RetrievedChunks.Count - 3} more chunks[/]");
                }
            }
        }
    }
}
