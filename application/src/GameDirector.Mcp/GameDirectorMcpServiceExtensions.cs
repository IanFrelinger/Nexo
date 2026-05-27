using GameDirector.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Domain.Execution;

namespace GameDirector.Mcp;

public static class GameDirectorMcpServiceExtensions
{
    public static IServiceCollection AddGameDirectorMcp(this IServiceCollection services)
    {
        services.AddSingleton<McpBrickExecutor>();
        services.AddSingleton<AnalyzeBalanceTool>();
        services.AddSingleton<ValidateMapTool>();
        services.AddSingleton<GenerateContentTool>();
        services.AddSingleton<GetAuditTrailTool>();
        services.AddSingleton<QueryPatternsTool>();
        services.AddSingleton<McpToolRegistry>();
        return services;
    }
}
