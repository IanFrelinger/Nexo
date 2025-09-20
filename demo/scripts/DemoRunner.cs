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
            await RunSpecificDemo(args[0]);
        }
        else
        {
            await RunFullShowcase();
        }
    }

    static void SetupCommands()
    {
        _composer
            .AddCommand("validate", async () => await RunValidation(), "Run environment validation")
            .AddCommand("demo-feature", async () => await RunSampleFeature(), "Run sample feature")
            .AddCommand("showcase-web", async () => await ShowcaseFrontend(FrontendType.Web), "Showcase web app generation")
            .AddCommand("showcase-mobile", async () => await ShowcaseFrontend(FrontendType.Mobile), "Showcase mobile app generation")
            .AddCommand("showcase-desktop", async () => await ShowcaseFrontend(FrontendType.Desktop), "Showcase desktop app generation")
            .AddCommand("showcase-console", async () => await ShowcaseFrontend(FrontendType.Console), "Showcase console app generation")
            .AddCommand("showcase-game", async () => await ShowcaseFrontend(FrontendType.Game), "Showcase game app generation")
            .AddCommand("showcase-all-frontends", async () => await ShowcaseAllFrontends(), "Showcase all frontend types")
            .AddCommand("interactive-demo", async () => await RunInteractiveDemo(), "Run interactive console demo")
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
}
