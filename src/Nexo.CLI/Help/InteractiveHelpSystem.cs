using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Help
{
    /// <summary>
    /// Interactive help system with searchable documentation and examples
    /// </summary>
    public class InteractiveHelpSystem : IInteractiveHelpSystem
    {
        private readonly IDocumentationGenerator _documentationGenerator;
        private readonly IExampleRepository _exampleRepository;
        private readonly ILogger<InteractiveHelpSystem> _logger;
        
        public InteractiveHelpSystem(
            IDocumentationGenerator documentationGenerator,
            IExampleRepository exampleRepository,
            ILogger<InteractiveHelpSystem> logger)
        {
            _documentationGenerator = documentationGenerator;
            _exampleRepository = exampleRepository;
            _logger = logger;
        }
        
        public async Task ShowInteractiveHelp(string? specificTopic = null)
        {
            if (specificTopic != null)
            {
                await ShowTopicHelp(specificTopic);
                return;
            }
            
            await ShowMainHelpMenu();
        }
        
        public async Task ShowCommandHelp(string commandName)
        {
            Console.Clear();
            Console.WriteLine($"📖 Help for: {commandName}");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            try
            {
                var documentation = await _documentationGenerator.GenerateCommandDocumentationAsync(commandName);
                Console.WriteLine(documentation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate documentation for command: {Command}", commandName);
                Console.WriteLine($"❌ Failed to load documentation for '{commandName}': {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
        
        public async Task SearchDocumentation(string searchTerm)
        {
            Console.Clear();
            Console.WriteLine($"🔍 Search Results for '{searchTerm}'");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            try
            {
                var results = await _documentationGenerator.SearchDocumentationAsync(searchTerm);
                
                if (!results.Any())
                {
                    Console.WriteLine("No results found. Try different search terms.");
                    Console.WriteLine();
                    Console.WriteLine("💡 Search Tips:");
                    Console.WriteLine("  • Use specific keywords");
                    Console.WriteLine("  • Try different variations");
                    Console.WriteLine("  • Check spelling");
                    Console.WriteLine();
                }
                else
                {
                    foreach (var result in results.Take(10))
                    {
                        Console.WriteLine($"📄 {result.Title}");
                        Console.WriteLine($"   {result.Summary}");
                        Console.WriteLine($"   Category: {result.Category} | Relevance: {result.Relevance:P0}");
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search documentation for: {SearchTerm}", searchTerm);
                Console.WriteLine($"❌ Search failed: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
        
        public async Task ShowExamples(string? category = null)
        {
            Console.Clear();
            Console.WriteLine("🎯 Examples");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            try
            {
                var examples = category != null 
                    ? (await _exampleRepository.GetExamplesByCategory()).GetValueOrDefault(category, new List<CommandExample>())
                    : await _exampleRepository.GetAllExamplesAsync();
                
                if (!examples.Any())
                {
                    Console.WriteLine($"No examples found{(category != null ? $" for category '{category}'" : "")}.");
                    return;
                }
                
                var examplesByCategory = examples.GroupBy(e => e.Category);
                
                foreach (var categoryGroup in examplesByCategory)
                {
                    Console.WriteLine($"📁 {categoryGroup.Key}:");
                    Console.WriteLine();
                    
                    foreach (var example in categoryGroup)
                    {
                        Console.WriteLine($"  🎯 {example.Title}");
                        Console.WriteLine($"     {example.Description}");
                        Console.WriteLine($"     Command: {example.CommandLine}");
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load examples for category: {Category}", category);
                Console.WriteLine($"❌ Failed to load examples: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
        
        public async Task ShowCommandBrowser()
        {
            Console.Clear();
            Console.WriteLine("📖 Command Browser");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            try
            {
                var topics = await _documentationGenerator.GetAvailableTopicsAsync();
                var categories = topics.GroupBy(t => t.Category).OrderBy(g => g.Key);
                
                foreach (var category in categories)
                {
                    Console.WriteLine($"🔧 {category.Key}:");
                    Console.WriteLine();
                    
                    foreach (var topic in category.OrderBy(t => t.Name))
                    {
                        Console.WriteLine($"  • {topic.Name.PadRight(20)} - {topic.Description}");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load command browser");
                Console.WriteLine($"❌ Failed to load command browser: {ex.Message}");
            }
            
            Console.WriteLine("💡 Type 'nexo <command> --help' for detailed information about any command");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
        
        private async Task ShowMainHelpMenu()
        {
            Console.Clear();
            Console.WriteLine("🔍 Nexo Interactive Help System");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            Console.WriteLine("📚 Available Topics:");
            Console.WriteLine("  1. Getting Started");
            Console.WriteLine("  2. Project Management");
            Console.WriteLine("  3. Code Generation");
            Console.WriteLine("  4. Performance Optimization");
            Console.WriteLine("  5. Unity Game Development");
            Console.WriteLine("  6. Real-Time Adaptation");
            Console.WriteLine("  7. Pipeline Management");
            Console.WriteLine("  8. Command Reference");
            Console.WriteLine("  9. Examples & Tutorials");
            Console.WriteLine();
            
            Console.WriteLine("💡 Interactive Options:");
            Console.WriteLine("  • Type a number to explore a topic");
            Console.WriteLine("  • Type 'search <term>' to search documentation");
            Console.WriteLine("  • Type 'commands' to browse all commands");
            Console.WriteLine("  • Type 'examples' to see practical examples");
            Console.WriteLine("  • Type 'q' to exit help");
            Console.WriteLine();
            
            while (true)
            {
                Console.Write("help> ");
                var input = Console.ReadLine()?.Trim().ToLower();
                
                if (string.IsNullOrEmpty(input)) continue;
                if (input == "q" || input == "quit" || input == "exit") break;
                
                await ProcessHelpInput(input);
            }
        }
        
        private async Task ProcessHelpInput(string input)
        {
            if (int.TryParse(input, out int topicNumber) && topicNumber >= 1 && topicNumber <= 9)
            {
                await ShowTopicByNumber(topicNumber);
            }
            else if (input.StartsWith("search "))
            {
                var searchTerm = input.Substring(7);
                await SearchDocumentation(searchTerm);
            }
            else if (input == "commands")
            {
                await ShowCommandBrowser();
            }
            else if (input == "examples")
            {
                await ShowExamples();
            }
            else
            {
                Console.WriteLine("❓ Unknown command. Try typing a number, 'search <term>', 'commands', or 'examples'");
            }
        }
        
        private async Task ShowTopicByNumber(int topicNumber)
        {
            var topic = topicNumber switch
            {
                1 => "getting-started",
                2 => "project-management",
                3 => "code-generation",
                4 => "performance-optimization",
                5 => "unity-game-development",
                6 => "real-time-adaptation",
                7 => "pipeline-management",
                8 => "command-reference",
                9 => "examples-tutorials",
                _ => "general"
            };
            
            await ShowTopicHelp(topic);
        }
        
        private async Task ShowTopicHelp(string topic)
        {
            Console.Clear();
            Console.WriteLine($"📖 Help Topic: {GetTopicDisplayName(topic)}");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            var content = await GetTopicContent(topic);
            Console.WriteLine(content);
            
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
        
        private string GetTopicDisplayName(string topic)
        {
            return topic switch
            {
                "getting-started" => "Getting Started",
                "project-management" => "Project Management",
                "code-generation" => "Code Generation",
                "performance-optimization" => "Performance Optimization",
                "unity-game-development" => "Unity Game Development",
                "real-time-adaptation" => "Real-Time Adaptation",
                "pipeline-management" => "Pipeline Management",
                "command-reference" => "Command Reference",
                "examples-tutorials" => "Examples & Tutorials",
                _ => "General Help"
            };
        }
        
        private async Task<string> GetTopicContent(string topic)
        {
            // This would integrate with actual documentation content
            return topic switch
            {
                "getting-started" => await GetGettingStartedContent(),
                "project-management" => await GetProjectManagementContent(),
                "code-generation" => await GetCodeGenerationContent(),
                "performance-optimization" => await GetPerformanceOptimizationContent(),
                "unity-game-development" => await GetUnityGameDevelopmentContent(),
                "real-time-adaptation" => await GetRealTimeAdaptationContent(),
                "pipeline-management" => await GetPipelineManagementContent(),
                "command-reference" => await GetCommandReferenceContent(),
                "examples-tutorials" => await GetExamplesTutorialsContent(),
                _ => "General help content would be displayed here."
            };
        }
        
        private async Task<string> GetGettingStartedContent()
        {
            return """
            # Getting Started with Nexo
            
            Welcome to Nexo, the AI-Enhanced Development Environment Orchestration Platform!
            
            ## Quick Start
            
            1. **Initialize a Project**
               ```bash
               nexo project init --name MyProject --type webapi
               ```
            
            2. **Start Interactive Mode**
               ```bash
               nexo interactive
               ```
            
            3. **View Real-Time Dashboard**
               ```bash
               nexo dashboard
               ```
            
            ## Key Features
            
            - 🚀 AI-powered code generation and analysis
            - 📊 Real-time performance monitoring
            - 🔄 Automatic adaptation and optimization
            - 🎮 Unity game development support
            - 🌐 Cross-platform development
            - 📈 Advanced analytics and insights
            
            ## Next Steps
            
            - Explore project management commands
            - Try the interactive development mode
            - Set up performance monitoring
            - Configure AI providers
            """;
        }
        
        private async Task<string> GetProjectManagementContent()
        {
            return """
            # Project Management
            
            Nexo provides comprehensive project management capabilities.
            
            ## Creating Projects
            
            ```bash
            # Create a new web API project
            nexo project init --name MyApi --type webapi --ai
            
            # Create a console application
            nexo project init --name MyConsole --type console
            
            # Create a class library
            nexo project init --name MyLibrary --type library
            ```
            
            ## Scaffolding Code
            
            ```bash
            # Scaffold a controller
            nexo project scaffold --type controller --name UserController
            
            # Scaffold a service
            nexo project scaffold --type service --name UserService
            
            # Scaffold a model
            nexo project scaffold --type model --name User
            ```
            
            ## Environment Management
            
            ```bash
            # Set up development environment
            nexo project env --setup
            
            # Check environment requirements
            nexo project env --check
            
            # Update development tools
            nexo project env --update
            ```
            """;
        }
        
        private async Task<string> GetCodeGenerationContent()
        {
            return """
            # Code Generation
            
            Generate high-quality code using AI-powered templates and patterns.
            
            ## AI-Enhanced Generation
            
            ```bash
            # Generate code with AI assistance
            nexo generate code --type service --name UserService --ai
            
            # Generate tests
            nexo generate tests --path ./src --coverage
            
            # Generate documentation
            nexo generate docs --project . --format markdown
            ```
            
            ## Template-Based Generation
            
            ```bash
            # Use predefined templates
            nexo generate template --name crud-service --output ./src/Services
            
            # Create custom templates
            nexo generate template --create --name my-template
            ```
            """;
        }
        
        private async Task<string> GetPerformanceOptimizationContent()
        {
            return """
            # Performance Optimization
            
            Optimize your applications with real-time monitoring and AI-powered suggestions.
            
            ## Performance Analysis
            
            ```bash
            # Analyze current performance
            nexo analyze performance --project .
            
            # Monitor in real-time
            nexo monitor start --metrics performance,memory,cpu
            
            # Generate optimization report
            nexo optimize report --detailed
            ```
            
            ## Automatic Optimization
            
            ```bash
            # Enable automatic optimization
            nexo optimize enable --strategy performance,memory
            
            # Apply optimizations
            nexo optimize apply --recommendations
            
            # Monitor optimization results
            nexo optimize status
            ```
            """;
        }
        
        private async Task<string> GetUnityGameDevelopmentContent()
        {
            return """
            # Unity Game Development
            
            Specialized tools for Unity game development and optimization.
            
            ## Unity Project Management
            
            ```bash
            # Initialize Unity project
            nexo unity project init --name MyGame --template 3d
            
            # Build for multiple platforms
            nexo unity build --platforms windows,macos,linux
            
            # Run Unity tests
            nexo unity test --playmode --editmode
            ```
            
            ## Performance Optimization
            
            ```bash
            # Optimize Unity build
            nexo unity optimize --build-size --performance
            
            # Analyze Unity project
            nexo unity analyze --assets --scripts --performance
            ```
            """;
        }
        
        private async Task<string> GetRealTimeAdaptationContent()
        {
            return """
            # Real-Time Adaptation
            
            Automatically adapt your application based on runtime conditions and user feedback.
            
            ## Adaptation Engine
            
            ```bash
            # Start adaptation engine
            nexo adaptation start --strategies performance,resource,ux
            
            # Monitor adaptations
            nexo adaptation status --detailed
            
            # Configure adaptation rules
            nexo adaptation configure --rules ./adaptation-rules.json
            ```
            
            ## Learning System
            
            ```bash
            # Enable continuous learning
            nexo adaptation learn --enable --feedback-collection
            
            # View learning insights
            nexo adaptation insights --timeframe 7d
            
            # Apply learned optimizations
            nexo adaptation apply --learned
            ```
            """;
        }
        
        private async Task<string> GetPipelineManagementContent()
        {
            return """
            # Pipeline Management
            
            Create and manage complex development workflows and CI/CD pipelines.
            
            ## Pipeline Creation
            
            ```bash
            # Create a new pipeline
            nexo pipeline create --name build-test-deploy --template standard
            
            # Define pipeline steps
            nexo pipeline steps --add build,test,deploy --parallel test,analyze
            
            # Configure pipeline triggers
            nexo pipeline triggers --on-commit --on-schedule "0 2 * * *"
            ```
            
            ## Pipeline Execution
            
            ```bash
            # Execute pipeline
            nexo pipeline execute --name build-test-deploy --async
            
            # Monitor pipeline progress
            nexo pipeline status --name build-test-deploy --watch
            
            # Get pipeline results
            nexo pipeline results --name build-test-deploy --detailed
            ```
            """;
        }
        
        private async Task<string> GetCommandReferenceContent()
        {
            return """
            # Command Reference
            
            Complete reference for all Nexo CLI commands.
            
            ## Core Commands
            
            - `nexo project` - Project management and scaffolding
            - `nexo analyze` - Code and performance analysis
            - `nexo optimize` - Performance optimization
            - `nexo generate` - Code and feature generation
            - `nexo test` - Testing and validation
            - `nexo monitor` - Real-time monitoring
            - `nexo adaptation` - Real-time adaptation management
            - `nexo pipeline` - Workflow and pipeline management
            - `nexo unity` - Unity game development
            - `nexo web` - Web development and optimization
            
            ## System Commands
            
            - `nexo interactive` - Start interactive mode
            - `nexo dashboard` - Open real-time dashboard
            - `nexo help` - Show help information
            - `nexo version` - Display version information
            - `nexo status` - Show system status
            
            ## Getting Help
            
            Use `--help` with any command for detailed information:
            ```bash
            nexo project --help
            nexo analyze --help
            ```
            """;
        }
        
        private async Task<string> GetExamplesTutorialsContent()
        {
            return """
            # Examples & Tutorials
            
            Practical examples and step-by-step tutorials for common tasks.
            
            ## Quick Examples
            
            ### Creating a Web API
            
            ```bash
            # 1. Initialize project
            nexo project init --name MyApi --type webapi --ai
            
            # 2. Scaffold controller
            nexo project scaffold --type controller --name ProductsController
            
            # 3. Add tests
            nexo generate tests --type controller --name ProductsController
            
            # 4. Run and test
            nexo test run --project .
            ```
            
            ### Performance Optimization
            
            ```bash
            # 1. Analyze current performance
            nexo analyze performance --project . --detailed
            
            # 2. Enable monitoring
            nexo monitor start --metrics all
            
            # 3. Apply optimizations
            nexo optimize apply --recommendations
            
            # 4. Monitor results
            nexo dashboard
            ```
            
            ## Interactive Tutorials
            
            Use the interactive mode for guided tutorials:
            ```bash
            nexo interactive
            # Then type 'tutorial' for available tutorials
            ```
            """;
        }
    }
}
