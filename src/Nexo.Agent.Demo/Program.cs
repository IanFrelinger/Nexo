using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Implementations;
using Nexo.Agent.Tools.Builtin;
using Nexo.Observability;
using Spectre.Console;

namespace Nexo.Agent.Demo;

/// <summary>
/// TUI demo application for the Agent Foundry system.
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Create host
        var host = CreateHostBuilder(args).Build();

        // Get services
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var agent = host.Services.GetRequiredService<ITaskExecutionAgent>();

        try
        {
            logger.LogInformation("Starting Nexo Agent Foundry Demo");

            // Run the demo
            var demo = new AgentFoundryDemo(agent, logger);
            
            // Check for demo mode flag
            if (args.Length > 0 && args[0] == "--demo")
            {
                return await demo.RunDemoModeAsync();
            }
            else
            {
                return await demo.RunAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in demo application");
            return 1;
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add observability
                services.AddNexoObservability(context.Configuration);

                // Add agent services
                services.AddSingleton<IToolRegistry, ToolRegistry>();
                services.AddSingleton<IToolBroker, ToolBroker>();
                services.AddSingleton<IAgentPlanner, SimplePlanner>();
                services.AddSingleton<IToolFactory, PipelineToolFactory>();
                services.AddSingleton<ITaskExecutionAgent, AtlasAgent>();

                // Register built-in tools
                services.AddSingleton<ITool, FileReadTool>();
                services.AddSingleton<ITool, CsvQueryTool>();
                services.AddSingleton<ITool, ReportWriteTool>();
                services.AddSingleton<ITool, SummarizeTool>();
            });
}
// This class acts as an orchestrator for various demo application functionalities,
// with specific categories defined in partial classes.