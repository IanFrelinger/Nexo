# Ashlar API Reference

API documentation for the Ashlar AI-enhanced development orchestration platform.

## Libraries

| Library | Description |
|---------|-------------|
| **Ashlar.Hosting** | Hosting extensions for embedding the Ashlar kernel. Call `AddAshlar()` to register orchestration, adaptation, persistence, trust, and agent services. |
| **Ashlar.API** | ASP.NET Core host with minimal API endpoints, static file serving (SPA), and optional API-key auth. |
| **Ashlar.Sdk** | Client SDK registration (`AddAshlarSdk(baseUrl, ...)`). |
| **Ashlar.Client** | HTTP client (`IAshlarClient`) for calling a running Ashlar API. |
| **Ashlar.Core.Application** | Use cases (MediatR handlers), validation, analysis, and ports. |
| **Ashlar.Core.Domain** | Domain entities, bricks, behaviors, agents. |
| **Ashlar.Abstractions** | Core interfaces and abstractions. |
| **Ashlar.Infrastructure** | ProviderFactory (LLM), persistence, adaptation, IO, execution. |

## REST Endpoints

Endpoints are registered via `MapAshlarEndpoints()` in `Ashlar.API`. Most live under `/api`; the health check is at the root. The API is **unversioned in `v0.x`** (no `/v1/` prefix until `1.0`); which of these routes are the documented surface with a breaking-change promise, and how breaking changes are announced, is in [versioning.md](versioning.md).

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
| GET | `/ready` | Readiness (host started; root, not under `/api`) |
| GET | `/api/bricks` | List authored bricks |
| GET | `/api/bricks/{brickId}` | Get an authored brick |
| POST | `/api/bricks/{brickId}/execute` | Execute an authored brick |
| GET | `/api/copilot/tasks/{taskId}` | Get one copilot task |
| POST | `/api/orgs` · GET `/api/orgs/{orgId}` | Create / read an organization |
| GET/POST | `/api/orgs/{orgId}/members` | List / add organization members |
| GET | `/api/runtime-studio/metrics` | Runtime Studio objective metrics |
| GET | `/api/support/diagnostics` | Support diagnostics bundle |
| GET | `/api/usage/summary` | Usage summary |
| GET/PUT | `/api/workloads`, `/api/workloads/{workloadId}/replicas` | Workload scaling (list, read, set replicas) |
| — | `/api/ide/*` | IDE session surface (models, agents, session, chat, chat/stream, edit, plan, director, runs) |

## Hosting Quick Start (Embedding)

```csharp
using Microsoft.Extensions.Hosting;
using Ashlar.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => services.AddAshlar())
    .Build();

var validationService = host.Services.GetRequiredService<IValidationService>();
var result = await validationService.ValidateAsync(filter: null, progress: null, CancellationToken.None);
```

## AshlarHostingOptions

`AddAshlar()` accepts an optional configuration callback:

| Option | Description | Default |
|--------|-------------|---------|
| `DeploymentProfile` | Module profile (`Full`, `Server`, `Edge`, `AirGapped`, `System`, `SecureWorkstation`) | `Full` (or `ASHLAR_DEPLOYMENT_PROFILE`) |
| `PatternStorePath` | LiteDB pattern store file path; sibling state files are co-located with it | `<state dir>/ashlar-patterns.db` (`ASHLAR_STATE_DIR`, else `<repo root>/.ashlar/state`) |
| `TrustEnabled` | Enable trust & sanitization | `false` |
| `RegisterBackgroundAgentHostedService` | Register background agent as hosted service | `false` |
| `DisableObservationPipeline` | Skip observation pipeline registration | `false` |
| `ObservationFailOpen` | Continue on observation store errors | `false` |
| `UseAdaptiveLoadBalancing` | Enable adaptive load balancing | `false` |
| `ExecutionRemoteUrl` | Remote execution endpoint URL | unset |
| `StrictMode` | Strict mode configuration (fail-fast + verbose diagnostics) | disabled |

See [Getting Started](../GettingStarted.md) and [Architecture](../Architecture.md) for more.
