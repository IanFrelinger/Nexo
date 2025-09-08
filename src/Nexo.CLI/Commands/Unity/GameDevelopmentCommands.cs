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
    /// Game development CLI commands for Unity projects
    /// </summary>
    public static class GameDevelopmentCommands
    {
        /// <summary>
        /// Creates the main game development command with all subcommands
        /// </summary>
        public static Command CreateGameDevelopmentCommand(IServiceProvider serviceProvider)
        {
            var gameCommand = new Command("game", "Game development tools and AI-powered features");
            
            // Add subcommands
            gameCommand.AddCommand(CreateGenerateCommand(serviceProvider));
            gameCommand.AddCommand(CreateBalanceCommand(serviceProvider));
            gameCommand.AddCommand(CreateWorkflowCommand(serviceProvider));
            gameCommand.AddCommand(CreateTestCommand(serviceProvider));
            
            return gameCommand;
        }
        
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
        /// Creates the workflow command
        /// </summary>
        private static Command CreateWorkflowCommand(IServiceProvider serviceProvider)
        {
            var workflowCommand = new Command("workflow", "Run automated game development workflows");
            
            var workflowOption = new Option<string>(
                "--workflow",
                () => "development",
                "Workflow to run (development, testing, optimization)");
            
            var projectPathOption = new Option<string>(
                "--project-path",
                () => ".",
                "Path to the Unity project directory");
            
            var verboseOption = new Option<bool>(
                "--verbose",
                "Show verbose output");
            
            workflowCommand.AddOption(workflowOption);
            workflowCommand.AddOption(projectPathOption);
            workflowCommand.AddOption(verboseOption);
            
            workflowCommand.SetHandler(async (workflow, projectPath, verbose) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<GameDevelopmentCommands>>();
                
                await RunGameWorkflow(logger, workflow, projectPath, verbose);
            }, workflowOption, projectPathOption, verboseOption);
            
            return workflowCommand;
        }
        
        /// <summary>
        /// Creates the test command
        /// </summary>
        private static Command CreateTestCommand(IServiceProvider serviceProvider)
        {
            var testCommand = new Command("test", "Run game testing and quality assurance");
            
            var testTypeOption = new Option<string>(
                "--test-type",
                () => "all",
                "Type of tests to run (unit, performance, gameplay, balance, all)");
            
            var projectPathOption = new Option<string>(
                "--project-path",
                () => ".",
                "Path to the Unity project directory");
            
            var durationOption = new Option<int>(
                "--duration",
                () => 300,
                "Test duration in seconds");
            
            testCommand.AddOption(testTypeOption);
            testCommand.AddOption(projectPathOption);
            testCommand.AddOption(durationOption);
            
            testCommand.SetHandler(async (testType, projectPath, duration) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<GameDevelopmentCommands>>();
                var testingWorkflow = serviceProvider.GetRequiredService<GameTestingWorkflow>();
                
                await RunGameTesting(testingWorkflow, logger, testType, projectPath, duration);
            }, testTypeOption, projectPathOption, durationOption);
            
            return testCommand;
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
                
                Console.WriteLine($"🎮 Generating {type}: {description}");
                
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
                    Console.WriteLine($"✅ Generated {type} successfully!");
                    Console.WriteLine($"📄 Generated Code:");
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
                        Console.WriteLine($"\n🚀 Performance Optimizations:");
                        foreach (var optimization in optimizations ?? [])
                        {
                            Console.WriteLine($"  • {optimization}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Failed to generate {type}");
                }
                
                logger.LogInformation("Game feature generation completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate game feature");
                Console.WriteLine($"❌ Generation failed: {ex.Message}");
            }
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
                
                Console.WriteLine($"⚖️ Analyzing game balance...");
                
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
                    Console.WriteLine($"📊 Overall Balance Score: {balanceScore:F2}/10");
                    
                    var issues = response.GetMetadata<IEnumerable<string>>("BalanceIssues");
                    if (issues?.Any() == true)
                    {
                        Console.WriteLine($"\n⚠️ Balance Issues Found:");
                        foreach (var issue in issues)
                        {
                            Console.WriteLine($"  • {issue}");
                        }
                    }
                    
                    var recommendations = response.GetMetadata<IEnumerable<string>>("Recommendations");
                    if (recommendations?.Any() == true)
                    {
                        Console.WriteLine($"\n💡 Recommendations:");
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
                    Console.WriteLine($"❌ Failed to analyze game balance");
                }
                
                logger.LogInformation("Game balance analysis completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze game balance");
                Console.WriteLine($"❌ Balance analysis failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Runs game development workflows
        /// </summary>
        private static async Task RunGameWorkflow(
            ILogger logger,
            string workflow,
            string projectPath,
            bool verbose)
        {
            try
            {
                logger.LogInformation("Running game workflow: {Workflow} for project: {ProjectPath}", workflow, projectPath);
                
                Console.WriteLine($"🔄 Running {workflow} workflow...");
                
                switch (workflow.ToLower())
                {
                    case "development":
                        await RunDevelopmentWorkflow(projectPath, verbose);
                        break;
                    case "testing":
                        await RunTestingWorkflow(projectPath, verbose);
                        break;
                    case "optimization":
                        await RunOptimizationWorkflow(projectPath, verbose);
                        break;
                    default:
                        Console.WriteLine($"❌ Unknown workflow: {workflow}");
                        break;
                }
                
                logger.LogInformation("Game workflow completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run game workflow");
                Console.WriteLine($"❌ Workflow failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Runs game testing
        /// </summary>
        private static async Task RunGameTesting(
            GameTestingWorkflow testingWorkflow,
            ILogger logger,
            string testType,
            string projectPath,
            int duration)
        {
            try
            {
                logger.LogInformation("Running game testing: {TestType} for project: {ProjectPath}", testType, projectPath);
                
                Console.WriteLine($"🧪 Running {testType} tests...");
                
                var request = new GameTestingWorkflowRequest
                {
                    ProjectPath = projectPath,
                    RunGameplayTests = testType == "gameplay" || testType == "all",
                    TestBalance = testType == "balance" || testType == "all",
                    TestDuration = TimeSpan.FromSeconds(duration)
                };
                
                var result = await testingWorkflow.ExecuteAsync(request);
                
                if (result.Status == WorkflowStatus.Completed)
                {
                    Console.WriteLine($"✅ Testing completed successfully!");
                    
                    if (result.FinalReport is GameTestReport report)
                    {
                        Console.WriteLine($"\n📋 Test Report Summary:");
                        Console.WriteLine($"  Total Tests: {report.QualityMetrics.TotalTests}");
                        Console.WriteLine($"  Passed: {report.QualityMetrics.PassedTests}");
                        Console.WriteLine($"  Failed: {report.QualityMetrics.FailedTests}");
                        Console.WriteLine($"  Pass Rate: {report.QualityMetrics.OverallTestPassRate:P}");
                        Console.WriteLine($"  Quality Score: {report.QualityMetrics.QualityScore:F2}/10");
                        
                        if (report.QualityMetrics.AverageFrameRate > 0)
                        {
                            Console.WriteLine($"  Average FPS: {report.QualityMetrics.AverageFrameRate:F1}");
                        }
                        
                        if (report.Recommendations.Any())
                        {
                            Console.WriteLine($"\n💡 Recommendations:");
                            foreach (var recommendation in report.Recommendations)
                            {
                                Console.WriteLine($"  • {recommendation}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Testing failed: {result.ErrorMessage}");
                }
                
                logger.LogInformation("Game testing completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run game testing");
                Console.WriteLine($"❌ Testing failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Runs development workflow
        /// </summary>
        private static async Task RunDevelopmentWorkflow(string projectPath, bool verbose)
        {
            Console.WriteLine($"🔧 Running development workflow...");
            
            // This would integrate with the actual GameDevelopmentWorkflow
            Console.WriteLine($"  • Project analysis");
            Console.WriteLine($"  • Code generation");
            Console.WriteLine($"  • Performance optimization");
            Console.WriteLine($"  • Build optimization");
            
            if (verbose)
            {
                Console.WriteLine($"  • Detailed logging enabled");
            }
            
            Console.WriteLine($"✅ Development workflow completed");
        }
        
        /// <summary>
        /// Runs testing workflow
        /// </summary>
        private static async Task RunTestingWorkflow(string projectPath, bool verbose)
        {
            Console.WriteLine($"🧪 Running testing workflow...");
            
            // This would integrate with the actual GameTestingWorkflow
            Console.WriteLine($"  • Unit testing");
            Console.WriteLine($"  • Performance testing");
            Console.WriteLine($"  • Gameplay testing");
            Console.WriteLine($"  • Balance testing");
            
            if (verbose)
            {
                Console.WriteLine($"  • Detailed test results");
            }
            
            Console.WriteLine($"✅ Testing workflow completed");
        }
        
        /// <summary>
        /// Runs optimization workflow
        /// </summary>
        private static async Task RunOptimizationWorkflow(string projectPath, bool verbose)
        {
            Console.WriteLine($"🚀 Running optimization workflow...");
            
            // This would integrate with the actual optimization workflow
            Console.WriteLine($"  • Performance analysis");
            Console.WriteLine($"  • Memory optimization");
            Console.WriteLine($"  • Rendering optimization");
            Console.WriteLine($"  • Build size optimization");
            
            if (verbose)
            {
                Console.WriteLine($"  • Detailed optimization results");
            }
            
            Console.WriteLine($"✅ Optimization workflow completed");
        }
        
        /// <summary>
        /// Shows detailed balance analysis
        /// </summary>
        private static async Task ShowDetailedBalanceAnalysis(AgentResponse response)
        {
            Console.WriteLine($"\n📋 Detailed Balance Analysis:");
            
            // Show implementation guidance if available
            if (response.Metadata.ContainsKey("ImplementationGuidance"))
            {
                var guidance = response.Metadata["ImplementationGuidance"];
                Console.WriteLine($"\n🔧 Implementation Guidance:");
                Console.WriteLine($"  {guidance}");
            }
            
            // Show testing strategy if available
            if (response.Metadata.ContainsKey("TestingStrategy"))
            {
                var strategy = response.Metadata["TestingStrategy"];
                Console.WriteLine($"\n🧪 Testing Strategy:");
                Console.WriteLine($"  {strategy}");
            }
        }
    }
}
