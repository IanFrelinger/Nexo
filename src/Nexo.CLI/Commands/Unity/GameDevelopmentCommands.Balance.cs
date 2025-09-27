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
    /// Game balance functionality
    /// </summary>
    public static partial class GameDevelopmentCommands
    {
        /// <summary>
        /// Creates the balance command
        /// </summary>
        private static Command CreateBalanceCommand(IServiceProvider serviceProvider)
        {
            var balanceCommand = new Command("balance", "Analyze and optimize game balance");
            
            var projectPathOption = new Option<string>(
                "--project-path",
                () => ".",
                "Path to the Unity project directory");
            
            var detailedOption = new Option<bool>(
                "--detailed",
                "Show detailed balance analysis");
            
            balanceCommand.AddOption(projectPathOption);
            balanceCommand.AddOption(detailedOption);
            
            balanceCommand.SetHandler(async (projectPath, detailed) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<GameDevelopmentCommands>>();
                var balanceAgent = serviceProvider.GetRequiredService<GameplayBalanceAgent>();
                
                await AnalyzeGameBalance(balanceAgent, logger, projectPath, detailed);
            }, projectPathOption, detailedOption);
            
            return balanceCommand;
        }

        /// <summary>
        /// Analyzes game balance
        /// </summary>
        private static async Task AnalyzeGameBalance(
            GameplayBalanceAgent balanceAgent,
            ILogger logger,
            string projectPath,
            bool detailed)
        {
            try
            {
                logger.LogInformation("Analyzing game balance for project: {ProjectPath}", projectPath);
                
                Console.WriteLine($"Analyzing Analyzing game balance...");
                
                var request = new AgentRequest
                {
                    Input = "Analyze current game balance",
                    Context = new AgentContext()
                        .SetProjectPath(projectPath)
                        .SetAnalysisMode("balance")
                };
                
                var response = await balanceAgent.ProcessAsync(request);
                
                if (response.HasResult)
                {
                    var balanceScore = response.GetMetadata<double>("BalanceScore");
                    Console.WriteLine($"Stats Overall Balance Score: {balanceScore:F2}/10");
                    
                    var issues = response.GetMetadata<IEnumerable<string>>("BalanceIssues");
                    if (issues?.Any() == true)
                    {
                        Console.WriteLine($"\nWARNING: Balance Issues Found:");
                        foreach (var issue in issues)
                        {
                            Console.WriteLine($"  • {issue}");
                        }
                    }
                    
                    var recommendations = response.GetMetadata<IEnumerable<string>>("Recommendations");
                    if (recommendations?.Any() == true)
                    {
                        Console.WriteLine($"\nIdea Recommendations:");
                        foreach (var recommendation in recommendations)
                        {
                            Console.WriteLine($"  • {recommendation}");
                        }
                    }
                    
                    if (detailed)
                    {
                        await ShowDetailedBalanceAnalysis(response);
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: Failed to analyze game balance");
                }
                
                logger.LogInformation("Game balance analysis completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze game balance");
                Console.WriteLine($"ERROR: Balance analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows detailed balance analysis
        /// </summary>
        private static async Task ShowDetailedBalanceAnalysis(AgentResponse response)
        {
            Console.WriteLine($"\nList Detailed Balance Analysis:");
            
            // Show implementation guidance if available
            if (response.Metadata.ContainsKey("ImplementationGuidance"))
            {
                var guidance = response.Metadata["ImplementationGuidance"];
                Console.WriteLine($"\nTool Implementation Guidance:");
                Console.WriteLine($"  {guidance}");
            }
            
            // Show testing strategy if available
            if (response.Metadata.ContainsKey("TestingStrategy"))
            {
                var strategy = response.Metadata["TestingStrategy"];
                Console.WriteLine($"\nTesting Testing Strategy:");
                Console.WriteLine($"  {strategy}");
            }
        }
    }
}
