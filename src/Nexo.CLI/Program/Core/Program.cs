using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Program.Commands;
using Nexo.CLI.Program.Configuration;
using Nexo.CLI.Program.Services;

namespace Nexo.CLI.Program.Core
{
    /// <summary>
    /// Main entry point for the Nexo CLI application.
    /// </summary>
    public partial class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Exit code.</returns>
        public static async Task<int> Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            // Build host
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddNexoCliServices(configuration);
                })
                .Build();

#if !EXCLUDE_AI
            // Configure AI providers
            host.Services.ConfigureAIProviders();
#endif

            // Create command orchestrator
            var commandOrchestrator = new CommandOrchestrator(host.Services);
            var rootCommand = commandOrchestrator.CreateRootCommand();

            return await rootCommand.InvokeAsync(args);
        }
    }
}
