using System;
using System.Threading.Tasks;
using Playground.Console.Commands;
using Playground.Console.Services;

namespace DemoScripts;

class DemoRunner
{
    private static readonly CommandComposer _composer = new();
    private static readonly FeatureService _featureService = new();
    private static readonly FrontendGeneratorService _frontendGenerator = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 NEXO FEATURE LAB DEMO RUNNER");
        Console.WriteLine("===============================");
        Console.WriteLine();

        SetupCommands();

        if (args.Length > 0)
        {
            var mode = args[0].ToLowerInvariant();
            if (mode == "--advanced" || mode == "-a")
            {
                await RunAdvancedMode(args.Length > 1 ? args[1] : null);
            }
            else
            {
                await RunSpecificDemo(args[0]);
            }
        }
        else
        {
            await ShowModeSelection();
        }
    }

    static void SetupCommands()
    {
        _composer
            // Basic commands
            .AddCommand("validate", async () => await RunValidation(), "Run environment validation")
            .AddCommand("demo-feature", async () => await RunSampleFeature(), "Run sample feature")
            .AddCommand("showcase-web", async () => await ShowcaseFrontend(FrontendType.Web), "Showcase web app generation")
            .AddCommand("showcase-mobile", async () => await ShowcaseFrontend(FrontendType.Mobile), "Showcase mobile app generation")
            .AddCommand("showcase-desktop", async () => await ShowcaseFrontend(FrontendType.Desktop), "Showcase desktop app generation")
            .AddCommand("showcase-console", async () => await ShowcaseFrontend(FrontendType.Console), "Showcase console app generation")
            .AddCommand("showcase-game", async () => await ShowcaseFrontend(FrontendType.Game), "Showcase game app generation")
            .AddCommand("showcase-all-frontends", async () => await ShowcaseAllFrontends(), "Showcase all frontend types")
            .AddCommand("interactive-demo", async () => await RunInteractiveDemo(), "Run interactive console demo")
            // Advanced commands (consolidated from AdvancedDemoRunner)
            .AddCommand("enterprise-web", async () => await RunEnterpriseWebScenario(), "Enterprise web application scenario")
            .AddCommand("startup-mobile", async () => await RunStartupMobileScenario(), "Startup mobile app scenario")
            .AddCommand("gaming-studio", async () => await RunGamingStudioScenario(), "Gaming studio scenario")
            .AddCommand("dev-tools", async () => await RunDevToolsScenario(), "Developer tools scenario")
            .AddCommand("full-stack", async () => await RunFullStackScenario(), "Full-stack application scenario")
            .AddCommand("microservices", async () => await RunMicroservicesScenario(), "Microservices architecture scenario")
            .AddCommand("ai-powered", async () => await RunAIPoweredScenario(), "AI-powered application scenario")
            .SetContext("featureService", _featureService)
            .SetContext("frontendGenerator", _frontendGenerator);
    }

    static async Task RunSpecificDemo(string demoName)
    {
        try
        {
            await _composer.ExecuteAsync(demoName);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"❌ Demo '{demoName}' not found. Available demos:");
            _composer.ListCommands();
        }
    }

    static async Task RunFullShowcase()
    {
        Console.WriteLine("Running full showcase...");
        Console.WriteLine();

        // Run validation
        await _composer.ExecuteAsync("validate");
        Console.WriteLine();

        // Run sample feature
        await _composer.ExecuteAsync("demo-feature");
        Console.WriteLine();

        // Showcase all frontend types
        await _composer.ExecuteAsync("showcase-all-frontends");
        Console.WriteLine();

        Console.WriteLine("🎉 Full showcase completed successfully!");
    }

    static async Task RunValidation()
    {
        Console.WriteLine("🔍 NEXO FEATURE LAB VALIDATION PASS");
        Console.WriteLine("====================================");
        Console.WriteLine("Running validation checks...");
        Console.WriteLine();

        var validationResult = await _featureService.ValidateEnvironmentAsync();

        Console.WriteLine("Validation Results:");
        Console.WriteLine("-------------------------------------------------------");
        foreach (var check in validationResult.Checks)
        {
            var status = check.Passed ? "✅" : "❌";
            Console.WriteLine($"{status} {check.Name}: {check.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("===========================================");
        if (validationResult.IsValid)
        {
            Console.WriteLine("✅ VALIDATION PASS: READY TO DEMO");
        }
        else
        {
            Console.WriteLine("❌ VALIDATION FAILED: NOT READY");
        }
        Console.WriteLine("===========================================");
    }

    static async Task RunSampleFeature()
    {
        Console.WriteLine("🚀 Running Sample Feature");
        Console.WriteLine("==========================");
        Console.WriteLine();

        var runResult = await _featureService.RunFeatureAsync("smart-reply", "Off", "Local");
        
        Console.WriteLine("Sample Run Result:");
        Console.WriteLine($"Feature: {runResult.FeatureName}");
        Console.WriteLine($"Status: {runResult.Status}");
        Console.WriteLine($"Steps: {runResult.Steps.Count}");
        Console.WriteLine($"Duration: {runResult.Metrics.GetValueOrDefault("duration_ms", 0)} ms");
    }

    static async Task ShowcaseFrontend(FrontendType frontendType)
    {
        Console.WriteLine($"🏗️  {frontendType} Application Generation");
        Console.WriteLine("==========================================");
        Console.WriteLine();

        var description = GetSampleDescription(frontendType);
        Console.WriteLine($"Description: {description}");
        Console.WriteLine();

        Console.WriteLine("🤖 AI agents are analyzing your requirements...");
        await Task.Delay(1500);

        var result = await _frontendGenerator.GenerateFrontendAsync(description, frontendType);

        Console.WriteLine($"✅ {result.Message}");
        Console.WriteLine();

        if (result.AgentCoordination != null)
        {
            Console.WriteLine("🤖 AI Agent Coordination:");
            Console.WriteLine($"  {result.AgentCoordination.Message}");
            Console.WriteLine("  Agents involved:");
            foreach (var agent in result.AgentCoordination.AgentsInvolved)
            {
                Console.WriteLine($"    • {agent}");
            }
            Console.WriteLine();
        }

        if (result.ArchitectureDecision != null)
        {
            Console.WriteLine("🏛️ Architecture Decision:");
            Console.WriteLine($"  Type: {result.ArchitectureDecision.ArchitectureType}");
            Console.WriteLine($"  Confidence: {result.ArchitectureDecision.ConfidenceScore:P1}");
            Console.WriteLine("  Platform Optimizations:");
            foreach (var optimization in result.ArchitectureDecision.PlatformOptimizations)
            {
                Console.WriteLine($"    • {optimization}");
            }
            Console.WriteLine();
        }

        if (result.GeneratedCode != null)
        {
            Console.WriteLine("💻 Generated Code:");
            Console.WriteLine($"  Platform: {result.GeneratedCode.Platform}");
            Console.WriteLine($"  Language: {result.GeneratedCode.Language}");
            Console.WriteLine($"  Framework: {result.GeneratedCode.Framework}");
            Console.WriteLine("  Files Generated:");
            foreach (var file in result.GeneratedCode.Files)
            {
                Console.WriteLine($"    • {file.FileName}");
            }
            Console.WriteLine();
        }

        if (result.GeneratedTests != null)
        {
            Console.WriteLine("🧪 Generated Tests:");
            Console.WriteLine($"  Unit Tests: {string.Join(", ", result.GeneratedTests.UnitTests)}");
            Console.WriteLine($"  Integration Tests: {string.Join(", ", result.GeneratedTests.IntegrationTests)}");
        }
    }

    static async Task ShowcaseAllFrontends()
    {
        Console.WriteLine("🎭 Showcasing All Frontend Types");
        Console.WriteLine("=================================");
        Console.WriteLine();

        var frontendTypes = new[] { FrontendType.Web, FrontendType.Mobile, FrontendType.Desktop, FrontendType.Console, FrontendType.Game };

        foreach (var frontendType in frontendTypes)
        {
            await ShowcaseFrontend(frontendType);
            Console.WriteLine();
            await Task.Delay(1000);
        }

        Console.WriteLine("🎉 All frontend types showcased successfully!");
    }

    static async Task RunInteractiveDemo()
    {
        Console.WriteLine("🎮 Starting Interactive Console Demo");
        Console.WriteLine("====================================");
        Console.WriteLine();

        // This would start the interactive console application
        Console.WriteLine("To run the interactive demo, use:");
        Console.WriteLine("dotnet run --project demo/feature-lab/Playground.Console");
        Console.WriteLine();
        Console.WriteLine("Available commands in interactive mode:");
        Console.WriteLine("1. Build Features (View available templates)");
        Console.WriteLine("2. Run Features (Simulate execution)");
        Console.WriteLine("3. Inspect Runs (View audit logs and approvals)");
        Console.WriteLine("4. Frontend Generator (AI-native application generation)");
        Console.WriteLine("5. Run Validation Pass");
        Console.WriteLine("6. Show Available Commands");
        Console.WriteLine("7. Exit");
    }

    static string GetSampleDescription(FrontendType frontendType)
    {
        return frontendType switch
        {
            FrontendType.Web => "A modern e-commerce web application with user authentication, product catalog, shopping cart, and payment processing",
            FrontendType.Mobile => "A fitness tracking mobile app with workout logging, progress tracking, and social features",
            FrontendType.Desktop => "A project management desktop application with task tracking, team collaboration, and reporting features",
            FrontendType.Console => "A command-line tool for managing database migrations with rollback support and validation",
            FrontendType.Game => "A 2D platformer game with character movement, enemy AI, collectibles, and level progression",
            _ => "A sample application"
        };
    }

    // Advanced mode methods (consolidated from AdvancedDemoRunner)
    static async Task ShowModeSelection()
    {
        Console.WriteLine("Select demo mode:");
        Console.WriteLine("1. Basic Mode - Standard demos and showcases");
        Console.WriteLine("2. Advanced Mode - Enterprise scenarios and complex workflows");
        Console.WriteLine("3. Full Showcase - Run all basic demos");
        Console.WriteLine();
        Console.Write("Enter choice (1-3): ");
        
        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                await RunFullShowcase();
                break;
            case "2":
                await RunAdvancedMode(null);
                break;
            case "3":
                await RunFullShowcase();
                break;
            default:
                Console.WriteLine("Invalid choice. Running basic showcase...");
                await RunFullShowcase();
                break;
        }
    }

    static async Task RunAdvancedMode(string? scenarioName)
    {
        Console.WriteLine("🚀 NEXO ADVANCED FEATURE LAB DEMO");
        Console.WriteLine("=================================");
        Console.WriteLine();

        if (!string.IsNullOrEmpty(scenarioName))
        {
            await RunScenario(scenarioName);
        }
        else
        {
            await ShowAvailableScenarios();
        }
    }

    static async Task RunScenario(string scenarioName)
    {
        try
        {
            await _composer.ExecuteAsync(scenarioName);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"❌ Scenario '{scenarioName}' not found. Available scenarios:");
            ShowAvailableScenarios();
        }
    }

    static async Task ShowAvailableScenarios()
    {
        Console.WriteLine("Available Advanced Scenarios:");
        Console.WriteLine("=============================");
        Console.WriteLine();
        Console.WriteLine("1. enterprise-web    - Enterprise web application scenario");
        Console.WriteLine("2. startup-mobile    - Startup mobile app scenario");
        Console.WriteLine("3. gaming-studio     - Gaming studio scenario");
        Console.WriteLine("4. dev-tools         - Developer tools scenario");
        Console.WriteLine("5. full-stack        - Full-stack application scenario");
        Console.WriteLine("6. microservices     - Microservices architecture scenario");
        Console.WriteLine("7. ai-powered        - AI-powered application scenario");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- --advanced <scenario-name>");
        Console.WriteLine("Example: dotnet run -- --advanced enterprise-web");
        
        await Task.CompletedTask;
    }

    // Advanced scenario implementations (consolidated from AdvancedDemoRunner)
    static async Task RunEnterpriseWebScenario()
    {
        Console.WriteLine("🏢 Running Enterprise Web Application Scenario...");
        Console.WriteLine("Building enterprise-grade web application with:");
        Console.WriteLine("- Multi-tenant architecture");
        Console.WriteLine("- Advanced security features");
        Console.WriteLine("- Scalable microservices");
        Console.WriteLine("- Real-time collaboration");
        // TODO: Implement actual enterprise web scenario
        await Task.CompletedTask;
    }

    static async Task RunStartupMobileScenario()
    {
        Console.WriteLine("🚀 Running Startup Mobile App Scenario...");
        Console.WriteLine("Building startup mobile application with:");
        Console.WriteLine("- Cross-platform development");
        Console.WriteLine("- Rapid prototyping");
        Console.WriteLine("- MVP-focused features");
        Console.WriteLine("- User engagement analytics");
        // TODO: Implement actual startup mobile scenario
        await Task.CompletedTask;
    }

    static async Task RunGamingStudioScenario()
    {
        Console.WriteLine("🎮 Running Gaming Studio Scenario...");
        Console.WriteLine("Building gaming application with:");
        Console.WriteLine("- Real-time multiplayer");
        Console.WriteLine("- Advanced graphics pipeline");
        Console.WriteLine("- Game asset management");
        Console.WriteLine("- Performance optimization");
        // TODO: Implement actual gaming studio scenario
        await Task.CompletedTask;
    }

    static async Task RunDevToolsScenario()
    {
        Console.WriteLine("🛠️ Running Developer Tools Scenario...");
        Console.WriteLine("Building developer tools with:");
        Console.WriteLine("- Code analysis and refactoring");
        Console.WriteLine("- Automated testing frameworks");
        Console.WriteLine("- CI/CD pipeline integration");
        Console.WriteLine("- Performance profiling");
        // TODO: Implement actual dev tools scenario
        await Task.CompletedTask;
    }

    static async Task RunFullStackScenario()
    {
        Console.WriteLine("🌐 Running Full-Stack Application Scenario...");
        Console.WriteLine("Building full-stack application with:");
        Console.WriteLine("- Modern frontend framework");
        Console.WriteLine("- RESTful API backend");
        Console.WriteLine("- Database integration");
        Console.WriteLine("- Authentication & authorization");
        // TODO: Implement actual full-stack scenario
        await Task.CompletedTask;
    }

    static async Task RunMicroservicesScenario()
    {
        Console.WriteLine("🔧 Running Microservices Architecture Scenario...");
        Console.WriteLine("Building microservices with:");
        Console.WriteLine("- Service discovery");
        Console.WriteLine("- API gateway");
        Console.WriteLine("- Distributed logging");
        Console.WriteLine("- Container orchestration");
        // TODO: Implement actual microservices scenario
        await Task.CompletedTask;
    }

    static async Task RunAIPoweredScenario()
    {
        Console.WriteLine("🤖 Running AI-Powered Application Scenario...");
        Console.WriteLine("Building AI-powered application with:");
        Console.WriteLine("- Machine learning integration");
        Console.WriteLine("- Natural language processing");
        Console.WriteLine("- Computer vision capabilities");
        Console.WriteLine("- Intelligent automation");
        // TODO: Implement actual AI-powered scenario
        await Task.CompletedTask;
    }
}
