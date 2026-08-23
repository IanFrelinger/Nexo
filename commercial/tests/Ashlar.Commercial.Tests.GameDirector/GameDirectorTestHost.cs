using GameDirector.Bricks;
using GameDirector.Domain;
using GameDirector.Mcp;
using GameDirector.Mcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Trust;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Client;
using Ashlar.Contracts;
using Ashlar.Infrastructure.Execution;

namespace Ashlar.Tests.GameDirector;

/// <summary>Game director test host.</summary>
internal static class GameDirectorTestHost
{
    /// <summary>New audit db path.</summary>
    public static string NewAuditDbPath() =>
        Path.Combine(Path.GetTempPath(), $"gd-audit-{Guid.NewGuid():N}.db");

    /// <summary>Creates audit log.</summary>
    public static LiteDbDataDecisionAuditLog CreateAuditLog() =>
        new(NewAuditDbPath());

    /// <summary>Creates context.</summary>
    /// <param name=""test"">"test".</param>
    public static IExecutionContext CreateContext(string agentId = "test") =>
        new Ashlar.Infrastructure.Execution.ExecutionContext { AgentId = agentId };

    public static BrickRegistry CreateBrickRegistry(
        IDataDecisionAuditLog? audit = null,
        IModel? model = null,
        IForgeSessionReader? sessions = null)
    {
        audit ??= CreateAuditLog();
        model ??= new GameDirectorOfflineModel();
        return new BrickRegistry([
            new BalanceAnalysisBrick(audit, NullLogger<BalanceAnalysisBrick>.Instance, model),
            new MapFlowBrick(audit, NullLogger<MapFlowBrick>.Instance, model),
            new ContentGenerationBrick(audit, NullLogger<ContentGenerationBrick>.Instance, model, sessions)
        ]);
    }

    public static McpToolRegistry CreateMcpRegistry(IBrickRegistry bricks, IDataDecisionAuditLog audit)
    {
        var executor = new McpBrickExecutor(bricks);
        return new McpToolRegistry(
            new AnalyzeBalanceTool(executor),
            new ValidateMapTool(executor),
            new GenerateContentTool(executor),
            new GetAuditTrailTool(audit),
            new QueryPatternsTool(new StubAshlarClient()));
    }

    public static BrickInput BalanceInput(string sheetId, params (string id, Dictionary<string, double> stats)[] rows)
    {
        var data = rows.Select(r => new { id = r.id, stats = r.stats }).ToArray();
        var input = new BrickInput();
        input.Set("sheet_id", sheetId);
        input.Set("data", System.Text.Json.JsonSerializer.SerializeToElement(data));
        return input;
    }

    public static BrickInput MapInput(string mapId, int chokeTiles = 2, int openTiles = 2, int spawnCount = 4)
    {
        var tiles = new List<object[]>
        {
            Enumerable.Range(0, chokeTiles).Select(i => new { x = i, y = 0, traversalOptions = 1 }).ToArray(),
            Enumerable.Range(0, openTiles).Select(i => new { x = i, y = 1, traversalOptions = 4 }).ToArray()
        };
        var spawns = Enumerable.Range(0, spawnCount).Select(i => new { id = $"s{i}", x = i * 10, y = i * 5 }).ToArray();
        var config = new { tiles, spawns, metadata = new { mode = "test" } };
        var input = new BrickInput();
        input.Set("map_id", mapId);
        input.Set("config", System.Text.Json.JsonSerializer.SerializeToElement(config));
        return input;
    }

    /// <summary>Client for stub ashlar interactions.</summary>
    internal sealed class StubAshlarClient : IAshlarClient
    {
        /// <summary>Run agent async.</summary>
        /// <param name="request">Request.</param>
        /// <param name="default">Default.</param>
        public Task<AgentResponse> RunAgentAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
            /// <summary>Not implemented exception.</summary>
            throw new NotImplementedException();
        /// <summary>Run validation async.</summary>
        /// <param name="null">Null.</param>
        /// <param name="default">Default.</param>
        public Task<ValidationResponse> RunValidationAsync(ValidationRequest? request = null, CancellationToken cancellationToken = default) =>
            /// <summary>Not implemented exception.</summary>
            throw new NotImplementedException();
        /// <summary>Orchestrate async.</summary>
        /// <param name="request">Request.</param>
        /// <param name="default">Default.</param>
        public Task<OrchestrationResponse> OrchestrateAsync(OrchestrationRequest request, CancellationToken cancellationToken = default) =>
            /// <summary>Not implemented exception.</summary>
            throw new NotImplementedException();
        /// <summary>Gets status async.</summary>
        /// <param name="default">Default.</param>
        public Task<StatusResponse> GetStatusAsync(CancellationToken cancellationToken = default) =>
            /// <summary>Not implemented exception.</summary>
            throw new NotImplementedException();
        /// <summary>Creates image async.</summary>
        /// <param name="request">Request.</param>
        /// <param name="default">Default.</param>
        public Task<ExecutionBuildResponse> BuildImageAsync(ExecutionBuildRequest request, CancellationToken cancellationToken = default) =>
            /// <summary>Not implemented exception.</summary>
            throw new NotImplementedException();
        /// <summary>Run container async.</summary>
        /// <param name="request">Request.</param>
        /// <param name="default">Default.</param>
        public Task<ExecutionRunResponse> RunContainerAsync(ExecutionRunRequest request, CancellationToken cancellationToken = default) =>
            /// <summary>Not implemented exception.</summary>
            throw new NotImplementedException();
        /// <summary>Query knowledge async.</summary>
        /// <param name="relativeQuery">Relative query.</param>
        /// <param name="default">Default.</param>
        public Task<System.Text.Json.JsonElement> QueryKnowledgeAsync(string relativeQuery, CancellationToken cancellationToken = default) =>
            Task.FromResult(System.Text.Json.JsonSerializer.SerializeToElement(new { entries = Array.Empty<object>(), total = 0 }));
        /// <summary>Invoke async.</summary>
        /// <param name="method">Method.</param>
        /// <param name="relativeUri">Relative uri.</param>
        /// <param name="null">Null.</param>
        /// <param name="default">Default.</param>
        public Task<HttpResponseMessage> InvokeAsync(HttpMethod method, string relativeUri, HttpContent? content = null, CancellationToken cancellationToken = default) =>
            /// <summary>Not implemented exception.</summary>
            throw new NotImplementedException();
    }

    /// <summary>Capturing model.</summary>
    internal sealed class CapturingModel : IModel
    {
        /// <summary>Prompts.</summary>
        public List<string> Prompts { get; } = [];
        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
        {
            Prompts.Add(string.Join("|", input.Messages.Select(m => m.content)));
            return Task.FromResult(new ModelOutput("captured-model-output"));
        }
    }
}
