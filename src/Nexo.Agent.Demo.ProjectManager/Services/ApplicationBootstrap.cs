using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Implementations;
using Nexo.Agent.Tools.Builtin;
using Nexo.Observability;

namespace Nexo.Agent.Demo.ProjectManager.Services;

/// <summary>
/// Handles application bootstrap and service configuration.
/// </summary>
public partial class ApplicationBootstrap
{
    /// <summary>
    /// Creates and configures the host with all required services.
    /// </summary>
    public static IHost CreateHost(string[] args)
    {
        return CreateHostBuilder(args).Build();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add observability
                services.AddNexoObservability(context.Configuration);

                // Add Agent services
                services.AddSingleton<IAgentPlanner, SimplePlanner>();
                services.AddSingleton<IToolRegistry, ToolRegistry>();
                services.AddSingleton<IToolBroker, ToolBroker>();
                services.AddSingleton<IToolFactory, PipelineToolFactory>();
                services.AddSingleton<ITaskExecutionAgent, AtlasAgent>();

                // Add built-in tools
                services.AddSingleton<FileReadTool>();
                services.AddSingleton<CsvQueryTool>();
                services.AddSingleton<ReportWriteTool>();
                services.AddSingleton<SummarizeTool>();
            });
}
