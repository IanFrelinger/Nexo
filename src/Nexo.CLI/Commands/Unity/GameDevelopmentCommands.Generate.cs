using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.AI.Agents;
using Nexo.Feature.Unity.Workflows;
using Nexo.Feature.AI.Models;

namespace Nexo.CLI.Commands.Unity
{
    /// <summary>
    /// Code generation functionality
    /// </summary>
    public static partial class GameDevelopmentCommands
    {
        /// <summary>
        /// Creates the generate command
        /// </summary>
        private static Command CreateGenerateCommand(IServiceProvider serviceProvider)
        {
            var generateCommand = new Command("generate", "Generate game features using AI");
            
            var descriptionOption = new Option<string>(
                "--description",
                "Description of the game feature to generate");
            
            var typeOption = new Option<string>(
                "--type",
                () => "mechanic",
                "Type of feature to generate (mechanic, system, ui, etc.)");
            
            var projectPathOption = new Option<string>(
                "--project-path",
                () => ".",
                "Path to the Unity project directory");
            
            generateCommand.AddOption(descriptionOption);
            generateCommand.AddOption(typeOption);
            generateCommand.AddOption(projectPathOption);
            
            generateCommand.SetHandler(async (description, type, projectPath) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<GameDevelopmentCommands>>();
                var mechanicsAgent = serviceProvider.GetRequiredService<GameMechanicsGenerationAgent>();
                
                await GenerateGameFeature(mechanicsAgent, logger, description, type, projectPath);
            }, descriptionOption, typeOption, projectPathOption);
            
            return generateCommand;
        }

        /// <summary>
        /// Generates game features using AI
        /// </summary>
        private static async Task GenerateGameFeature(
            GameMechanicsGenerationAgent mechanicsAgent,
            ILogger logger,
            string description,
            string type,
            string projectPath)
        {
            try
            {
                logger.LogInformation("Generating game feature: {Type} - {Description}", type, description);
                
                Console.WriteLine($"Game Generating {type}: {description}");
                
                var request = new AgentRequest
                {
                    Input = description,
                    Context = new AgentContext()
                        .SetProjectPath(projectPath)
                        .SetGameDevelopmentMode(true)
                };
                
                var response = await mechanicsAgent.ProcessAsync(request);
                
                if (response.HasResult)
                {
                    Console.WriteLine($"SUCCESS: Generated {type} successfully!");
                    Console.WriteLine($"File Generated Code:");
                    Console.WriteLine(response.Result);
                    
                    // Show metadata
                    if (response.Metadata.ContainsKey("UnityComponents"))
                    {
                        var components = response.Metadata["UnityComponents"] as IEnumerable<string>;
                        Console.WriteLine($"\n🧩 Unity Components Created:");
                        foreach (var component in components ?? [])
                        {
                            Console.WriteLine($"  • {component}");
                        }
                    }
                    
                    if (response.Metadata.ContainsKey("PerformanceOptimizations"))
                    {
                        var optimizations = response.Metadata["PerformanceOptimizations"] as IEnumerable<string>;
                        Console.WriteLine($"\nRunning Performance Optimizations:");
                        foreach (var optimization in optimizations ?? [])
                        {
                            Console.WriteLine($"  • {optimization}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: Failed to generate {type}");
                }
                
                logger.LogInformation("Game feature generation completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate game feature");
                Console.WriteLine($"ERROR: Generation failed: {ex.Message}");
            }
        }
    }
}
