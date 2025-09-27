using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Help
{
    /// <summary>
    /// Core help system functionality
    /// </summary>
    public partial class InteractiveHelpSystem : IInteractiveHelpSystem
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
        
        private async Task ShowMainHelpMenu()
        {
            Console.Clear();
            Console.WriteLine("Search Nexo Interactive Help System");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            Console.WriteLine("Documentation Available Topics:");
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
            
            Console.WriteLine("Idea Interactive Options:");
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
                Console.WriteLine("UNKNOWN Unknown command. Try typing a number, 'search <term>', 'commands', or 'examples'");
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
    }
}
