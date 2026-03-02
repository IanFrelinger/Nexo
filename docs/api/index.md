# Nexo API Reference

API documentation for the Nexo AI-enhanced development orchestration platform.

## Libraries

| Library | Description |
|---------|-------------|
| **Nexo.Hosting** | Hosting extensions for embedding the Nexo kernel. Call `AddNexo()` to register orchestration, adaptation, persistence, trust, and agent services. |
| **Nexo.Core.Application** | Use cases (MediatR handlers), validation, analysis, and ports. |
| **Nexo.Core.Domain** | Domain entities, bricks, behaviors, agents. |
| **Nexo.Abstractions** | Core interfaces and abstractions. |
| **Nexo.Infrastructure** | ProviderFactory (LLM), persistence, adaptation, IO, execution. |

## Quick Start

```csharp
using Microsoft.Extensions.Hosting;
using Nexo.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => services.AddNexo())
    .Build();

var validationService = host.Services.GetRequiredService<IValidationService>();
var result = await validationService.ValidateAsync(filter: null, progress: null, CancellationToken.None);
```

See [Getting Started](GettingStarted.md) and [Architecture](Architecture.md) for more.
