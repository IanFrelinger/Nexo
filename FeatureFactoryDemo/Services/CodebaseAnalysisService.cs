using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;

namespace FeatureFactoryDemo.Services
{
    public partial class CodebaseAnalysisService
    {
        private readonly ILogger<CodebaseAnalysisService> _logger;
        private readonly ICodebaseAnalyzer _codebaseAnalyzer;

        public CodebaseAnalysisService(
            ILogger<CodebaseAnalysisService> logger,
            ICodebaseAnalyzer codebaseAnalyzer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _codebaseAnalyzer = codebaseAnalyzer ?? throw new ArgumentNullException(nameof(codebaseAnalyzer));
        }

        public async Task DemoCodebaseContextAnalysis()
        {
            Console.WriteLine("\n--- Demo 1: Codebase Context Analysis ---");
            
            var description = "Create a user authentication service";
            var context = await GetCodebaseContextAsync(description);
            
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Codebase Context: {context}");
        }

        public async Task DemoSimilarCommandAnalysis()
        {
            Console.WriteLine("\n--- Demo 2: Similar Command Analysis ---");
            
            var description = "Create a payment processing service";
            var similarCommands = await GetSimilarCommandsAsync(description);
            
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Similar Commands Found: {similarCommands.Count}");
            
            foreach (var command in similarCommands.Take(3))
            {
                Console.WriteLine($"  - {command.Command} (Similarity: {command.SimilarityScore:F2})");
            }
        }

        public async Task DemoCodebasePatternAnalysis()
        {
            Console.WriteLine("\n--- Demo 3: Codebase Pattern Analysis ---");
            
            var patterns = await AnalyzeCodebasePatternsAsync();
            
            Console.WriteLine($"Codebase Patterns Found: {patterns.Count}");
            
            foreach (var pattern in patterns.Take(5))
            {
                Console.WriteLine($"  - {pattern.PatternName}: {pattern.Description}");
            }
        }

        public async Task<string> GetCodebaseContextAsync(string description)
        {
            try
            {
                _logger.LogInformation("Analyzing codebase context for: {Description}", description);

                // Simulate codebase analysis
                await Task.Delay(200);

                // Mock codebase context based on description
                if (description.Contains("authentication") || description.Contains("auth"))
                {
                    return "Found existing authentication patterns: JWT tokens, OAuth2 integration, user management services";
                }
                else if (description.Contains("payment") || description.Contains("billing"))
                {
                    return "Found existing payment patterns: Stripe integration, PayPal support, invoice generation";
                }
                else if (description.Contains("database") || description.Contains("data"))
                {
                    return "Found existing data patterns: Entity Framework, repository pattern, unit of work";
                }
                else
                {
                    return "Found general patterns: dependency injection, logging, error handling, configuration management";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing codebase context");
                return string.Empty;
            }
        }

        public async Task<List<CommandHistory>> GetSimilarCommandsAsync(string description)
        {
            try
            {
                _logger.LogInformation("Finding similar commands for: {Description}", description);

                // Simulate command history analysis
                await Task.Delay(150);

                var similarCommands = new List<CommandHistory>();

                // Mock similar commands based on description
                if (description.Contains("authentication"))
                {
                    similarCommands.Add(new CommandHistory
                    {
                        Command = "Create user login endpoint",
                        SimilarityScore = 0.85,
                        Timestamp = DateTime.UtcNow.AddDays(-1)
                    });
                    similarCommands.Add(new CommandHistory
                    {
                        Command = "Implement JWT token validation",
                        SimilarityScore = 0.78,
                        Timestamp = DateTime.UtcNow.AddDays(-2)
                    });
                }
                else if (description.Contains("payment"))
                {
                    similarCommands.Add(new CommandHistory
                    {
                        Command = "Create payment processing service",
                        SimilarityScore = 0.92,
                        Timestamp = DateTime.UtcNow.AddDays(-3)
                    });
                    similarCommands.Add(new CommandHistory
                    {
                        Command = "Implement Stripe integration",
                        SimilarityScore = 0.88,
                        Timestamp = DateTime.UtcNow.AddDays(-5)
                    });
                }
                else
                {
                    similarCommands.Add(new CommandHistory
                    {
                        Command = "Create REST API service",
                        SimilarityScore = 0.65,
                        Timestamp = DateTime.UtcNow.AddDays(-7)
                    });
                    similarCommands.Add(new CommandHistory
                    {
                        Command = "Implement CRUD operations",
                        SimilarityScore = 0.58,
                        Timestamp = DateTime.UtcNow.AddDays(-10)
                    });
                }

                return similarCommands.OrderByDescending(c => c.SimilarityScore).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar commands");
                return new List<CommandHistory>();
            }
        }

        public async Task<List<CodebasePattern>> AnalyzeCodebasePatternsAsync()
        {
            try
            {
                _logger.LogInformation("Analyzing codebase patterns");

                // Simulate pattern analysis
                await Task.Delay(300);

                var patterns = new List<CodebasePattern>
                {
                    new CodebasePattern
                    {
                        PatternName = "Repository Pattern",
                        Description = "Data access abstraction layer",
                        Frequency = 0.85,
                        Files = new[] { "UserRepository.cs", "OrderRepository.cs", "ProductRepository.cs" }
                    },
                    new CodebasePattern
                    {
                        PatternName = "Dependency Injection",
                        Description = "Service registration and resolution",
                        Frequency = 0.92,
                        Files = new[] { "Startup.cs", "Program.cs", "ServiceCollectionExtensions.cs" }
                    },
                    new CodebasePattern
                    {
                        PatternName = "Logging",
                        Description = "Structured logging throughout application",
                        Frequency = 0.78,
                        Files = new[] { "UserService.cs", "OrderService.cs", "PaymentService.cs" }
                    },
                    new CodebasePattern
                    {
                        PatternName = "Error Handling",
                        Description = "Global exception handling middleware",
                        Frequency = 0.88,
                        Files = new[] { "GlobalExceptionMiddleware.cs", "ErrorController.cs" }
                    },
                    new CodebasePattern
                    {
                        PatternName = "Configuration",
                        Description = "Strongly-typed configuration classes",
                        Frequency = 0.75,
                        Files = new[] { "DatabaseSettings.cs", "EmailSettings.cs", "PaymentSettings.cs" }
                    }
                };

                return patterns.OrderByDescending(p => p.Frequency).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing codebase patterns");
                return new List<CodebasePattern>();
            }
        }
    }

    public partial class CodebasePattern
    {
        public string PatternName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Frequency { get; set; }
        public string[] Files { get; set; } = Array.Empty<string>();
    }
}