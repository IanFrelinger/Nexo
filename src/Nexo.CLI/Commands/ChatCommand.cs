using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Core.Domain.Models.CodeQuality;
using Nexo.Infrastructure.Orchestration;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Interactive chat command for tool generation and management
    /// </summary>
    public class ChatCommand : ICommand
    {
        private readonly ToolGenerationOrchestrator _orchestrator;
        private readonly IToolEvolver _evolver;
        private readonly IToolRepository _toolRepo;
        private readonly ILogger<ChatCommand> _logger;

        public ChatCommand(
            ToolGenerationOrchestrator orchestrator,
            IToolEvolver evolver,
            IToolRepository toolRepo,
            ILogger<ChatCommand> logger)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _evolver = evolver ?? throw new ArgumentNullException(nameof(evolver));
            _toolRepo = toolRepo ?? throw new ArgumentNullException(nameof(toolRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("🤖 Nexo Interactive Mode");
            Console.WriteLine("Describe what you need, or 'exit' to quit");
            Console.WriteLine("Commands: 'list' to see available tools, 'help' for more info");
            Console.WriteLine();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.Write("\n> ");
                    var input = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(input))
                        continue;

                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowHelp();
                        continue;
                    }

                    if (input.Equals("list", StringComparison.OrdinalIgnoreCase))
                    {
                        await ShowAvailableToolsAsync(cancellationToken);
                        continue;
                    }

                    // Detect intent and handle accordingly
                    if (IsEvolutionRequest(input))
                    {
                        await HandleEvolutionAsync(input, cancellationToken);
                    }
                    else if (IsListRequest(input))
                    {
                        await ShowAvailableToolsAsync(cancellationToken);
                    }
                    else
                    {
                        await HandleGenerationAsync(input, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in chat command");
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }
            }

            Console.WriteLine("\n👋 Goodbye!");
        }

        /// <summary>
        /// Handles tool generation requests
        /// </summary>
        private async Task HandleGenerationAsync(string description, CancellationToken cancellationToken)
        {
            Console.WriteLine("🔨 Generating tool...");

            try
            {
                var tool = await _orchestrator.GenerateToolAsync(description, cancellationToken);
                
                Console.WriteLine($"✅ Created: {tool.Name}");
                Console.WriteLine($"   Description: {tool.Description}");
                Console.WriteLine($"   Usage: nexo {tool.CommandName} [args]");
                
                if (tool.QualityScore != null)
                {
                    var quality = tool.QualityScore;
                    var qualityIcon = quality.QualityLevel switch
                    {
                        QualityLevel.Excellent => "🌟",
                        QualityLevel.Good => "✅",
                        QualityLevel.Acceptable => "⚠️",
                        QualityLevel.Poor => "❌",
                        QualityLevel.Unacceptable => "🚫",
                        _ => "❓"
                    };
                    
                    Console.WriteLine($"   Quality: {qualityIcon} {quality.OverallScore:F1}/10 ({quality.QualityLevel})");
                    
                    if (quality.RequiresHumanReview)
                    {
                        Console.WriteLine($"   ⚠️  Requires human review");
                    }
                    
                    if (!quality.IsProductionReady)
                    {
                        Console.WriteLine($"   ⚠️  Not production ready");
                    }
                }
                
                Console.WriteLine($"   🔒 Safety validated and approved");
                Console.WriteLine($"   Ready to use!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Generation failed: {ex.Message}");
                _logger.LogError(ex, "Tool generation failed for: {Description}", description);
            }
        }

        /// <summary>
        /// Handles tool evolution requests
        /// </summary>
        private async Task HandleEvolutionAsync(string input, CancellationToken cancellationToken)
        {
            try
            {
                // Parse evolution request (e.g., "modify json-formatter to also validate JSON")
                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    Console.WriteLine("❌ Evolution format: 'modify <tool-name> to <description>'");
                    return;
                }

                var toolName = parts[1];
                var modification = string.Join(" ", parts.Skip(2));

                Console.WriteLine($"🔄 Evolving tool: {toolName}");

                var evolvedTool = await _evolver.EvolveToolAsync(toolName, modification, cancellationToken);

                if (evolvedTool.Success)
                {
                    Console.WriteLine($"✅ Evolved: {evolvedTool.Name} v{evolvedTool.Version}");
                    Console.WriteLine($"   Modification: {evolvedTool.EvolutionDescription}");
                    Console.WriteLine($"   Usage: nexo {toolName} [args]");
                }
                else
                {
                    Console.WriteLine($"❌ Evolution failed: {string.Join(", ", evolvedTool.Errors)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Evolution failed: {ex.Message}");
                _logger.LogError(ex, "Tool evolution failed for input: {Input}", input);
            }
        }

        /// <summary>
        /// Shows available tools
        /// </summary>
        private async Task ShowAvailableToolsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("📋 Available Tools:");
                Console.WriteLine();

                var tools = await _toolRepo.ListToolsAsync(cancellationToken);
                var toolList = tools.ToList();

                if (!toolList.Any())
                {
                    Console.WriteLine("   No tools available. Generate one by describing what you need!");
                    return;
                }

                foreach (var tool in toolList.OrderBy(t => t.Name))
                {
                    Console.WriteLine($"   {tool.Name} v{tool.Version}");
                    Console.WriteLine($"      {tool.Description}");
                    Console.WriteLine($"      Usage: nexo {tool.CommandName} [args]");
                    Console.WriteLine($"      Created: {tool.CreatedAt:yyyy-MM-dd HH:mm}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to list tools: {ex.Message}");
                _logger.LogError(ex, "Failed to list tools");
            }
        }

        /// <summary>
        /// Shows help information
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("📖 Nexo Help");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  list                    - Show all available tools");
            Console.WriteLine("  help                    - Show this help");
            Console.WriteLine("  exit                    - Exit interactive mode");
            Console.WriteLine();
            Console.WriteLine("Tool Generation:");
            Console.WriteLine("  Just describe what you need:");
            Console.WriteLine("  > Create a JSON formatter");
            Console.WriteLine("  > Build an API tester");
            Console.WriteLine("  > Make a CSV to JSON converter");
            Console.WriteLine();
            Console.WriteLine("Tool Evolution:");
            Console.WriteLine("  modify <tool-name> to <description>");
            Console.WriteLine("  > modify json-formatter to also validate JSON");
            Console.WriteLine("  > modify api-tester to save responses to files");
            Console.WriteLine();
        }

        /// <summary>
        /// Checks if input is an evolution request
        /// </summary>
        private static bool IsEvolutionRequest(string input)
        {
            return input.StartsWith("modify ", StringComparison.OrdinalIgnoreCase) ||
                   input.StartsWith("update ", StringComparison.OrdinalIgnoreCase) ||
                   input.StartsWith("change ", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if input is a list request
        /// </summary>
        private static bool IsListRequest(string input)
        {
            return input.Equals("list", StringComparison.OrdinalIgnoreCase) ||
                   input.Equals("show tools", StringComparison.OrdinalIgnoreCase) ||
                   input.Equals("tools", StringComparison.OrdinalIgnoreCase);
        }
    }
}
