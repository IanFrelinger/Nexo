using System;
using System.Threading.Tasks;
using Playground.Console.Services;
using Playground.Console.Commands;

namespace Playground.Console;

class Program
{
    private static readonly FeatureService _featureService = new();
    private static readonly FrontendGeneratorService _frontendGenerator = new();
    private static readonly CommandComposer _commandComposer = new();

    static async Task Main(string[] args)
    {
        global::System.Console.Clear();
        
        // Setup command composition
        SetupCommands();
        
        // Check if we have command line arguments for non-interactive mode
        if (args.Length > 0)
        {
            await RunNonInteractiveMode(args);
            return;
        }
        
        // Check if input is redirected (non-interactive)
        if (global::System.Console.IsInputRedirected || global::System.Console.IsOutputRedirected)
        {
            await RunNonInteractiveMode(Array.Empty<string>());
            return;
        }
        
        // Run in interactive mode
        await RunInteractiveMode();
    }

    static void SetupCommands()
    {
        _commandComposer
            .AddCommand("validate", async () => await RunValidationPass(), "Run environment validation checks")
            .AddCommand("demo-feature", async () => await RunSampleFeature(), "Run a sample feature demonstration")
            .AddCommand("generate-web", async () => await GenerateFrontend(FrontendType.Web), "Generate a web application")
            .AddCommand("generate-mobile", async () => await GenerateFrontend(FrontendType.Mobile), "Generate a mobile application")
            .AddCommand("generate-desktop", async () => await GenerateFrontend(FrontendType.Desktop), "Generate a desktop application")
            .AddCommand("generate-console", async () => await GenerateFrontend(FrontendType.Console), "Generate a console application")
            .AddCommand("generate-game", async () => await GenerateFrontend(FrontendType.Game), "Generate a game application")
            .AddCommand("showcase-all", async () => await ShowcaseAllFrontends(), "Showcase all frontend types")
            .SetContext("featureService", _featureService)
            .SetContext("frontendGenerator", _frontendGenerator);
    }

    static async Task RunInteractiveMode()
    {
        global::System.Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        global::System.Console.WriteLine("║                 NEXO FEATURE LAB (Console)            ║");
        global::System.Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Welcome to the Nexo Feature Lab!");
        global::System.Console.WriteLine("This interactive demo showcases composable AI features.");
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Press any key to continue...");
        global::System.Console.ReadKey();
        
        bool running = true;
        while (running)
        {
            await ShowMainMenu();
            global::System.Console.WriteLine();
            global::System.Console.WriteLine("Press any key to continue...");
            global::System.Console.ReadKey();
        }
    }

    static async Task RunNonInteractiveMode(string[] args)
    {
        global::System.Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        global::System.Console.WriteLine("║                 NEXO FEATURE LAB (Console)            ║");
        global::System.Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Welcome to the Nexo Feature Lab!");
        global::System.Console.WriteLine("This interactive demo showcases composable AI features.");
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("[Non-interactive mode detected. Running automated demo...]");
        global::System.Console.WriteLine();

        // Execute commands based on arguments or run default showcase
        if (args.Length > 0)
        {
            foreach (var arg in args)
            {
                try
                {
                    await _commandComposer.ExecuteAsync(arg);
                }
                catch (ArgumentException ex)
                {
                    global::System.Console.WriteLine($"❌ {ex.Message}");
                }
            }
        }
        else
        {
            // Run default showcase
            await _commandComposer.ExecuteAsync("validate");
            await _commandComposer.ExecuteAsync("demo-feature");
            await _commandComposer.ExecuteAsync("showcase-all");
        }
        
        global::System.Console.WriteLine("\n[Demo completed. Exiting...]");
    }

    static async Task ShowMainMenu()
    {
        global::System.Console.Clear();
        global::System.Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        global::System.Console.WriteLine("║                 NEXO FEATURE LAB (Console)            ║");
        global::System.Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Choose an option:");
        global::System.Console.WriteLine("-------------------------------------------------------");
        global::System.Console.WriteLine("1. Build Features (View available templates)");
        global::System.Console.WriteLine("2. Run Features (Simulate execution)");
        global::System.Console.WriteLine("3. Inspect Runs (View audit logs and approvals)");
        global::System.Console.WriteLine("4. Frontend Generator (AI-native application generation)");
        global::System.Console.WriteLine("5. Run Validation Pass");
        global::System.Console.WriteLine("6. Show Available Commands");
        global::System.Console.WriteLine("7. Exit");
        global::System.Console.WriteLine();
        global::System.Console.Write("Enter your choice: ");

        var choice = global::System.Console.ReadLine();
        await ProcessMainMenuChoice(choice);
    }

    static async Task ProcessMainMenuChoice(string? choice)
    {
        switch (choice?.Trim())
        {
            case "1":
                await ShowBuildMenu();
                break;
            case "2":
                await ShowRunMenu();
                break;
            case "3":
                await ShowInspectMenu();
                break;
            case "4":
                await ShowFrontendGeneratorMenu();
                break;
            case "5":
                await _commandComposer.ExecuteAsync("validate");
                break;
            case "6":
                _commandComposer.ListCommands();
                break;
            case "7":
                global::System.Console.WriteLine("Exiting Feature Lab. Goodbye!");
                Environment.Exit(0);
                break;
            default:
                global::System.Console.WriteLine("Invalid choice. Please try again.");
                break;
        }
    }

    static async Task ShowBuildMenu()
    {
        global::System.Console.Clear();
        global::System.Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        global::System.Console.WriteLine("║                 BUILD FEATURES                       ║");
        global::System.Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        global::System.Console.WriteLine();
        
        var features = await _featureService.GetAvailableFeaturesAsync();
        global::System.Console.WriteLine("Available Feature Templates:");
        global::System.Console.WriteLine("-------------------------------------------------------");
        
        for (int i = 0; i < features.Count; i++)
        {
            var feature = features[i];
            global::System.Console.WriteLine($"{i + 1}. {feature.Name}");
            global::System.Console.WriteLine($"   Description: {feature.Description}");
            global::System.Console.WriteLine($"   Capabilities: {string.Join(", ", feature.Capabilities)}");
            global::System.Console.WriteLine();
        }
    }

    static async Task ShowRunMenu()
    {
        global::System.Console.Clear();
        global::System.Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        global::System.Console.WriteLine("║                 RUN FEATURES                         ║");
        global::System.Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        global::System.Console.WriteLine();
        
        global::System.Console.WriteLine("Available AI Modes:");
        global::System.Console.WriteLine("1. Off (No AI, deterministic)");
        global::System.Console.WriteLine("2. Hybrid (AI-assisted)");
        global::System.Console.WriteLine("3. Embedded (Full AI)");
        global::System.Console.WriteLine();
        global::System.Console.Write("Select mode (1-3): ");
        
        var modeChoice = global::System.Console.ReadLine();
        var mode = modeChoice switch
        {
            "1" => "Off",
            "2" => "Hybrid", 
            "3" => "Embedded",
            _ => "Off"
        };
        
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Available Providers:");
        global::System.Console.WriteLine("1. Local (Stub)");
        global::System.Console.WriteLine("2. CloudA");
        global::System.Console.WriteLine("3. CloudB");
        global::System.Console.WriteLine();
        global::System.Console.Write("Select provider (1-3): ");
        
        var providerChoice = global::System.Console.ReadLine();
        var provider = providerChoice switch
        {
            "1" => "Local",
            "2" => "CloudA",
            "3" => "CloudB", 
            _ => "Local"
        };
        
        global::System.Console.WriteLine();
        global::System.Console.WriteLine($"Running Smart Reply Panel in {mode} mode with {provider} provider...");
        
        var result = await _featureService.RunFeatureAsync("smart-reply", mode, provider);
        
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Run Results:");
        global::System.Console.WriteLine($"Feature: {result.FeatureName}");
        global::System.Console.WriteLine($"Status: {result.Status}");
        global::System.Console.WriteLine($"Steps: {result.Steps.Count}");
        global::System.Console.WriteLine($"Duration: {result.Metrics.GetValueOrDefault("duration_ms", 0)} ms");
    }

    static async Task ShowInspectMenu()
    {
        global::System.Console.Clear();
        global::System.Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        global::System.Console.WriteLine("║                 INSPECT RUNS                         ║");
        global::System.Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        global::System.Console.WriteLine();
        
        global::System.Console.WriteLine("Recent Runs:");
        global::System.Console.WriteLine("-------------------------------------------------------");
        global::System.Console.WriteLine("Run ID: 12345-abcde");
        global::System.Console.WriteLine("Feature: Smart Reply Panel");
        global::System.Console.WriteLine("Status: Completed");
        global::System.Console.WriteLine("Mode: Off");
        global::System.Console.WriteLine("Provider: Local");
        global::System.Console.WriteLine("Duration: 2001ms");
        global::System.Console.WriteLine();
        
        global::System.Console.WriteLine("Approvals Queue:");
        global::System.Console.WriteLine("-------------------------------------------------------");
        global::System.Console.WriteLine("No pending approvals");
    }

    static async Task ShowFrontendGeneratorMenu()
    {
        global::System.Console.Clear();
        global::System.Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        global::System.Console.WriteLine("║                 FRONTEND GENERATOR                   ║");
        global::System.Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("🏭 AI-native frontend application generation system");
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Available Frontend Types:");
        global::System.Console.WriteLine("1. Web Application (Progressive Web App)");
        global::System.Console.WriteLine("2. Mobile Application (Cross-Platform)");
        global::System.Console.WriteLine("3. Desktop Application (Cross-Platform)");
        global::System.Console.WriteLine("4. Console Application (Command-Line)");
        global::System.Console.WriteLine("5. Game Application (Unity/Unreal)");
        global::System.Console.WriteLine("6. Showcase All Types");
        global::System.Console.WriteLine();
        global::System.Console.Write("Select frontend type (1-6): ");
        
        var choice = global::System.Console.ReadLine();
        var frontendType = choice switch
        {
            "1" => FrontendType.Web,
            "2" => FrontendType.Mobile,
            "3" => FrontendType.Desktop,
            "4" => FrontendType.Console,
            "5" => FrontendType.Game,
            "6" => (FrontendType?)null,
            _ => FrontendType.Web
        };

        if (frontendType == null)
        {
            await ShowcaseAllFrontends();
        }
        else
        {
            await GenerateFrontend(frontendType.Value);
        }
    }

    static async Task GenerateFrontend(FrontendType frontendType)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine($"Generating {frontendType} application...");
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Enter your application description in natural language:");
        global::System.Console.Write("> ");
        
        string? description = global::System.Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(description))
        {
            description = $"A sample {frontendType.ToString().ToLower()} application";
        }

        global::System.Console.WriteLine();
        global::System.Console.WriteLine("🤖 AI agents are analyzing your requirements...");
        await Task.Delay(1500);
        
        var result = await _frontendGenerator.GenerateFrontendAsync(description, frontendType);
        
        global::System.Console.WriteLine();
        global::System.Console.WriteLine($"✅ {result.Message}");
        global::System.Console.WriteLine();
        
        if (result.AgentCoordination != null)
        {
            global::System.Console.WriteLine("🤖 AI Agent Coordination:");
            global::System.Console.WriteLine($"  {result.AgentCoordination.Message}");
            global::System.Console.WriteLine("  Agents involved:");
            foreach (var agent in result.AgentCoordination.AgentsInvolved)
            {
                global::System.Console.WriteLine($"    • {agent}");
            }
            global::System.Console.WriteLine();
        }
        
        if (result.ArchitectureDecision != null)
        {
            global::System.Console.WriteLine("🏛️ Architecture Decision:");
            global::System.Console.WriteLine($"  Type: {result.ArchitectureDecision.ArchitectureType}");
            global::System.Console.WriteLine($"  Confidence: {result.ArchitectureDecision.ConfidenceScore:P1}");
            global::System.Console.WriteLine("  Platform Optimizations:");
            foreach (var optimization in result.ArchitectureDecision.PlatformOptimizations)
            {
                global::System.Console.WriteLine($"    • {optimization}");
            }
            global::System.Console.WriteLine();
        }
        
        if (result.GeneratedCode != null)
        {
            global::System.Console.WriteLine("💻 Generated Code:");
            global::System.Console.WriteLine($"  Platform: {result.GeneratedCode.Platform}");
            global::System.Console.WriteLine($"  Language: {result.GeneratedCode.Language}");
            global::System.Console.WriteLine($"  Framework: {result.GeneratedCode.Framework}");
            global::System.Console.WriteLine("  Files Generated:");
            foreach (var file in result.GeneratedCode.Files)
            {
                global::System.Console.WriteLine($"    • {file.FileName}");
            }
            global::System.Console.WriteLine();
        }
        
        if (result.GeneratedTests != null)
        {
            global::System.Console.WriteLine("🧪 Generated Tests:");
            global::System.Console.WriteLine($"  Unit Tests: {string.Join(", ", result.GeneratedTests.UnitTests)}");
            global::System.Console.WriteLine($"  Integration Tests: {string.Join(", ", result.GeneratedTests.IntegrationTests)}");
        }
    }

    static async Task ShowcaseAllFrontends()
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("🎭 Showcasing All Frontend Types");
        global::System.Console.WriteLine("=================================");
        global::System.Console.WriteLine();
        
        var frontendTypes = new[] { FrontendType.Web, FrontendType.Mobile, FrontendType.Desktop, FrontendType.Console, FrontendType.Game };
        
        foreach (var frontendType in frontendTypes)
        {
            global::System.Console.WriteLine($"🏗️  Generating {frontendType} Application...");
            await GenerateFrontend(frontendType);
            global::System.Console.WriteLine();
            await Task.Delay(1000);
        }
        
        global::System.Console.WriteLine("🎉 All frontend types showcased successfully!");
    }

    static async Task RunValidationPass()
    {
        global::System.Console.Clear();
        global::System.Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        global::System.Console.WriteLine("║                 RUN VALIDATION PASS                   ║");
        global::System.Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("Simulating validation checks...");
        global::System.Console.WriteLine();
        
        var validationResult = await _featureService.ValidateEnvironmentAsync();
        
        global::System.Console.WriteLine("Validation Results:");
        global::System.Console.WriteLine("-------------------------------------------------------");
        foreach (var check in validationResult.Checks)
        {
            var status = check.Passed ? "✅" : "❌";
            global::System.Console.WriteLine($"{status} {check.Name}: {check.Message}");
        }
        
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("===========================================");
        if (validationResult.IsValid)
        {
            global::System.Console.WriteLine("✅ VALIDATION PASS: READY TO DEMO");
        }
        else
        {
            global::System.Console.WriteLine("❌ VALIDATION FAILED: NOT READY");
        }
        global::System.Console.WriteLine("===========================================");
    }

    static async Task RunSampleFeature()
    {
        global::System.Console.WriteLine("Running sample feature...");
        var runResult = await _featureService.RunFeatureAsync("smart-reply", "Off", "Local");
        global::System.Console.WriteLine($"Sample Run Result:");
        global::System.Console.WriteLine($"Feature: {runResult.FeatureName}");
        global::System.Console.WriteLine($"Status: {runResult.Status}");
        global::System.Console.WriteLine($"Steps: {runResult.Steps.Count}");
        global::System.Console.WriteLine($"Duration: {runResult.Metrics.GetValueOrDefault("duration_ms", 0)} ms");
    }
}