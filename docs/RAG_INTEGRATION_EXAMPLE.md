# RAG Integration Example

This document demonstrates how to integrate and use the RAG (Retrieval-Augmented Generation) system with the Nexo AI agents for enhanced C# and .NET development assistance.

## Setup and Configuration

### 1. Register RAG Services

```csharp
// In Program.cs or Startup.cs
using Nexo.Feature.AI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add RAG services
builder.Services.AddRAGServices(options =>
{
    options.DocumentationPath = "./docs/rag-documentation";
    options.DefaultSimilarityThreshold = 0.7;
    options.DefaultMaxResults = 5;
    options.AutoIngestOnStartup = true;
    options.EnableRAGByDefault = true;
});

var app = builder.Build();
```

### 2. Ingest Documentation

```csharp
// Ingest C# language documentation
var ingestionService = serviceProvider.GetRequiredService<IDocumentationIngestionService>();
await ingestionService.IngestCSharpDocumentationAsync("./docs/rag-documentation/Languages/CSharp");

// Ingest .NET 8.0 documentation
await ingestionService.IngestDotNetRuntimeDocumentationAsync("./docs/rag-documentation/Frameworks/Net5Plus/8.0", "8.0");
```

## Using RAG-Enhanced Agents

### 1. RAG-Enhanced Developer Agent

```csharp
// Create a RAG-enhanced developer agent
var agent = new RAGEnhancedDeveloperAgent(
    new AgentId("rag-dev-001"),
    new AgentName("RAG Developer Agent"),
    new AgentRole("Developer"),
    modelOrchestrator,
    ragService,
    logger
);

// Process a development request
var request = new AgentRequest
{
    Input = "How do I create a REST API controller in ASP.NET Core 8?",
    Type = "Code Generation",
    Priority = "High",
    UseAi = true
};

var response = await agent.ProcessRequestAsync(request);
Console.WriteLine(response.Content);
```

### 2. RAG-Enhanced Code Generation Agent

```csharp
// Create a RAG-enhanced code generation agent
var codeAgent = new RAGEnhancedCodeGenerationAgent(
    new AgentId("rag-code-001"),
    new AgentName("RAG Code Generation Agent"),
    new AgentRole("Code Generator"),
    modelOrchestrator,
    ragService,
    logger
);

// Generate code with RAG enhancement
var codeRequest = new AgentRequest
{
    Input = "Create a service class for user management with dependency injection and async methods",
    Type = "Code Generation",
    Context = new Dictionary<string, object>
    {
        ["TargetFramework"] = "net8.0",
        ["Runtime"] = "Net5Plus"
    }
};

var codeResponse = await codeAgent.ProcessRequestAsync(codeRequest);
Console.WriteLine(codeResponse.Content);
```

### 3. RAG-Enhanced Problem Solving Agent

```csharp
// Create a RAG-enhanced problem solving agent
var problemAgent = new RAGEnhancedProblemSolvingAgent(
    new AgentId("rag-problem-001"),
    new AgentName("RAG Problem Solving Agent"),
    new AgentRole("Problem Solver"),
    modelOrchestrator,
    ragService,
    logger
);

// Solve a problem with RAG enhancement
var problemRequest = new AgentRequest
{
    Input = "My async method is causing a deadlock. How do I fix it?",
    Type = "Problem Solving",
    Context = new Dictionary<string, object>
    {
        ["ErrorType"] = "Deadlock",
        ["Technology"] = "ASP.NET Core"
    }
};

var problemResponse = await problemAgent.ProcessRequestAsync(problemRequest);
Console.WriteLine(problemResponse.Content);
```

## Direct RAG Service Usage

### 1. Query Documentation

```csharp
var ragService = serviceProvider.GetRequiredService<IDocumentationRAGService>();

var query = new RAGQuery
{
    Query = "How do I use async await in C# 8?",
    ContextType = DocumentationContextType.CodeGeneration,
    MaxResults = 5,
    SimilarityThreshold = 0.7,
    Filters = new List<DocumentationFilter>
    {
        new() { Field = "Version", Value = "8.0", Operator = FilterOperator.Equals }
    }
};

var response = await ragService.QueryDocumentationAsync(query);
Console.WriteLine($"AI Response: {response.AIResponse}");
Console.WriteLine($"Confidence: {response.ConfidenceScore:P1}");
Console.WriteLine($"Retrieved Chunks: {response.RetrievedChunks.Count}");
```

### 2. Add Custom Documentation

```csharp
var customChunk = new DocumentationChunk
{
    Id = Guid.NewGuid().ToString(),
    Title = "Custom Best Practice",
    Content = "Always use ConfigureAwait(false) in library code to avoid deadlocks.",
    DocumentationType = "Best Practice",
    Version = "8.0",
    Runtime = "Net5Plus",
    Tags = new List<string> { "async", "await", "deadlock", "best-practice" },
    Categories = new List<string> { "Performance", "Threading" }
};

await ragService.IndexDocumentationAsync(customChunk);
```

## CLI Usage

### 1. Query Documentation

```bash
# General query
nexo rag query "How do I create a minimal API in .NET 8?"

# Code generation context
nexo rag query "Show me how to implement dependency injection" --context CodeGeneration

# Problem solving context
nexo rag query "My application is running out of memory" --context ProblemSolving

# API reference context
nexo rag query "What methods are available in List<T>?" --context APIReference
```

### 2. Ingest Documentation

```bash
# Ingest C# language documentation
nexo rag ingest ./docs/rag-documentation/Languages/CSharp csharp

# Ingest .NET 8.0 documentation
nexo rag ingest ./docs/rag-documentation/Frameworks/Net5Plus/8.0 dotnet

# Ingest specific version
nexo rag ingest ./docs/rag-documentation/Runtimes/Net5Plus/8.0 dotnet
```

### 3. Test RAG System

```bash
# Run comprehensive tests
nexo rag test

# List system status
nexo rag list
```

## Advanced Configuration

### 1. Custom Embedding Service

```csharp
public class CustomEmbeddingService : IDocumentationEmbeddingService
{
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        // Implement custom embedding logic
        // Could use OpenAI, sentence-transformers, or other models
        return await Task.FromResult(new float[100]);
    }

    public async Task<IEnumerable<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts)
    {
        var tasks = texts.Select(GenerateEmbeddingAsync);
        return await Task.WhenAll(tasks);
    }
}

// Register custom service
services.AddSingleton<IDocumentationEmbeddingService, CustomEmbeddingService>();
```

### 2. Custom Vector Store

```csharp
public class CustomVectorStore : IDocumentationVectorStore
{
    // Implement custom vector storage
    // Could use Pinecone, Weaviate, FAISS, or other vector databases
}

// Register custom service
services.AddSingleton<IDocumentationVectorStore, CustomVectorStore>();
```

### 3. Custom Documentation Parser

```csharp
public class CustomDocumentationParser : IDocumentationParser
{
    public async Task<IEnumerable<DocumentationChunk>> ParseDocumentationAsync(string content, DocumentationMetadata metadata)
    {
        // Implement custom parsing logic
        // Could handle different formats like XML, JSON, etc.
        return await Task.FromResult(Enumerable.Empty<DocumentationChunk>());
    }
}

// Register custom service
services.AddSingleton<IDocumentationParser, CustomDocumentationParser>();
```

## Performance Optimization

### 1. Caching

```csharp
// Enable caching in options
services.AddRAGServices(options =>
{
    options.EmbeddingModel.EnableCaching = true;
    options.EmbeddingModel.CacheSizeLimit = 1000;
});
```

### 2. Batch Processing

```csharp
// Process multiple queries in batch
var queries = new[]
{
    new RAGQuery { Query = "Query 1" },
    new RAGQuery { Query = "Query 2" },
    new RAGQuery { Query = "Query 3" }
};

var tasks = queries.Select(q => ragService.QueryDocumentationAsync(q));
var responses = await Task.WhenAll(tasks);
```

### 3. Filtering

```csharp
// Use filters to narrow down results
var query = new RAGQuery
{
    Query = "async await",
    Filters = new List<DocumentationFilter>
    {
        new() { Field = "Version", Value = "8.0", Operator = FilterOperator.Equals },
        new() { Field = "DocumentationType", Value = "Language", Operator = FilterOperator.Equals }
    }
};
```

## Monitoring and Debugging

### 1. Logging

```csharp
// Enable detailed logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

### 2. Metrics

```csharp
// Access RAG metrics from response metadata
var response = await ragService.QueryDocumentationAsync(query);

var metrics = new
{
    Confidence = response.ConfidenceScore,
    ProcessingTime = response.ProcessingTimeMs,
    ChunksRetrieved = response.RetrievedChunks.Count,
    RAGUsed = response.Metadata?.GetValueOrDefault("RAGUsed", false)
};
```

## Best Practices

1. **Use Appropriate Context Types**: Choose the right context type for your use case
2. **Set Proper Similarity Thresholds**: Balance between relevance and recall
3. **Filter Results**: Use filters to narrow down to relevant documentation
4. **Monitor Performance**: Track confidence scores and processing times
5. **Regular Updates**: Keep documentation up to date
6. **Test Regularly**: Use the test command to verify system health
7. **Cache Embeddings**: Enable caching for better performance
8. **Batch Operations**: Process multiple items together when possible

## Troubleshooting

### Common Issues

1. **Low Confidence Scores**: Check if documentation is relevant and up to date
2. **Slow Performance**: Enable caching and optimize embedding generation
3. **No Results**: Lower similarity threshold or check query terms
4. **Memory Issues**: Reduce cache size or use external vector store
5. **Poor Quality Responses**: Ensure documentation is well-structured and comprehensive

### Debug Commands

```bash
# Test the system
nexo rag test

# Check system status
nexo rag list

# Query with verbose output
nexo rag query "your question" --verbose
```
