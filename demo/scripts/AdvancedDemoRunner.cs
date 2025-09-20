using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Playground.Console.Commands;
using Playground.Console.Services;

namespace DemoScripts;

class AdvancedDemoRunner
{
    private static readonly CommandComposer _composer = new();
    private static readonly FeatureService _featureService = new();
    private static readonly FrontendGeneratorService _frontendGenerator = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 NEXO ADVANCED FEATURE LAB DEMO");
        Console.WriteLine("=================================");
        Console.WriteLine();

        SetupCommands();

        if (args.Length > 0)
        {
            await RunScenario(args[0]);
        }
        else
        {
            await ShowAvailableScenarios();
        }
    }

    static void SetupCommands()
    {
        _composer
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

    static async Task RunScenario(string scenarioName)
    {
        try
        {
            await _composer.ExecuteAsync(scenarioName);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"❌ Scenario '{scenarioName}' not found.");
            await ShowAvailableScenarios();
        }
    }

    static async Task ShowAvailableScenarios()
    {
        Console.WriteLine("Available Demo Scenarios:");
        Console.WriteLine("=========================");
        Console.WriteLine();
        _composer.ListCommands();
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- <scenario-name>");
        Console.WriteLine("Example: dotnet run -- enterprise-web");
    }

    static async Task RunEnterpriseWebScenario()
    {
        Console.WriteLine("🏢 Enterprise Web Application Scenario");
        Console.WriteLine("======================================");
        Console.WriteLine();

        // Step 1: Validation
        Console.WriteLine("Step 1: Environment Validation");
        await RunValidation();
        Console.WriteLine();

        // Step 2: Generate enterprise web app
        Console.WriteLine("Step 2: Generating Enterprise Web Application");
        await GenerateFrontendWithDescription(
            FrontendType.Web,
            "A comprehensive enterprise web application with user management, role-based access control, audit logging, multi-tenancy, and advanced reporting capabilities"
        );
        Console.WriteLine();

        // Step 3: Generate mobile companion
        Console.WriteLine("Step 3: Generating Mobile Companion App");
        await GenerateFrontendWithDescription(
            FrontendType.Mobile,
            "A mobile companion app for the enterprise web application with offline capabilities, push notifications, and secure authentication"
        );
        Console.WriteLine();

        // Step 4: Generate admin console
        Console.WriteLine("Step 4: Generating Admin Console");
        await GenerateFrontendWithDescription(
            FrontendType.Desktop,
            "A desktop admin console for managing the enterprise application with real-time monitoring, user management, and system configuration"
        );
        Console.WriteLine();

        Console.WriteLine("✅ Enterprise scenario completed successfully!");
    }

    static async Task RunStartupMobileScenario()
    {
        Console.WriteLine("🚀 Startup Mobile App Scenario");
        Console.WriteLine("===============================");
        Console.WriteLine();

        // Step 1: Generate mobile MVP
        Console.WriteLine("Step 1: Generating Mobile MVP");
        await GenerateFrontendWithDescription(
            FrontendType.Mobile,
            "A mobile-first social networking app for connecting local communities with real-time messaging, event planning, and location-based features"
        );
        Console.WriteLine();

        // Step 2: Generate web dashboard
        Console.WriteLine("Step 2: Generating Web Dashboard");
        await GenerateFrontendWithDescription(
            FrontendType.Web,
            "A web dashboard for the mobile app with analytics, user management, content moderation, and business intelligence features"
        );
        Console.WriteLine();

        // Step 3: Generate admin tools
        Console.WriteLine("Step 3: Generating Admin Tools");
        await GenerateFrontendWithDescription(
            FrontendType.Console,
            "Command-line admin tools for managing the mobile app backend, user data, content moderation, and system maintenance"
        );
        Console.WriteLine();

        Console.WriteLine("✅ Startup scenario completed successfully!");
    }

    static async Task RunGamingStudioScenario()
    {
        Console.WriteLine("🎮 Gaming Studio Scenario");
        Console.WriteLine("==========================");
        Console.WriteLine();

        // Step 1: Generate game
        Console.WriteLine("Step 1: Generating Game Application");
        await GenerateFrontendWithDescription(
            FrontendType.Game,
            "A 3D multiplayer action RPG with character customization, quest system, real-time combat, and social features"
        );
        Console.WriteLine();

        // Step 2: Generate game launcher
        Console.WriteLine("Step 2: Generating Game Launcher");
        await GenerateFrontendWithDescription(
            FrontendType.Desktop,
            "A desktop game launcher with automatic updates, friend management, achievements, and game library management"
        );
        Console.WriteLine();

        // Step 3: Generate mobile companion
        Console.WriteLine("Step 3: Generating Mobile Companion");
        await GenerateFrontendWithDescription(
            FrontendType.Mobile,
            "A mobile companion app for the game with character management, guild features, marketplace, and social interactions"
        );
        Console.WriteLine();

        Console.WriteLine("✅ Gaming studio scenario completed successfully!");
    }

    static async Task RunDevToolsScenario()
    {
        Console.WriteLine("🛠️ Developer Tools Scenario");
        Console.WriteLine("============================");
        Console.WriteLine();

        // Step 1: Generate CLI tool
        Console.WriteLine("Step 1: Generating CLI Tool");
        await GenerateFrontendWithDescription(
            FrontendType.Console,
            "A comprehensive CLI tool for database management with migration support, backup/restore, query optimization, and monitoring"
        );
        Console.WriteLine();

        // Step 2: Generate web interface
        Console.WriteLine("Step 2: Generating Web Interface");
        await GenerateFrontendWithDescription(
            FrontendType.Web,
            "A web-based database management interface with visual query builder, performance monitoring, and user management"
        );
        Console.WriteLine();

        // Step 3: Generate desktop app
        Console.WriteLine("Step 3: Generating Desktop Application");
        await GenerateFrontendWithDescription(
            FrontendType.Desktop,
            "A desktop database management application with advanced features, offline capabilities, and system integration"
        );
        Console.WriteLine();

        Console.WriteLine("✅ Developer tools scenario completed successfully!");
    }

    static async Task RunFullStackScenario()
    {
        Console.WriteLine("🌐 Full-Stack Application Scenario");
        Console.WriteLine("===================================");
        Console.WriteLine();

        // Step 1: Generate web frontend
        Console.WriteLine("Step 1: Generating Web Frontend");
        await GenerateFrontendWithDescription(
            FrontendType.Web,
            "A modern full-stack web application with React frontend, Node.js backend, real-time features, and microservices architecture"
        );
        Console.WriteLine();

        // Step 2: Generate mobile app
        Console.WriteLine("Step 2: Generating Mobile App");
        await GenerateFrontendWithDescription(
            FrontendType.Mobile,
            "A cross-platform mobile app that shares the same backend as the web application with native performance and offline support"
        );
        Console.WriteLine();

        // Step 3: Generate admin panel
        Console.WriteLine("Step 3: Generating Admin Panel");
        await GenerateFrontendWithDescription(
            FrontendType.Desktop,
            "A desktop admin panel for managing the full-stack application with real-time monitoring, user management, and system configuration"
        );
        Console.WriteLine();

        Console.WriteLine("✅ Full-stack scenario completed successfully!");
    }

    static async Task RunMicroservicesScenario()
    {
        Console.WriteLine("🔧 Microservices Architecture Scenario");
        Console.WriteLine("======================================");
        Console.WriteLine();

        // Step 1: Generate API gateway
        Console.WriteLine("Step 1: Generating API Gateway");
        await GenerateFrontendWithDescription(
            FrontendType.Console,
            "A high-performance API gateway for microservices with load balancing, authentication, rate limiting, and monitoring"
        );
        Console.WriteLine();

        // Step 2: Generate web frontend
        Console.WriteLine("Step 2: Generating Web Frontend");
        await GenerateFrontendWithDescription(
            FrontendType.Web,
            "A microservices-based web application with service discovery, circuit breakers, and distributed tracing"
        );
        Console.WriteLine();

        // Step 3: Generate monitoring dashboard
        Console.WriteLine("Step 3: Generating Monitoring Dashboard");
        await GenerateFrontendWithDescription(
            FrontendType.Desktop,
            "A real-time monitoring dashboard for microservices with health checks, performance metrics, and alerting"
        );
        Console.WriteLine();

        Console.WriteLine("✅ Microservices scenario completed successfully!");
    }

    static async Task RunAIPoweredScenario()
    {
        Console.WriteLine("🤖 AI-Powered Application Scenario");
        Console.WriteLine("===================================");
        Console.WriteLine();

        // Step 1: Generate AI web app
        Console.WriteLine("Step 1: Generating AI-Powered Web Application");
        await GenerateFrontendWithDescription(
            FrontendType.Web,
            "An AI-powered web application with natural language processing, machine learning models, and intelligent recommendations"
        );
        Console.WriteLine();

        // Step 2: Generate mobile AI app
        Console.WriteLine("Step 2: Generating Mobile AI App");
        await GenerateFrontendWithDescription(
            FrontendType.Mobile,
            "A mobile AI application with computer vision, voice recognition, and on-device machine learning capabilities"
        );
        Console.WriteLine();

        // Step 3: Generate AI development tools
        Console.WriteLine("Step 3: Generating AI Development Tools");
        await GenerateFrontendWithDescription(
            FrontendType.Console,
            "Command-line tools for AI model training, data preprocessing, and model deployment with MLOps capabilities"
        );
        Console.WriteLine();

        Console.WriteLine("✅ AI-powered scenario completed successfully!");
    }

    static async Task GenerateFrontendWithDescription(FrontendType frontendType, string description)
    {
        Console.WriteLine($"Generating {frontendType} application...");
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
            Console.WriteLine();
        }

        if (result.ArchitectureDecision != null)
        {
            Console.WriteLine("🏛️ Architecture Decision:");
            Console.WriteLine($"  Type: {result.ArchitectureDecision.ArchitectureType}");
            Console.WriteLine($"  Confidence: {result.ArchitectureDecision.ConfidenceScore:P1}");
            Console.WriteLine();
        }

        if (result.GeneratedCode != null)
        {
            Console.WriteLine("💻 Generated Code:");
            Console.WriteLine($"  Platform: {result.GeneratedCode.Platform}");
            Console.WriteLine($"  Language: {result.GeneratedCode.Language}");
            Console.WriteLine($"  Framework: {result.GeneratedCode.Framework}");
            Console.WriteLine($"  Files Generated: {result.GeneratedCode.Files.Count}");
            Console.WriteLine();
        }
    }

    static async Task RunValidation()
    {
        Console.WriteLine("🔍 Running Environment Validation...");
        var validationResult = await _featureService.ValidateEnvironmentAsync();
        
        Console.WriteLine("Validation Results:");
        foreach (var check in validationResult.Checks)
        {
            var status = check.Passed ? "✅" : "❌";
            Console.WriteLine($"  {status} {check.Name}: {check.Message}");
        }
        
        if (validationResult.IsValid)
        {
            Console.WriteLine("✅ Validation passed - ready to proceed");
        }
        else
        {
            Console.WriteLine("❌ Validation failed - please check environment");
        }
    }
}
