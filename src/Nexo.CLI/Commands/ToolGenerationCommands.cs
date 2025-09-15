using System;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Infrastructure.Orchestration;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// CLI commands for tool generation and management
    /// </summary>
    public static class ToolGenerationCommands
    {
        /// <summary>
        /// Creates the tool generation command
        /// </summary>
        public static Command CreateToolGenerationCommand(IServiceProvider serviceProvider)
        {
            var toolCommand = new Command("tool", "Tool generation and management");

            // Generate command
            var generateCommand = new Command("generate", "Generate a new tool from description");
            var descriptionArgument = new Argument<string>("description", "Description of the tool to generate");
            generateCommand.AddArgument(descriptionArgument);
            
            generateCommand.SetHandler(async (string description) =>
            {
                await GenerateToolAsync(description, serviceProvider);
            }, descriptionArgument);

            // Chat command
            var chatCommand = new Command("chat", "Start interactive tool generation chat");
            chatCommand.SetHandler(async () =>
            {
                await StartChatAsync(serviceProvider);
            });

            // List command
            var listCommand = new Command("list", "List all available tools");
            listCommand.SetHandler(async () =>
            {
                await ListToolsAsync(serviceProvider);
            });

            // Evolve command
            var evolveCommand = new Command("evolve", "Evolve an existing tool");
            var toolNameArgument = new Argument<string>("tool-name", "Name of the tool to evolve");
            var modificationArgument = new Argument<string>("modification", "Description of the modification");
            evolveCommand.AddArgument(toolNameArgument);
            evolveCommand.AddArgument(modificationArgument);
            
            evolveCommand.SetHandler(async (string toolName, string modification) =>
            {
                await EvolveToolAsync(toolName, modification, serviceProvider);
            }, toolNameArgument, modificationArgument);

            // Guided generation command
            var guidedCommand = new Command("guided", "Start guided tool generation");
            guidedCommand.SetHandler(async () =>
            {
                await StartGuidedGenerationAsync(serviceProvider);
            });

            toolCommand.AddCommand(generateCommand);
            toolCommand.AddCommand(chatCommand);
            toolCommand.AddCommand(listCommand);
            toolCommand.AddCommand(evolveCommand);
            toolCommand.AddCommand(guidedCommand);

            return toolCommand;
        }

        /// <summary>
        /// Generates a tool from description
        /// </summary>
        private static async Task GenerateToolAsync(string description, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ToolGenerationCommands>>();
            var orchestrator = serviceProvider.GetRequiredService<ToolGenerationOrchestrator>();

            try
            {
                Console.WriteLine($"🔨 Generating tool: {description}");
                
                var tool = await orchestrator.GenerateToolAsync(description);
                
                Console.WriteLine($"SUCCESS: Created: {tool.Name}");
                Console.WriteLine($"   Description: {tool.Description}");
                Console.WriteLine($"   Usage: nexo tool {tool.CommandName} [args]");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tool generation failed");
                Console.WriteLine($"ERROR: Generation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts interactive chat mode
        /// </summary>
        private static async Task StartChatAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ToolGenerationCommands>>();
            var orchestrator = serviceProvider.GetRequiredService<ToolGenerationOrchestrator>();
            var evolver = serviceProvider.GetRequiredService<IToolEvolver>();
            var toolRepo = serviceProvider.GetRequiredService<IToolRepository>();

            var chatCommand = new ChatCommand(orchestrator, evolver, toolRepo, logger);
            await chatCommand.ExecuteAsync(Array.Empty<string>());
        }

        /// <summary>
        /// Lists all available tools
        /// </summary>
        private static async Task ListToolsAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ToolGenerationCommands>>();
            var toolRepo = serviceProvider.GetRequiredService<IToolRepository>();

            try
            {
                Console.WriteLine("List Available Tools:");
                Console.WriteLine();

                var tools = await toolRepo.ListToolsAsync();
                var toolList = tools.ToList();

                if (!toolList.Any())
                {
                    Console.WriteLine("   No tools available. Generate one with: nexo tool generate \"description\"");
                    return;
                }

                foreach (var tool in toolList.OrderBy(t => t.Name))
                {
                    Console.WriteLine($"   {tool.Name} v{tool.Version}");
                    Console.WriteLine($"      {tool.Description}");
                    Console.WriteLine($"      Usage: nexo tool {tool.CommandName} [args]");
                    Console.WriteLine($"      Created: {tool.CreatedAt:yyyy-MM-dd HH:mm}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to list tools");
                Console.WriteLine($"ERROR: Failed to list tools: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts guided tool generation
        /// </summary>
        private static async Task StartGuidedGenerationAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ToolGenerationCommands>>();
            var guidedService = serviceProvider.GetRequiredService<IGuidedGenerationService>();

            var guidedCommand = new GuidedGenerationCommand(guidedService, logger);
            await guidedCommand.ExecuteAsync(Array.Empty<string>());
        }

        /// <summary>
        /// Evolves an existing tool
        /// </summary>
        private static async Task EvolveToolAsync(string toolName, string modification, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ToolGenerationCommands>>();
            var evolver = serviceProvider.GetRequiredService<IToolEvolver>();

            try
            {
                Console.WriteLine($"Processing Evolving tool: {toolName}");
                
                var evolvedTool = await evolver.EvolveToolAsync(toolName, modification);
                
                if (evolvedTool.Success)
                {
                    Console.WriteLine($"SUCCESS: Evolved: {evolvedTool.Name} v{evolvedTool.Version}");
                    Console.WriteLine($"   Modification: {evolvedTool.EvolutionDescription}");
                }
                else
                {
                    Console.WriteLine($"ERROR: Evolution failed: {string.Join(", ", evolvedTool.Errors)}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tool evolution failed");
                Console.WriteLine($"ERROR: Evolution failed: {ex.Message}");
            }
        }
    }
}
