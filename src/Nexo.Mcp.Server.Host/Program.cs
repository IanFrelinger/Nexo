// Nexo MCP stdio server host.
//
// Boots the Nexo kernel and serves the MCP tool bridge over stdio for local AI clients
// (Claude Desktop / Claude Code / IDEs spawn this binary and speak MCP over stdin/stdout).
// Mirrors Nexo.Transport.Grpc.Server.Host: a thin delivery vehicle over Nexo.Hosting.
//
// Configuration (env vars or appsettings.json):
//   Nexo__Mcp__Server__Enabled                 defaults to true here — running this binary IS the opt-in;
//                                              still overridable to false, and refused under AirGapped.
//   Nexo__Mcp__Server__ExposedToolIds__0       allowlist, e.g. "repo.fs.read" — empty exposes zero tools.
//   Nexo__Mcp__Server__RepoRoot                world-snapshot root (defaults to the working directory).
//   Nexo__Mcp__Server__ArgumentOverrides__repo.fs.read__root
//                                              pin caller-visible args, e.g. force 'root' for repo tools.
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Hosting;
using Nexo.Mcp.Server;
using Nexo.Tools.Dev;

var builder = Host.CreateApplicationBuilder(args);

// Lowest-priority defaults: everything (appsettings, env vars, args) overrides them.
builder.Configuration.Sources.Insert(0, new MemoryConfigurationSource
{
    InitialData = new Dictionary<string, string?>
    {
        [$"{NexoMcpServerOptions.SectionPath}:Enabled"] = "true",
    },
});

// stdout belongs to the MCP stdio transport — a single stray log line corrupts the JSON-RPC
// stream, so every console log goes to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddNexo();

// Read-only repo tools available for allowlisting out of the box. Nothing is exposed until the
// operator lists ids in ExposedToolIds; mutating tools (repo.fs.write, git commit, dotnet run)
// are deliberately not pre-registered — hosts that want them must add them explicitly.
builder.Services.AddSingleton<ITool, RepoFsReadTool>();
builder.Services.AddSingleton<ITool, RepoFsListTool>();

builder.Services.AddNexoMcpServer(builder.Configuration).WithStdioServerTransport();

await builder.Build().RunAsync();
