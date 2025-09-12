# RAG Documentation System for C# and .NET

This directory contains the documentation structure for the RAG (Retrieval-Augmented Generation) system that provides offline access to C# and .NET documentation.

## Directory Structure

```
rag-documentation/
├── Languages/
│   └── CSharp/
│       ├── 8.0/
│       │   ├── async-await.md
│       │   ├── pattern-matching.md
│       │   └── nullable-reference-types.md
│       ├── 9.0/
│       │   ├── records.md
│       │   ├── init-only-properties.md
│       │   └── top-level-programs.md
│       └── 10.0/
│           ├── global-usings.md
│           ├── file-scoped-namespaces.md
│           └── record-structs.md
├── Frameworks/
│   ├── NetFramework/
│   │   ├── 4.8/
│   │   │   ├── web-forms.md
│   │   │   ├── wcf.md
│   │   │   └── wpf.md
│   │   └── 4.7.2/
│   │       ├── performance-improvements.md
│   │       └── security-updates.md
│   ├── NetCore/
│   │   ├── 3.1/
│   │   │   ├── aspnet-core.md
│   │   │   ├── entity-framework-core.md
│   │   │   └── dependency-injection.md
│   │   └── 2.1/
│   │       ├── razor-pages.md
│   │       └── signalr.md
│   └── Net5Plus/
│       ├── 5.0/
│       │   ├── system-text-json.md
│       │   ├── source-generators.md
│       │   └── performance-improvements.md
│       ├── 6.0/
│       │   ├── minimal-apis.md
│       │   ├── hot-reload.md
│       │   └── performance-improvements.md
│       └── 8.0/
│           ├── native-aot.md
│           ├── performance-improvements.md
│           └── new-features.md
├── Runtimes/
│   ├── NetFramework/
│   │   ├── 4.8/
│   │   │   ├── runtime-features.md
│   │   │   └── performance.md
│   │   └── 4.7.2/
│   │       ├── runtime-features.md
│   │       └── security.md
│   ├── NetCore/
│   │   ├── 3.1/
│   │   │   ├── runtime-features.md
│   │   │   └── performance.md
│   │   └── 2.1/
│   │       ├── runtime-features.md
│   │       └── performance.md
│   └── Net5Plus/
│       ├── 8.0/
│       │   ├── runtime-features.md
│       │   ├── performance.md
│       │   └── native-aot.md
│       └── 6.0/
│           ├── runtime-features.md
│           └── performance.md
├── API/
│   ├── NetFramework/
│   │   └── 4.8/
│   │       ├── system-collections.md
│   │       ├── system-linq.md
│   │       └── system-threading.md
│   ├── NetCore/
│   │   └── 3.1/
│   │       ├── microsoft-extensions.md
│   │       ├── system-text-json.md
│   │       └── aspnet-core.md
│   └── Net5Plus/
│       └── 8.0/
│           ├── system-collections.md
│           ├── system-linq.md
│           └── microsoft-extensions.md
└── Migrations/
    ├── NetFramework/
    │   └── 4.8/
    │       ├── netcore-migration.md
    │       └── net5-migration.md
    ├── NetCore/
    │   └── 3.1/
    │       ├── net5-migration.md
    │       └── net6-migration.md
    └── Net5Plus/
        └── 8.0/
            ├── net6-migration.md
            └── net7-migration.md
```

## Usage

### 1. Ingest Documentation
```bash
# Ingest C# language documentation
nexo rag ingest ./docs/rag-documentation/Languages/CSharp csharp

# Ingest .NET runtime documentation
nexo rag ingest ./docs/rag-documentation/Runtimes/Net5Plus/8.0 dotnet

# Ingest specific version
nexo rag ingest ./docs/rag-documentation/Frameworks/Net5Plus/8.0 dotnet
```

### 2. Query Documentation
```bash
# General query
nexo rag query "How do I use async await in C#?"

# Code generation context
nexo rag query "Show me how to create a REST API controller" --context CodeGeneration

# Problem solving context
nexo rag query "Why is my async method not working?" --context ProblemSolving

# API reference context
nexo rag query "What are the methods available in System.Collections.Generic.List?" --context APIReference
```

### 3. List Status
```bash
# Show RAG system status
nexo rag list
```

## Features

- **Semantic Search**: Find relevant documentation using vector similarity
- **Context-Aware**: Different contexts for different use cases
- **Version-Specific**: Support for different C# and .NET versions
- **Offline-First**: No internet connection required
- **Extensible**: Easy to add new documentation sources
- **Fast**: In-memory vector storage for quick retrieval

## Adding New Documentation

1. Create markdown files in the appropriate directory structure
2. Use clear, descriptive titles and headers
3. Include code examples where relevant
4. Add appropriate tags and categories
5. Run `nexo rag ingest` to add to the system

## Example Documentation Format

```markdown
# Async and Await in C# 8.0

## Overview
The async and await keywords in C# provide a way to write asynchronous code that looks like synchronous code.

## Syntax
```csharp
public async Task<string> GetDataAsync()
{
    var data = await SomeAsyncOperation();
    return data;
}
```

## Best Practices
- Always use ConfigureAwait(false) in library code
- Don't use async void except for event handlers
- Use Task.Run for CPU-bound work

## Examples
[Include practical examples here]
```
