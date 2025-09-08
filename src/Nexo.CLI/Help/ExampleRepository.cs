using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Help
{
    /// <summary>
    /// Repository for command examples and tutorials
    /// </summary>
    public class ExampleRepository : IExampleRepository
    {
        private readonly ILogger<ExampleRepository> _logger;
        private readonly List<CommandExample> _examples;
        
        public ExampleRepository(ILogger<ExampleRepository> logger)
        {
            _logger = logger;
            _examples = InitializeExamples();
        }
        
        public async Task<Dictionary<string, List<CommandExample>>> GetExamplesByCategory()
        {
            return _examples
                .GroupBy(e => e.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        
        public async Task<IEnumerable<CommandExample>> GetExamplesForCommandAsync(string commandName)
        {
            return _examples.Where(e => e.CommandLine.Contains(commandName));
        }
        
        public async Task<IEnumerable<CommandExample>> GetAllExamplesAsync()
        {
            return _examples;
        }
        
        private List<CommandExample> InitializeExamples()
        {
            return new List<CommandExample>
            {
                // Project Management Examples
                new CommandExample
                {
                    Title = "Create Web API Project",
                    Description = "Initialize a new ASP.NET Core Web API project with AI enhancement",
                    CommandLine = "nexo project init --name MyApi --type webapi --ai",
                    ExpectedOutput = "✅ Project 'MyApi' initialized successfully with AI enhancements!",
                    Category = "Project Management",
                    Tags = new List<string> { "project", "init", "webapi", "ai" }
                },
                new CommandExample
                {
                    Title = "Scaffold Controller",
                    Description = "Generate a new API controller with CRUD operations",
                    CommandLine = "nexo project scaffold --type controller --name ProductsController",
                    ExpectedOutput = "✅ Created ProductsController.cs with full CRUD operations",
                    Category = "Project Management",
                    Tags = new List<string> { "scaffold", "controller", "crud" }
                },
                new CommandExample
                {
                    Title = "Set Up Development Environment",
                    Description = "Configure development environment for the current project",
                    CommandLine = "nexo project env --setup",
                    ExpectedOutput = "✅ Development environment configured successfully",
                    Category = "Project Management",
                    Tags = new List<string> { "environment", "setup", "development" }
                },
                
                // Code Generation Examples
                new CommandExample
                {
                    Title = "Generate Service with AI",
                    Description = "Create a service class using AI-powered code generation",
                    CommandLine = "nexo generate code --type service --name UserService --ai",
                    ExpectedOutput = "✅ Generated UserService.cs with AI-optimized implementation",
                    Category = "Code Generation",
                    Tags = new List<string> { "generate", "service", "ai" }
                },
                new CommandExample
                {
                    Title = "Generate Unit Tests",
                    Description = "Create comprehensive unit tests for existing code",
                    CommandLine = "nexo generate tests --path ./src/Services --coverage",
                    ExpectedOutput = "✅ Generated 15 unit tests with 95% coverage",
                    Category = "Code Generation",
                    Tags = new List<string> { "generate", "tests", "coverage" }
                },
                new CommandExample
                {
                    Title = "Generate Documentation",
                    Description = "Create API documentation from code comments",
                    CommandLine = "nexo generate docs --project . --format markdown",
                    ExpectedOutput = "✅ Generated comprehensive API documentation",
                    Category = "Code Generation",
                    Tags = new List<string> { "generate", "docs", "documentation" }
                },
                
                // Performance Optimization Examples
                new CommandExample
                {
                    Title = "Analyze Performance",
                    Description = "Analyze application performance and identify bottlenecks",
                    CommandLine = "nexo analyze performance --project . --detailed",
                    ExpectedOutput = "📊 Performance analysis complete. Found 3 optimization opportunities.",
                    Category = "Performance Optimization",
                    Tags = new List<string> { "analyze", "performance", "optimization" }
                },
                new CommandExample
                {
                    Title = "Start Performance Monitoring",
                    Description = "Begin real-time performance monitoring",
                    CommandLine = "nexo monitor start --metrics performance,memory,cpu",
                    ExpectedOutput = "📊 Performance monitoring started. View dashboard with 'nexo dashboard'",
                    Category = "Performance Optimization",
                    Tags = new List<string> { "monitor", "performance", "real-time" }
                },
                new CommandExample
                {
                    Title = "Apply Optimizations",
                    Description = "Apply recommended performance optimizations",
                    CommandLine = "nexo optimize apply --recommendations",
                    ExpectedOutput = "✅ Applied 5 performance optimizations. Expected 25% improvement.",
                    Category = "Performance Optimization",
                    Tags = new List<string> { "optimize", "apply", "recommendations" }
                },
                
                // Unity Game Development Examples
                new CommandExample
                {
                    Title = "Initialize Unity Project",
                    Description = "Create a new Unity project with optimal settings",
                    CommandLine = "nexo unity project init --name MyGame --template 3d",
                    ExpectedOutput = "🎮 Unity project 'MyGame' initialized with 3D template",
                    Category = "Unity Game Development",
                    Tags = new List<string> { "unity", "project", "init", "3d" }
                },
                new CommandExample
                {
                    Title = "Build for Multiple Platforms",
                    Description = "Build Unity project for Windows, macOS, and Linux",
                    CommandLine = "nexo unity build --platforms windows,macos,linux",
                    ExpectedOutput = "🎮 Building for 3 platforms... ✅ All builds completed successfully",
                    Category = "Unity Game Development",
                    Tags = new List<string> { "unity", "build", "cross-platform" }
                },
                new CommandExample
                {
                    Title = "Run Unity Tests",
                    Description = "Execute both play mode and edit mode tests",
                    CommandLine = "nexo unity test --playmode --editmode",
                    ExpectedOutput = "🧪 Running Unity tests... ✅ All tests passed (45/45)",
                    Category = "Unity Game Development",
                    Tags = new List<string> { "unity", "test", "playmode", "editmode" }
                },
                
                // Real-Time Adaptation Examples
                new CommandExample
                {
                    Title = "Start Adaptation Engine",
                    Description = "Enable real-time adaptation with multiple strategies",
                    CommandLine = "nexo adaptation start --strategies performance,resource,ux",
                    ExpectedOutput = "🔄 Adaptation engine started with 3 active strategies",
                    Category = "Real-Time Adaptation",
                    Tags = new List<string> { "adaptation", "start", "strategies" }
                },
                new CommandExample
                {
                    Title = "Monitor Adaptations",
                    Description = "View detailed adaptation status and recent improvements",
                    CommandLine = "nexo adaptation status --detailed",
                    ExpectedOutput = "📊 Adaptation Status: Active | Recent improvements: +15% performance",
                    Category = "Real-Time Adaptation",
                    Tags = new List<string> { "adaptation", "status", "monitor" }
                },
                new CommandExample
                {
                    Title = "Enable Learning System",
                    Description = "Activate continuous learning and feedback collection",
                    CommandLine = "nexo adaptation learn --enable --feedback-collection",
                    ExpectedOutput = "🧠 Learning system enabled. Collecting user feedback for optimization.",
                    Category = "Real-Time Adaptation",
                    Tags = new List<string> { "adaptation", "learn", "feedback" }
                },
                
                // Pipeline Management Examples
                new CommandExample
                {
                    Title = "Create Build Pipeline",
                    Description = "Set up a complete build, test, and deploy pipeline",
                    CommandLine = "nexo pipeline create --name build-test-deploy --template standard",
                    ExpectedOutput = "🔧 Pipeline 'build-test-deploy' created with standard template",
                    Category = "Pipeline Management",
                    Tags = new List<string> { "pipeline", "create", "build", "deploy" }
                },
                new CommandExample
                {
                    Title = "Execute Pipeline",
                    Description = "Run the pipeline asynchronously",
                    CommandLine = "nexo pipeline execute --name build-test-deploy --async",
                    ExpectedOutput = "🚀 Pipeline execution started. Monitor with 'nexo pipeline status'",
                    Category = "Pipeline Management",
                    Tags = new List<string> { "pipeline", "execute", "async" }
                },
                new CommandExample
                {
                    Title = "Monitor Pipeline Progress",
                    Description = "Watch pipeline execution in real-time",
                    CommandLine = "nexo pipeline status --name build-test-deploy --watch",
                    ExpectedOutput = "👀 Watching pipeline progress... Step 2/5: Running tests",
                    Category = "Pipeline Management",
                    Tags = new List<string> { "pipeline", "status", "watch" }
                },
                
                // Interactive Mode Examples
                new CommandExample
                {
                    Title = "Start Interactive Mode",
                    Description = "Launch interactive CLI with intelligent suggestions",
                    CommandLine = "nexo interactive",
                    ExpectedOutput = "🚀 Welcome to Nexo Interactive Mode\nType 'help' for available commands",
                    Category = "Interactive Mode",
                    Tags = new List<string> { "interactive", "cli", "suggestions" }
                },
                new CommandExample
                {
                    Title = "Open Real-Time Dashboard",
                    Description = "Launch the monitoring dashboard",
                    CommandLine = "nexo dashboard",
                    ExpectedOutput = "📊 Real-time dashboard opened. Press 'Q' to quit.",
                    Category = "Interactive Mode",
                    Tags = new List<string> { "dashboard", "monitor", "real-time" }
                },
                new CommandExample
                {
                    Title = "Get Command Suggestions",
                    Description = "Get intelligent command recommendations",
                    CommandLine = "nexo suggest",
                    ExpectedOutput = "💡 Based on your current context, try: 'nexo analyze performance'",
                    Category = "Interactive Mode",
                    Tags = new List<string> { "suggest", "recommendations", "ai" }
                },
                
                // Testing Examples
                new CommandExample
                {
                    Title = "Run All Tests",
                    Description = "Execute all tests in the project",
                    CommandLine = "nexo test run --project .",
                    ExpectedOutput = "🧪 Running tests... ✅ 127/127 tests passed",
                    Category = "Testing",
                    Tags = new List<string> { "test", "run", "all" }
                },
                new CommandExample
                {
                    Title = "Generate Test Coverage Report",
                    Description = "Create detailed test coverage analysis",
                    CommandLine = "nexo test coverage --detailed --output html",
                    ExpectedOutput = "📊 Test coverage: 94.2% | Report saved to coverage.html",
                    Category = "Testing",
                    Tags = new List<string> { "test", "coverage", "report" }
                },
                new CommandExample
                {
                    Title = "Run Performance Tests",
                    Description = "Execute performance and load tests",
                    CommandLine = "nexo test performance --load --duration 5m",
                    ExpectedOutput = "⚡ Performance tests completed. Average response time: 45ms",
                    Category = "Testing",
                    Tags = new List<string> { "test", "performance", "load" }
                }
            };
        }
    }
}
