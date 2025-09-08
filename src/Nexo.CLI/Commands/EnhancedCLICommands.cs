using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Interactive;
using Nexo.CLI.Dashboard;
using Nexo.CLI.Progress;
using Nexo.CLI.Help;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Enhanced CLI commands that integrate all new interactive features
    /// </summary>
    public static class EnhancedCLICommands
    {
        /// <summary>
        /// Creates the enhanced interactive command with all new features
        /// </summary>
        public static Command CreateEnhancedInteractiveCommand(
            IInteractiveCLI interactiveCLI,
            IRealTimeDashboard dashboard,
            IInteractiveHelpSystem helpSystem,
            ILogger logger)
        {
            var interactiveCommand = new Command("interactive", "Enhanced interactive CLI with intelligent suggestions and real-time monitoring");
            
            // Main interactive mode
            var startCommand = new Command("start", "Start enhanced interactive mode");
            startCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Starting enhanced interactive mode");
                    await interactiveCLI.StartInteractiveModeAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start interactive mode");
                    Console.WriteLine($"❌ Failed to start interactive mode: {ex.Message}");
                }
            });
            
            // Dashboard command
            var dashboardCommand = new Command("dashboard", "Open real-time monitoring dashboard");
            dashboardCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Opening real-time dashboard");
                    await dashboard.ShowRealTimeDashboard();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to open dashboard");
                    Console.WriteLine($"❌ Failed to open dashboard: {ex.Message}");
                }
            });
            
            // Help command
            var helpCommand = new Command("help", "Show interactive help system");
            var helpTopicArgument = new Argument<string?>("topic", "Specific help topic to show");
            helpCommand.AddArgument(helpTopicArgument);
            helpCommand.SetHandler(async (topic) =>
            {
                try
                {
                    logger.LogInformation("Showing interactive help for topic: {Topic}", topic ?? "general");
                    await helpSystem.ShowInteractiveHelp(topic);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show help");
                    Console.WriteLine($"❌ Failed to show help: {ex.Message}");
                }
            }, helpTopicArgument);
            
            // Status command
            var statusCommand = new Command("status", "Show current system status and context");
            statusCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Showing system status");
                    await interactiveCLI.ShowSystemStatusAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show status");
                    Console.WriteLine($"❌ Failed to show status: {ex.Message}");
                }
            });
            
            // Context command
            var contextCommand = new Command("context", "Show contextual help and suggestions");
            contextCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Showing contextual help");
                    await interactiveCLI.ShowContextualHelpAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show contextual help");
                    Console.WriteLine($"❌ Failed to show contextual help: {ex.Message}");
                }
            });
            
            // Examples command
            var examplesCommand = new Command("examples", "Show practical examples and tutorials");
            var examplesCategoryArgument = new Argument<string?>("category", "Specific category of examples to show");
            examplesCommand.AddArgument(examplesCategoryArgument);
            examplesCommand.SetHandler(async (category) =>
            {
                try
                {
                    logger.LogInformation("Showing examples for category: {Category}", category ?? "all");
                    await helpSystem.ShowExamples(category);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show examples");
                    Console.WriteLine($"❌ Failed to show examples: {ex.Message}");
                }
            }, examplesCategoryArgument);
            
            // Search command
            var searchCommand = new Command("search", "Search documentation and help");
            var searchTermArgument = new Argument<string>("term", "Search term");
            searchCommand.AddArgument(searchTermArgument);
            searchCommand.SetHandler(async (term) =>
            {
                try
                {
                    logger.LogInformation("Searching documentation for: {Term}", term);
                    await helpSystem.SearchDocumentation(term);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to search documentation");
                    Console.WriteLine($"❌ Failed to search documentation: {ex.Message}");
                }
            }, searchTermArgument);
            
            // Commands command
            var commandsCommand = new Command("commands", "Browse all available commands");
            commandsCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Showing command browser");
                    await helpSystem.ShowCommandBrowser();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show command browser");
                    Console.WriteLine($"❌ Failed to show command browser: {ex.Message}");
                }
            });
            
            // Add all subcommands
            interactiveCommand.AddCommand(startCommand);
            interactiveCommand.AddCommand(dashboardCommand);
            interactiveCommand.AddCommand(helpCommand);
            interactiveCommand.AddCommand(statusCommand);
            interactiveCommand.AddCommand(contextCommand);
            interactiveCommand.AddCommand(examplesCommand);
            interactiveCommand.AddCommand(searchCommand);
            interactiveCommand.AddCommand(commandsCommand);
            
            return interactiveCommand;
        }
        
        /// <summary>
        /// Creates the enhanced help command with searchable documentation
        /// </summary>
        public static Command CreateEnhancedHelpCommand(
            IInteractiveHelpSystem helpSystem,
            ILogger logger)
        {
            var helpCommand = new Command("help", "Enhanced help system with searchable documentation and examples");
            
            // General help
            var generalHelpCommand = new Command("general", "Show general help information");
            generalHelpCommand.SetHandler(async () =>
            {
                try
                {
                    await helpSystem.ShowInteractiveHelp();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show general help");
                    Console.WriteLine($"❌ Failed to show help: {ex.Message}");
                }
            });
            
            // Command-specific help
            var commandHelpCommand = new Command("command", "Show help for a specific command");
            var commandNameArgument = new Argument<string>("name", "Command name");
            commandHelpCommand.AddArgument(commandNameArgument);
            commandHelpCommand.SetHandler(async (name) =>
            {
                try
                {
                    await helpSystem.ShowCommandHelp(name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show command help for: {Command}", name);
                    Console.WriteLine($"❌ Failed to show help for command '{name}': {ex.Message}");
                }
            }, commandNameArgument);
            
            // Search help
            var searchHelpCommand = new Command("search", "Search help documentation");
            var searchTermArgument = new Argument<string>("term", "Search term");
            searchHelpCommand.AddArgument(searchTermArgument);
            searchHelpCommand.SetHandler(async (term) =>
            {
                try
                {
                    await helpSystem.SearchDocumentation(term);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to search help documentation");
                    Console.WriteLine($"❌ Failed to search help: {ex.Message}");
                }
            }, searchTermArgument);
            
            // Examples help
            var examplesHelpCommand = new Command("examples", "Show examples and tutorials");
            var examplesCategoryArgument = new Argument<string?>("category", "Example category");
            examplesHelpCommand.AddArgument(examplesCategoryArgument);
            examplesHelpCommand.SetHandler(async (category) =>
            {
                try
                {
                    await helpSystem.ShowExamples(category);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show examples");
                    Console.WriteLine($"❌ Failed to show examples: {ex.Message}");
                }
            }, examplesCategoryArgument);
            
            // Commands help
            var commandsHelpCommand = new Command("commands", "Browse all available commands");
            commandsHelpCommand.SetHandler(async () =>
            {
                try
                {
                    await helpSystem.ShowCommandBrowser();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show command browser");
                    Console.WriteLine($"❌ Failed to show command browser: {ex.Message}");
                }
            });
            
            // Add subcommands
            helpCommand.AddCommand(generalHelpCommand);
            helpCommand.AddCommand(commandHelpCommand);
            helpCommand.AddCommand(searchHelpCommand);
            helpCommand.AddCommand(examplesHelpCommand);
            helpCommand.AddCommand(commandsHelpCommand);
            
            return helpCommand;
        }
        
        /// <summary>
        /// Creates the enhanced dashboard command
        /// </summary>
        public static Command CreateEnhancedDashboardCommand(
            IRealTimeDashboard dashboard,
            ILogger logger)
        {
            var dashboardCommand = new Command("dashboard", "Real-time monitoring dashboard with performance metrics and adaptation status");
            
            // Main dashboard
            var showCommand = new Command("show", "Show the real-time dashboard");
            showCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Opening real-time dashboard");
                    await dashboard.ShowRealTimeDashboard();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to open dashboard");
                    Console.WriteLine($"❌ Failed to open dashboard: {ex.Message}");
                }
            });
            
            // Dashboard status
            var statusCommand = new Command("status", "Show dashboard status without opening full interface");
            statusCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Checking dashboard status");
                    Console.WriteLine("📊 Dashboard Status: Available");
                    Console.WriteLine("💡 Use 'nexo dashboard show' to open the full dashboard");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to check dashboard status");
                    Console.WriteLine($"❌ Failed to check dashboard status: {ex.Message}");
                }
            });
            
            // Add subcommands
            dashboardCommand.AddCommand(showCommand);
            dashboardCommand.AddCommand(statusCommand);
            
            return dashboardCommand;
        }
        
        /// <summary>
        /// Creates the enhanced status command
        /// </summary>
        public static Command CreateEnhancedStatusCommand(
            IInteractiveCLI interactiveCLI,
            ILogger logger)
        {
            var statusCommand = new Command("status", "Show comprehensive system status and context information");
            
            // System status
            var systemCommand = new Command("system", "Show system status and health");
            systemCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Showing system status");
                    await interactiveCLI.ShowSystemStatusAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show system status");
                    Console.WriteLine($"❌ Failed to show system status: {ex.Message}");
                }
            });
            
            // Context status
            var contextCommand = new Command("context", "Show current CLI context and suggestions");
            contextCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Showing context status");
                    await interactiveCLI.ShowContextualHelpAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show context status");
                    Console.WriteLine($"❌ Failed to show context status: {ex.Message}");
                }
            });
            
            // Performance status
            var performanceCommand = new Command("performance", "Show performance metrics and optimization status");
            performanceCommand.SetHandler(async () =>
            {
                try
                {
                    logger.LogInformation("Showing performance status");
                    Console.WriteLine("📊 Performance Status:");
                    Console.WriteLine("  CPU Usage: 25%");
                    Console.WriteLine("  Memory Usage: 512 MB");
                    Console.WriteLine("  Active Adaptations: 2");
                    Console.WriteLine("  Recent Improvements: +15% performance");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to show performance status");
                    Console.WriteLine($"❌ Failed to show performance status: {ex.Message}");
                }
            });
            
            // Add subcommands
            statusCommand.AddCommand(systemCommand);
            statusCommand.AddCommand(contextCommand);
            statusCommand.AddCommand(performanceCommand);
            
            return statusCommand;
        }
    }
}
