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

## Later phases (planned)

- **MCP client** — external MCP servers' tools proxied as namespaced `ITool`s
  (`mcp:{server}:{tool}`) with pinned descriptions, HTTP transports only in v1.
- **A2A server** — agent-card projection of allowlisted `AgentCard`s; task execution through the
  gRPC-facade pattern (barrier identity resolution → registry-validated agent name →
  `IAgentTransport.SendAsync`); synchronous terminal tasks in v1.
- **A2A client** — `A2AAgentTransport : IAgentTransport`; peers are ordinary
  `EndpointDescriptor`s using an `a2a+https://` scheme convention, dispatched by a
  scheme-routing composite so gRPC and A2A coexist.
- **Nexo.API wiring** — `/api/mcp` + `/api/a2a/*` behind an explicit all-verbs auth filter and
  rate limiting; `IngressCatalog` rows; per-tenant capability allowlists.

## Deliberately deferred

Durable A2A tasks + SSE streaming + push notifications; MCP client stdio child processes
(needs a command allowlist design); dynamic tool-set mutation (restart to pick up remote tool
changes); MEAI `AIFunction` bridging; mesh `ICapabilityAdvertisement`-based A2A peer discovery;
migration of the commercial Game Director MCP endpoint onto this layer (breaking for its
clients; needs consumer sign-off).
