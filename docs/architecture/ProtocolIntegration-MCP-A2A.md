# Protocol Integration: MCP and A2A

Nexo speaks the two industry-standard agent protocols through Tier-2 adapter projects:

- **MCP (Model Context Protocol)** — AI clients (Claude, IDEs, other agents) discover and call
  tools. Nexo acts as an **MCP server** (exposing allowlisted Nexo tools) and, in a later phase,
  as an **MCP client** (consuming external MCP servers' tools as `ITool`s).
- **A2A (Agent2Agent)** — peer agents discover each other via agent cards and delegate tasks.
  Planned: Nexo as an **A2A server** (external agents delegate to allowlisted Nexo agents) and as
  an **A2A client** (`A2AAgentTransport` behind the existing endpoint routing).

Both protocols use the official SDKs (`ModelContextProtocol` 2.x, stable; `A2A` 1.0 preview) —
the hand-rolled JSON-RPC endpoint in the commercial Game Director vertical predates this layer
and is unaffected.

## Design rules (all phases)

1. **Kernel spine stays clean.** `Nexo.Abstractions`, `Nexo.Core.*`, `Nexo.Contracts`, and
   `Nexo.Hosting` never reference protocol SDK packages. Hosts opt in by calling the
   `AddNexo*` extensions from the adapter projects (same pattern as
   `Nexo.Transport.Grpc.Server.Host`). This also keeps preview packages out of the
   `Nexo.Hosting` NuGet pack graph.
2. **Fail-closed everywhere.** Every surface has `Enabled=false` defaults, explicit allowlists
   that default to empty, `ValidateOnStart` options validation, and a hard refusal to enable
   under `NEXO_DEPLOYMENT_PROFILE=airgapped`.
3. **Existing seams, not parallel plumbing.** Tools flow through `ITool`/`ToolSchema`
   (`src/Nexo.Abstractions`), agents will flow through `IAgentTransport` + endpoint routing,
   and HTTP exposure lands behind the Nexo API middleware chain under `/api/...`.

## Phase 1 (this phase): Nexo as MCP server

Projects:

| Project | Role |
|---|---|
| `src/Nexo.Mcp.Server/` | Protocol-agnostic bridge: catalog, policy gate, handlers over the official SDK |
| `src/Nexo.Mcp.Server.Host/` | Standalone **stdio** host for local AI clients (Claude Desktop, Claude Code, IDEs) |
| `src/Nexo.Mcp.Server.Tests/` | Unit tests |

### How exposure works

Tools become callable over MCP only when **both** are true:

1. The host registers the tool in DI: `services.AddSingleton<ITool, RepoFsReadTool>();`
2. The operator allowlists its id: `Nexo__Mcp__Server__ExposedToolIds__0=repo.fs.read`

Every `tools/call` then flows through `NexoMcpToolBridge`, which applies in order:

1. **Catalog routing** — only allowlisted tools exist; unknown names are JSON-RPC
   `invalid params` protocol errors.
2. **Argument overrides** — operator-pinned arguments replace caller-supplied ones
   (`Nexo__Mcp__Server__ArgumentOverrides__repo.fs.read__root=/srv/repo`). Use this for the
   `repo.fs.*` tools, whose `root` argument would otherwise let a remote caller point them at an
   arbitrary directory.
3. **Policy gate** — `IMcpInvocationGate` (default: every DI-registered `IPolicy` must approve,
   evaluated against the canonical tool id, mirroring `AgentHost`'s approve+audit pair, which MCP
   traffic otherwise bypasses). Denials come back as `isError` tool results.
4. **Concurrency ceiling** — `MaxConcurrentToolCalls` (default 8); Nexo tools were written for a
   single agent loop and are not assumed reentrant.
5. **Audit logging** — caller principal, canonical id, duration, and outcome.

Startup validation (`McpCatalogStartupValidator`) fails the host boot when the surface is enabled
but the catalog is invalid: allowlisted id with no registered tool, an input schema that is not a
JSON object schema, or two ids collapsing to the same sanitized MCP name (`repo.fs.read` →
`repo_fs_read`; MCP-strict clients reject dots).

### Running the stdio host

```bash
dotnet run --project src/Nexo.Mcp.Server.Host -- \
  Nexo__Mcp__Server__ExposedToolIds__0=repo.fs.read \
  Nexo__Mcp__Server__ExposedToolIds__1=repo.fs.list \
  Nexo__Mcp__Server__RepoRoot=/srv/repo
```

(Values can equally come from environment variables or `appsettings.json`; `Enabled` defaults to
`true` in this host only — running the binary is the opt-in — and all logging goes to stderr
because stdout carries the protocol stream.)

Claude Code registration example:

```bash
claude mcp add nexo -- dotnet run --project src/Nexo.Mcp.Server.Host
```

The host pre-registers only the read-only repo tools (`repo.fs.read`, `repo.fs.list`) for
allowlisting; mutating tools (`repo.fs.write`, `repo.git.commit`, `dotnet.run`, …) must be added
by the operator explicitly in their own host.

### HTTP exposure

`AddNexoMcpServer(configuration).WithHttpTransport()` + `MapNexoMcpEndpoint()` (defaults to
`/api/mcp`, streamable HTTP). The Nexo.API wiring lands in the application-layer phase; the
mapping helper deliberately refuses the commercial endpoint's `AllowAnonymous()` pattern and maps
nothing at all while disabled.

## Phase 2 (this phase): Nexo as MCP client

Projects: `src/Nexo.Mcp.Client/` (+ `src/Nexo.Mcp.Client.Tests/`, whose round-trip suite runs a
real `McpClient` against the Phase-1 server bridge over in-memory pipes).

External MCP servers are configured under `Nexo:Mcp:Client` (streamable HTTP only in v1) and
their tools surface as `ITool`s through the `IToolSource` seam:

1. **Connect + pin at startup.** `McpClientConnectionManager` (a hosted service) dials each
   configured server, lists tools (optionally narrowed by a per-server `AllowedTools` list), and
   pins each definition — name, description, raw JSON schema — for the process lifetime.
2. **Namespaced ids.** Proxies register as `mcp:{server}:{tool}`, so a remote server can never
   shadow a native tool id (`CapabilityRegistry` registration is last-wins by id).
3. **Drift faults, never follows.** A periodic re-list compares against the pins; a changed or
   vanished definition marks the tool faulted (withdrawn from toolboxes, calls fail with the
   reason) until a restart re-pins. Remote servers do not get to redefine an agent's tools
   mid-flight.
4. **Failures degrade, not crash.** An unreachable server contributes zero tools; remote
   `isError` results and transport failures come back as error payloads (repo tool convention),
   visible to the calling model.
5. **Secrets via environment.** Per-server API keys are named by env var
   (`ApiKeyHeader`/`ApiKeyEnvVar` pair); a referenced-but-unset variable fails startup.

Toolboxes pick proxies up through `Nexo.Abstractions.IToolSource`:
`RepoFsToolboxFactory.CreateMinimal/CreateWithBuildTest` accept `extraTools`, and
`SelfExtendRunnerAdapter` folds all DI-registered `IToolSource`s in per cycle. Hosts wire it with
`services.AddNexoMcpClient(configuration)`.

```bash
Nexo__Mcp__Client__Enabled=true
Nexo__Mcp__Client__Servers__0__Name=github
Nexo__Mcp__Client__Servers__0__Url=https://mcp.example.com/mcp
Nexo__Mcp__Client__Servers__0__ApiKeyHeader=Authorization
Nexo__Mcp__Client__Servers__0__ApiKeyEnvVar=GITHUB_MCP_TOKEN
Nexo__Mcp__Client__Servers__0__AllowedTools__0=search_issues
```

## Phase 3 (this phase): A2A server core + client transport

Projects: `src/Nexo.Transport.A2A/` (client transport), `src/Nexo.Transport.A2A.Server/`
(server core), test satellites for both (the server suite drives the real client transport
through the really-mapped endpoints over an ASP.NET TestServer). Built on the official `A2A` +
`A2A.AspNetCore` SDK (1.0.0-preview2, A2A v1.0 spec) — both projects are `IsPackable=false`
until a stable SDK ships.

### A2A client (`A2AAgentTransport : IAgentTransport`)

Peers are ordinary routing endpoints with an **`a2a+` scheme prefix**
(`a2a+https://peer.example.com/api/a2a/agent`): capability routing, health filtering, and
barrier levels on `EndpointDescriptor` work unchanged, and no kernel public API moved. The
runtime's `AddNexoRuntimeTransport` now wraps the remote side in a scheme-dispatching composite
whenever `AgentTransportSchemeRegistration`s exist in DI — with none registered the composition
is byte-for-byte the old gRPC-only behavior. Hosts opt in with
`services.AddNexoA2ATransport(configuration)` **before** `AddNexo()`
(`Nexo:A2A:Transport:Enabled=true`; refused under AirGapped).

Correlation, span, and ambient barrier context propagate as protocol metadata
(`nexo.correlationId`, `nexo.barrier`, …) on both the message and the request — the A2A
counterpart of the gRPC transport's `x-nexo-*` headers, with the same audit-log event. Remote
task states map to typed results (`a2a.task.failed`, `a2a.timeout`, …); transport-level
`HttpRequestException`s honor `MaxRetries`, task-level failures are never retried. Per-endpoint
API keys are env-var named (`Nexo:A2A:Transport:Endpoints:0:ApiKeyHeader/ApiKeyEnvVar`).

### A2A server (`src/Nexo.Transport.A2A.Server/`)

- **Exposure**: explicit `ExposedAgentIds` allowlist, plus opt-in
  `ExposeByCoordinationProtocol` for agents whose domain card declares the `a2a` coordination
  protocol. Deny-by-default; enabled-with-zero-agents refuses to boot.
- **Execution**: `NexoA2AAgentHandler` deliberately mirrors the gRPC facade — agent identity is
  fixed at construction from the allowlisted descriptor and execution flows through
  `IAgentTransport.SendAsync` with a bounded budget (`AgentConstraints.MaxExecutionTime`, else
  `DefaultExecutionTimeout`). It never touches the reflection-scanning
  `RunAgentCommand`/`AgentExecutorAdapter` path.
- **Cards**: `INexoA2ACardProjector` builds spec cards (behaviors → skills,
  `streaming=false, pushNotifications=false` in the synchronous v1). The domain's own
  `Nexo.Core.Domain.Agents.AgentCard` is untouched; the colliding spec type never leaves the
  adapter. Hosts implement `INexoA2AAgentCatalog` over their agent registry (the adapter cannot
  reference the domain model per transport layering rules).
- **Mapping**: `MapNexoA2AEndpoints()` maps, per exposed agent, a JSON-RPC endpoint + card at
  `/api/a2a/{agentId}`, plus the **primary** agent's card at `/.well-known/agent-card.json`
  (explicit `PrimaryAgentId` required when several agents are exposed). RPC and card endpoint
  builders are returned separately so hosts can auth-gate them differently
  (`AllowAnonymousAgentCard` is the public-discovery opt-out, default off).
- **Tasks**: synchronous terminal tasks in an in-memory per-agent store — `tasks/get` works
  within the process lifetime; durable tasks/streaming are deferred.

## Later phases (planned)

- **Nexo.API wiring** — `/api/mcp` + `/api/a2a/*` behind an explicit all-verbs auth filter and
  rate limiting; `IngressCatalog` rows; per-tenant capability allowlists; an
  `INexoA2AAgentCatalog` adapter over `IAgentRegistry`.

## Deliberately deferred

Durable A2A tasks + SSE streaming + push notifications; MCP client stdio child processes
(needs a command allowlist design); dynamic tool-set mutation (restart to pick up remote tool
changes); MEAI `AIFunction` bridging; mesh `ICapabilityAdvertisement`-based A2A peer discovery;
migration of the commercial Game Director MCP endpoint onto this layer (breaking for its
clients; needs consumer sign-off).
