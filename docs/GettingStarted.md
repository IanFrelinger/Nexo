# Getting Started with Nexo

This guide walks you through installing Nexo, configuring it, running your first command, and extending it with a custom brick.

## Prerequisites

- .NET 8 SDK
- Docker (optional, for multi-platform test execution)
- Ollama (optional, for local LLM) or OpenAI/Azure API key (for cloud providers)

## Install

### Option 1: .NET Tool (recommended)

```bash
dotnet tool install -g Nexo.CLI
nexo --help
```

### Option 2: Run from source

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
dotnet build Nexo.sln
dotnet run --project src/Nexo.CLI -- --help
```

## Configure

Nexo reads configuration from `~/.nexo/config.json`. Create it if needed:

```json
{
  "provider": "openai",
  "model": "gpt-4o-mini"
}
```

Set your API key:

```bash
export OPENAI_API_KEY="sk-..."
```

For local Ollama:

```bash
export provider=ollama
export OLLAMA_MODEL=llama3.2
```

## Run Your First Command

### Validate architecture tests

```bash
nexo validate
```

Runs architecture tests and contract checks in the current directory.

### Analyze code

```bash
nexo analyze --path .
```

Runs code and assembly analyzers.

### Run an agent

```bash
nexo agent --name MyAgent
```

## Embed Nexo in Your Application

Use `AddNexo()` to register the kernel in your host:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddNexo(options =>
        {
            options.PatternStorePath = "/path/to/patterns";  // optional
            options.TrustEnabled = true;                     // optional
        });
    })
    .Build();

// Resolve and use services
var analysisService = host.Services.GetRequiredService<Nexo.Core.Application.Analysis.Ports.IAnalysisService>();
var result = await analysisService.AnalyzeAsync(new DirectoryInfo("."), CancellationToken.None);
```

## Extend with a Custom Brick

1. Create a class that inherits from `Brick`:

```csharp
using Nexo.Core.Domain.Bricks;

public class MyCustomBrick : Brick
{
    public MyCustomBrick()
    {
        Id = "my-custom";
        Name = "My Custom Brick";
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new BrickOutput { ["result"] = "Hello from custom brick" });
    }
}
```

2. Register it via `AddAdaptationBricks` when using the adaptation pipeline, or add it to the brick registry in your host configuration.

## OpenTelemetry Metrics

Enable OpenTelemetry metrics export by calling `AddNexoOpenTelemetry()` after `AddNexo()`:

```csharp
services.AddNexo();
services.AddNexoOpenTelemetry(m => m.AddConsoleExporter());  // or AddOtlpExporter()
```

## Docker

Run the Nexo CLI in Docker:

```bash
make docker-cli
docker run --rm nexo-cli:latest --help
# Run validate against your workspace:
docker run --rm -v $(pwd):/workspace -w /workspace nexo-cli:latest validate
```

Note: If your project has a `global.json` that pins an SDK version, the container (which has only the runtime) may fail. Use the native CLI or temporarily move `global.json` aside when using Docker.

## NuGet Packages

- **Nexo.Hosting** — Library for embedding the Nexo kernel. `dotnet add package Nexo.Hosting`
- **Nexo.CLI** — Global tool. `dotnet tool install -g Nexo.CLI`

Build NuGet packages locally: `make pack` (output in `dist/nuget/`).

## Next Steps

- [Trust & Information Architecture](TrustAndInformationArchitecture.md) — Data sanitization, audit, access boundary
- [Configuration Reference](Configuration.md) — All env vars and config options
- [Architecture](Architecture.md) — System design and layers
