# Microservices and Nexo

## Summary

**Yes.** Nexo’s architecture naturally accommodates microservices. The same code can run as a single process or be split across services by implementing the existing ports with remote adapters and, optionally, a distributed message bus.

## Why It Fits

### 1. Ports and Adapters (Hexagonal)

- **Domain** and **application** depend only on interfaces (ports).
- **Infrastructure** implements those interfaces (adapters).
- A “microservice” is just another adapter: e.g. `IGeoTerrainService` implemented by an HTTP client that calls a GeoTerrain service. No core logic changes.

### 2. Bounded Contexts Already Separated

- **Nexo.GeoTerrain** / **Nexo.GeoVector** / **Nexo.GeoWorld** are clear domain boundaries.
- **Nexo.API** is already a deployable REST service (geospatial jobs, webhooks, SSE).
- You can run one API host per context (e.g. GeoTerrain API, GeoVector API) or keep a single API that calls internal “services” via interfaces—those implementations can later be swapped to HTTP/gRPC clients.

### 3. Persistence Per Service

- Storage is behind **IUnitOfWork** and **IRepository&lt;TEntity, TKey&gt;** (see [PERSISTENCE.md](PERSISTENCE.md)).
- Each service can register its own adapter (in-memory, SQLite, Postgres, etc.) and own its data. No shared-database requirement.

### 4. Messaging Abstraction

- **IAgentBus** is async pub/sub between agents. The default implementation is in-memory (`AgentBus`).
- For cross-service communication, add an adapter that implements **IAgentBus** over a distributed bus (e.g. RabbitMQ, Azure Service Bus, Redis Streams). Orchestration and agents stay unchanged.

### 5. Commands and Orchestration

- Use cases are encapsulated in **commands** and **orchestrators**.
- They depend on ports, not on “same process.” You can:
  - Keep orchestrator + agents in one “orchestration service” and call domain capabilities (terrain, vector, world) via HTTP/gRPC, or
  - Replace **IAgentBus** with a distributed implementation and run agents in separate processes if you need that scale.

### 6. Existing API and Contracts

- **Nexo.API** uses REST, Swagger/OpenAPI, and async jobs with callbacks.
- Other “microservices” can expose OpenAPI or gRPC; Nexo’s adapters call them via typed clients. Correlation IDs and metrics in the orchestrator align with distributed tracing (e.g. OpenTelemetry) when crossing service boundaries.

## What You Add for Microservices

| Concern | Today | For multiple services |
|--------|--------|---------------------------|
| **Service boundary** | Single host (CLI + optional API) | One deployable per bounded context (e.g. API per domain) or one API that delegates to back-end services. |
| **Cross-service calls** | In-process (interfaces) | Adapters that implement the same interfaces via HTTP/gRPC. |
| **Messaging** | In-memory **IAgentBus** | Optional: **IAgentBus** adapter over RabbitMQ/Service Bus/Redis. |
| **Persistence** | In-memory or SQLite (e.g. API jobs) | Each service registers its own **IUnitOfWork**/adapter (and optionally **IJobRepository**-style port) with its chosen store. |
| **Discovery / contracts** | Not required (single process) | OpenAPI or gRPC contracts and, if needed, service discovery or a gateway. |

## Conclusion

Nexo does not assume a monolith. Its hexagonal layout, command/orchestration model, and abstractions for persistence and messaging allow you to:

- Run everything in one process (CLI + API),
- Split by bounded context into multiple deployable services, and
- Swap in remote or distributed adapters without rewriting domain or application logic.

So the system **naturally accommodates** microservices; the main work is choosing service boundaries and implementing the corresponding adapters (and optionally a distributed **IAgentBus**).
