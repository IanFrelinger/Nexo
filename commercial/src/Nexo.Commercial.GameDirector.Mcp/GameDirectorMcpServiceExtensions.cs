using GameDirector.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Nexo.BackgroundAgents.Playtesting;
using Nexo.Core.Domain.Execution;

namespace GameDirector.Mcp;

/// <summary>Game director mcp service extensions.</summary>
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
        if (services.Any(descriptor =>
                descriptor.ServiceType == typeof(IPlaytestRunRunner)))
        {
            services.AddSingleton<RunBrPlaytestTool>();
            services.AddSingleton<GetBrPlaytestReportTool>();
        }
        services.AddSingleton<McpToolRegistry>();
        return services;
    }
}
