using System;
using System.CommandLine;
using System.Threading.Tasks;
using DemoScripts;

namespace CentralDemoRunner
{
    /// <summary>
    /// Central demo runner that demonstrates command composition and execution
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("🎯 NEXO CENTRAL COMMAND AGGREGATOR");
            Console.WriteLine("===================================");
            Console.WriteLine();
            Console.WriteLine("This demonstrates how all commands in the project can be");
            Console.WriteLine("composed together and executed via a central aggregator.");
            Console.WriteLine();

            // Create the demo command aggregator
            var demoAggregator = new DemoCommandAggregator();
            var demoCommand = demoAggregator.CreateDemoCommand();

            // Add help command
            var helpCommand = new Command("help", "Show help information");
            helpCommand.SetHandler(() =>
            {
                ShowHelp();
            });
            demoCommand.AddCommand(helpCommand);

            // Add examples command
            var examplesCommand = new Command("examples", "Show usage examples");
            examplesCommand.SetHandler(() =>
            {
                ShowExamples();
            });
            demoCommand.AddCommand(examplesCommand);

            // Add orchestration examples
            var orchestrationExamplesCommand = new Command("orchestration-examples", "Command orchestration examples");
            orchestrationExamplesCommand.SetHandler(() =>
            {
                ShowOrchestrationExamples();
            });
            demoCommand.AddCommand(orchestrationExamplesCommand);

            // Execute the command
            return await demoCommand.InvokeAsync(args);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("📚 NEXO CENTRAL COMMAND AGGREGATOR HELP");
            Console.WriteLine("=======================================");
            Console.WriteLine();
            Console.WriteLine("This central aggregator composes all commands from the project");
            Console.WriteLine("and provides a unified entry point for execution.");
            Console.WriteLine();
            Console.WriteLine("🔹 COMMAND CATEGORIES:");
            Console.WriteLine();
            Console.WriteLine("  feature-lab    - Feature Lab playground commands");
            Console.WriteLine("  validation     - Validation and preflight checks");
            Console.WriteLine("  showcase       - Demo showcase commands");
            Console.WriteLine("  frontend       - Frontend generation commands");
            Console.WriteLine("  orchestrate    - Command orchestration");
            Console.WriteLine("  discover       - Command discovery and search");
            Console.WriteLine();
            Console.WriteLine("🔹 USAGE EXAMPLES:");
            Console.WriteLine();
            Console.WriteLine("  # Start Feature Lab");
            Console.WriteLine("  dotnet run -- feature-lab start --platform blazor");
            Console.WriteLine();
            Console.WriteLine("  # Run validation");
            Console.WriteLine("  dotnet run -- validation run");
            Console.WriteLine();
            Console.WriteLine("  # Showcase Feature Factory");
            Console.WriteLine("  dotnet run -- showcase factory --type web");
            Console.WriteLine();
            Console.WriteLine("  # Generate frontend app");
            Console.WriteLine("  dotnet run -- frontend generate 'E-commerce app' --type mobile");
            Console.WriteLine();
            Console.WriteLine("  # Run command sequence");
            Console.WriteLine("  dotnet run -- orchestrate sequence validation run showcase all");
            Console.WriteLine();
            Console.WriteLine("  # Discover commands");
            Console.WriteLine("  dotnet run -- discover list --category showcase");
            Console.WriteLine();
            Console.WriteLine("🔹 WORKFLOWS:");
            Console.WriteLine();
            Console.WriteLine("  # Run predefined workflow");
            Console.WriteLine("  dotnet run -- orchestrate workflow full-demo");
            Console.WriteLine();
            Console.WriteLine("  # See all examples");
            Console.WriteLine("  dotnet run -- examples");
            Console.WriteLine();
        }

        private static void ShowExamples()
        {
            Console.WriteLine("💡 USAGE EXAMPLES");
            Console.WriteLine("=================");
            Console.WriteLine();
            
            Console.WriteLine("🔹 BASIC COMMANDS:");
            Console.WriteLine();
            Console.WriteLine("  # Start Feature Lab on Blazor");
            Console.WriteLine("  dotnet run -- feature-lab start --platform blazor --port 5000");
            Console.WriteLine();
            Console.WriteLine("  # Start Feature Lab on MAUI");
            Console.WriteLine("  dotnet run -- feature-lab start --platform maui");
            Console.WriteLine();
            Console.WriteLine("  # Check Feature Lab status");
            Console.WriteLine("  dotnet run -- feature-lab status");
            Console.WriteLine();
            Console.WriteLine("  # Stop Feature Lab");
            Console.WriteLine("  dotnet run -- feature-lab stop");
            Console.WriteLine();
            
            Console.WriteLine("🔹 VALIDATION COMMANDS:");
            Console.WriteLine();
            Console.WriteLine("  # Run full validation");
            Console.WriteLine("  dotnet run -- validation run");
            Console.WriteLine();
            Console.WriteLine("  # Skip tests during validation");
            Console.WriteLine("  dotnet run -- validation run --skip-tests");
            Console.WriteLine();
            Console.WriteLine("  # Check environment only");
            Console.WriteLine("  dotnet run -- validation env");
            Console.WriteLine();
            Console.WriteLine("  # Check dependencies only");
            Console.WriteLine("  dotnet run -- validation deps");
            Console.WriteLine();
            
            Console.WriteLine("🔹 SHOWCASE COMMANDS:");
            Console.WriteLine();
            Console.WriteLine("  # Run complete showcase");
            Console.WriteLine("  dotnet run -- showcase all");
            Console.WriteLine();
            Console.WriteLine("  # Run interactive showcase");
            Console.WriteLine("  dotnet run -- showcase all --interactive");
            Console.WriteLine();
            Console.WriteLine("  # Feature Factory showcase (Web)");
            Console.WriteLine("  dotnet run -- showcase factory --type web");
            Console.WriteLine();
            Console.WriteLine("  # Feature Factory showcase (Mobile)");
            Console.WriteLine("  dotnet run -- showcase factory --type mobile");
            Console.WriteLine();
            Console.WriteLine("  # Smart Reply showcase");
            Console.WriteLine("  dotnet run -- showcase smart-reply");
            Console.WriteLine();
            Console.WriteLine("  # Contract Summary showcase");
            Console.WriteLine("  dotnet run -- showcase contract-summary");
            Console.WriteLine();
            
            Console.WriteLine("🔹 FRONTEND GENERATION:");
            Console.WriteLine();
            Console.WriteLine("  # Generate web application");
            Console.WriteLine("  dotnet run -- frontend generate 'E-commerce platform' --type web --output ./web-app");
            Console.WriteLine();
            Console.WriteLine("  # Generate mobile application");
            Console.WriteLine("  dotnet run -- frontend generate 'Banking app' --type mobile --output ./mobile-app");
            Console.WriteLine();
            Console.WriteLine("  # Generate desktop application");
            Console.WriteLine("  dotnet run -- frontend generate 'Productivity suite' --type desktop --output ./desktop-app");
            Console.WriteLine();
            Console.WriteLine("  # Generate console application");
            Console.WriteLine("  dotnet run -- frontend generate 'CLI tool' --type console --output ./cli-app");
            Console.WriteLine();
            Console.WriteLine("  # Generate game application");
            Console.WriteLine("  dotnet run -- frontend generate '2D platformer' --type game --output ./game-app");
            Console.WriteLine();
            Console.WriteLine("  # List available frontend types");
            Console.WriteLine("  dotnet run -- frontend list-types");
            Console.WriteLine();
        }

        private static void ShowOrchestrationExamples()
        {
            Console.WriteLine("🔄 COMMAND ORCHESTRATION EXAMPLES");
            Console.WriteLine("=================================");
            Console.WriteLine();
            
            Console.WriteLine("🔹 COMMAND SEQUENCES:");
            Console.WriteLine();
            Console.WriteLine("  # Run validation then showcase");
            Console.WriteLine("  dotnet run -- orchestrate sequence validation run showcase all");
            Console.WriteLine();
            Console.WriteLine("  # Start Feature Lab then run showcases");
            Console.WriteLine("  dotnet run -- orchestrate sequence feature-lab start showcase factory showcase smart-reply");
            Console.WriteLine();
            Console.WriteLine("  # Generate multiple frontend types");
            Console.WriteLine("  dotnet run -- orchestrate sequence frontend generate 'Web app' --type web frontend generate 'Mobile app' --type mobile");
            Console.WriteLine();
            
            Console.WriteLine("🔹 PREDEFINED WORKFLOWS:");
            Console.WriteLine();
            Console.WriteLine("  # Full demo workflow");
            Console.WriteLine("  dotnet run -- orchestrate workflow full-demo");
            Console.WriteLine();
            Console.WriteLine("  # Quick showcase workflow");
            Console.WriteLine("  dotnet run -- orchestrate workflow quick-showcase");
            Console.WriteLine();
            Console.WriteLine("  # Frontend generation workflow");
            Console.WriteLine("  dotnet run -- orchestrate workflow frontend-generation");
            Console.WriteLine();
            
            Console.WriteLine("🔹 COMMAND DISCOVERY:");
            Console.WriteLine();
            Console.WriteLine("  # List all commands");
            Console.WriteLine("  dotnet run -- discover list");
            Console.WriteLine();
            Console.WriteLine("  # List commands by category");
            Console.WriteLine("  dotnet run -- discover list --category showcase");
            Console.WriteLine();
            Console.WriteLine("  # Search for commands");
            Console.WriteLine("  dotnet run -- discover search feature");
            Console.WriteLine();
            Console.WriteLine("  # Search for validation commands");
            Console.WriteLine("  dotnet run -- discover search validation");
            Console.WriteLine();
            
            Console.WriteLine("🔹 ADVANCED ORCHESTRATION:");
            Console.WriteLine();
            Console.WriteLine("  # Complex multi-step workflow");
            Console.WriteLine("  dotnet run -- orchestrate sequence validation run feature-lab start --platform blazor showcase all frontend generate 'Demo app' --type web");
            Console.WriteLine();
            Console.WriteLine("  # Conditional execution (would be implemented)");
            Console.WriteLine("  # dotnet run -- orchestrate conditional validation run --if env-check showcase all --if validation-pass");
            Console.WriteLine();
            Console.WriteLine("  # Parallel execution (would be implemented)");
            Console.WriteLine("  # dotnet run -- orchestrate parallel showcase factory showcase smart-reply showcase contract-summary");
            Console.WriteLine();
        }
    }
}
