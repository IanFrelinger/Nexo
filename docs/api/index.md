# Nexo API Reference

API documentation for the Nexo AI-enhanced development orchestration platform.

## Libraries

| Library | Description |
|---------|-------------|
| **Nexo.Hosting** | Hosting extensions for embedding the Nexo kernel. Call `AddNexo()` to register orchestration, adaptation, persistence, trust, and agent services. |
| **Nexo.API** | ASP.NET Core host with minimal API endpoints, static file serving (SPA), and optional API-key auth. |
| **Nexo.Sdk** | Client SDK registration (`AddNexoSdk(baseUrl, ...)`). |
| **Nexo.Client** | HTTP client (`INexoClient`) for calling a running Nexo API. |
| **Nexo.Core.Application** | Use cases (MediatR handlers), validation, analysis, and ports. |
| **Nexo.Core.Domain** | Domain entities, bricks, behaviors, agents. |
| **Nexo.Abstractions** | Core interfaces and abstractions. |
| **Nexo.Infrastructure** | ProviderFactory (LLM), persistence, adaptation, IO, execution. |

## REST Endpoints

Endpoints are registered via `MapNexoEndpoints()` in `Nexo.API`. Most live under `/api`; the health check is at the root.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check (returns `{status, timestamp}`) |
| POST | `/api/agent` | Run an agent |
| POST | `/api/validate` | Run validation |
| POST | `/api/orchestrate` | Run orchestration |
| POST | `/api/copilot/task` | Submit a copilot task (with audit context) |
| GET | `/api/copilot/tasks` | List copilot tasks |
| GET | `/api/status` | Background agent status |
| POST | `/api/execution/build` | Build a container image |
| POST | `/api/execution/run` | Run a container |
| GET | `/api/capabilities` | Node capability manifest |
| GET | `/api/security/advisory` | Exposure profile / advisory |
| GET | `/api/trust/status` | Trust boundary status |
| GET | `/api/trust/dashboard` | Trust + audit dashboard |
| POST | `/api/trust/pause` | Pause/resume observation |
| POST | `/api/trust/rule` | Add allow/deny trust rules |
| POST | `/api/director/run` | Run a director iteration (produces a daily) |
| GET | `/api/director/dailies` | List director dailies |
| GET | `/api/director/dailies/{dailyId}` | Get a specific daily |
| GET | `/api/background-agents/summary` | Background agent health summary |
| GET | `/api/knowledge/query` | Knowledge timeline query |
| GET | `/api/preferences` | Get server-side user preferences |
| POST | `/api/preferences` | Save server-side user preferences |
| GET | `/api/activity/feed` | Recent background agent + audit activity |
| POST | `/api/changelog/generate` | Generate project changelog summary |
| GET | `/api/onboarding/status` | First-run setup status (provider availability) |

## Hosting Quick Start (Embedding)

```csharp
using Microsoft.Extensions.Hosting;
using Nexo.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => services.AddNexo())
    .Build();

var validationService = host.Services.GetRequiredService<IValidationService>();
var result = await validationService.ValidateAsync(filter: null, progress: null, CancellationToken.None);
```

## NexoHostingOptions

`AddNexo()` accepts an optional configuration callback:

| Option | Description | Default |
|--------|-------------|---------|
| `DeploymentProfile` | Module profile (`Full`, `Server`, `Edge`, `AirGapped`, `System`) | `Full` (or `NEXO_DEPLOYMENT_PROFILE`) |
| `PatternStorePath` | LiteDB pattern store file path; sibling state files are co-located with it | `<state dir>/nexo-patterns.db` (`NEXO_STATE_DIR`, else `<repo root>/.nexo/state`) |
| `TrustEnabled` | Enable trust & sanitization | `false` |
| `RegisterBackgroundAgentHostedService` | Register background agent as hosted service | `false` |
| `DisableObservationPipeline` | Skip observation pipeline registration | `false` |
| `ObservationFailOpen` | Continue on observation store errors | `false` |
| `UseAdaptiveLoadBalancing` | Enable adaptive load balancing | `false` |
| `ExecutionRemoteUrl` | Remote execution endpoint URL | unset |
| `StrictMode` | Strict mode configuration (fail-fast + verbose diagnostics) | disabled |

See [Getting Started](../GettingStarted.md) and [Architecture](../Architecture.md) for more.
