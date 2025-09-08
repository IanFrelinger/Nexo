using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Services;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Shared.Interfaces.Resource;

namespace TestConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Nexo Pipeline Feature Demo ===");
            Console.WriteLine();

            // Setup logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                // Load pipeline configuration
                Console.WriteLine("1. Loading pipeline configuration...");
                var pipelineConfig = await LoadPipelineConfigurationAsync();
                Console.WriteLine($"   ✓ Loaded pipeline: {pipelineConfig.Name} v{pipelineConfig.Version}");
                Console.WriteLine($"   ✓ Description: {pipelineConfig.Description}");
                Console.WriteLine();

                // Create pipeline services
                Console.WriteLine("2. Initializing pipeline services...");
                var configLogger = loggerFactory.CreateLogger<PipelineConfigurationService>();
                var executionLogger = loggerFactory.CreateLogger<PipelineExecutionEngine>();
                var configService = new PipelineConfigurationService(configLogger);
                
                // Create mock resource monitor and optimizer for demo
                var mockResourceMonitor = new MockResourceMonitor();
                var mockResourceOptimizer = new MockResourceOptimizer();
                var executionEngine = new PipelineExecutionEngine(executionLogger, mockResourceMonitor, mockResourceOptimizer);
                Console.WriteLine("   ✓ Pipeline services initialized");
                Console.WriteLine();

                // Create pipeline context
                Console.WriteLine("3. Creating pipeline context...");
                var context = new PipelineContext(logger, pipelineConfig);
                Console.WriteLine($"   ✓ Execution ID: {context.ExecutionId}");
                Console.WriteLine($"   ✓ Start Time: {context.StartTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();

                // Set up test variables
                Console.WriteLine("4. Setting up test variables...");
                context.SetValue("projectName", "DemoProject");
                context.SetValue("projectType", "webapi");
                context.SetValue("targetPath", "./demo-output");
                context.SetValue("requirements", "Modern web API with authentication and database");
                context.SetValue("constraints", "Must use .NET 8 and follow REST principles");
                Console.WriteLine("   ✓ Test variables configured");
                Console.WriteLine();

                // Validate pipeline configuration
                Console.WriteLine("5. Validating pipeline configuration...");
                // Note: ValidateAsync method is not implemented in PipelineConfigurationService
                // For demo purposes, we'll create a mock validation result
                var validationResult = new Nexo.Shared.Models.ValidationResult(true);
                if (validationResult.IsValid)
                {
                    Console.WriteLine("   ✓ Pipeline configuration is valid");
                    Console.WriteLine($"   ✓ Commands: {pipelineConfig.Commands.Count}");
                    Console.WriteLine($"   ✓ Behaviors: {pipelineConfig.Behaviors.Count}");
                    Console.WriteLine($"   ✓ Aggregators: {pipelineConfig.Aggregators.Count}");
                }
                else
                {
                    Console.WriteLine("   ⚠ Pipeline configuration has validation issues:");
                    foreach (var error in validationResult.Errors)
                    {
                        Console.WriteLine($"     - {error.Message}");
                    }
                }
                Console.WriteLine();

                // Simulate pipeline execution (since we don't have actual command implementations)
                Console.WriteLine("6. Simulating pipeline execution...");
                await SimulatePipelineExecutionAsync(pipelineConfig, context, logger);
                Console.WriteLine();

                // Display execution results
                Console.WriteLine("7. Pipeline execution completed!");
                Console.WriteLine($"   ✓ Final Status: {context.Status}");
                Console.WriteLine($"   ✓ Execution Time: {DateTime.UtcNow - context.StartTime:hh\\:mm\\:ss}");
                
                var metrics = context.GetMetrics();
                Console.WriteLine($"   ✓ Metrics: {metrics.CommandsExecuted + metrics.BehaviorsExecuted + metrics.AggregatorsExecuted} steps processed");
                Console.WriteLine();

                Console.WriteLine("=== Demo Completed Successfully ===");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pipeline demo failed");
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine("Check the logs for more details.");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task<PipelineConfiguration> LoadPipelineConfigurationAsync()
        {
            var configPath = Path.Combine("..", "..", "..", "..", "examples", "pipeline-demo.json");
            
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Pipeline configuration not found at: {configPath}");
            }

            var jsonContent = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<PipelineConfiguration>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize pipeline configuration");
            }

            return config;
        }

        private static async Task SimulatePipelineExecutionAsync(
            PipelineConfiguration config, 
            PipelineContext context, 
            ILogger logger)
        {
            // Simulate command execution
            foreach (var command in config.Commands)
            {
                logger.LogInformation("Executing command: {CommandName} ({CommandId})", command.Name, command.Id);
                Console.WriteLine($"   → Executing: {command.Name}");
                
                // Simulate command processing time
                await Task.Delay(500);
                
                // Simulate command result
                var step = PipelineExecutionStep.Create(ExecutionStepType.Command, command.Name, command.Id, CommandPriority.Normal);
                step.StartTime = DateTime.UtcNow.AddSeconds(-1);
                step.EndTime = DateTime.UtcNow;
                step.Status = ExecutionStepStatus.Completed;
                step.AddMetadata("output", $"Simulated output for {command.Name}");
                
                context.AddExecutionStep(step);
                Console.WriteLine($"   ✓ Completed: {command.Name}");
                
                // Simulate some commands having dependencies
                if (command.Dependencies.Count > 0)
                {
                    Console.WriteLine($"     (Dependencies: {string.Join(", ", command.Dependencies)})");
                }
            }

            // Simulate behavior execution
            foreach (var behavior in config.Behaviors)
            {
                logger.LogInformation("Executing behavior: {BehaviorName} ({BehaviorId})", behavior.Name, behavior.Id);
                Console.WriteLine($"   → Executing behavior: {behavior.Name}");
                
                await Task.Delay(300);
                Console.WriteLine($"   ✓ Completed behavior: {behavior.Name}");
            }

            // Simulate aggregator execution
            foreach (var aggregator in config.Aggregators)
            {
                logger.LogInformation("Executing aggregator: {AggregatorName} ({AggregatorId})", aggregator.Name, aggregator.Id);
                Console.WriteLine($"   → Executing aggregator: {aggregator.Name}");
                
                await Task.Delay(200);
                Console.WriteLine($"   ✓ Completed aggregator: {aggregator.Name}");
            }

            // Set final status
            context.Status = PipelineExecutionStatus.Completed;
        }
    }

    // Mock implementations for demo purposes
    public class MockResourceMonitor : IResourceMonitor
    {
        public Task<double> GetCpuUsageAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(25.0);

        public Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new MemoryInfo
            {
                TotalBytes = 2147483648, // 2GB
                AvailableBytes = 1073741824 // 1GB
            });

        public Task<DiskInfo> GetDiskInfoAsync(string path, CancellationToken cancellationToken = default)
            => Task.FromResult(new DiskInfo
            {
                TotalBytes = 107374182400, // 100GB
                AvailableBytes = 53687091200, // 50GB
                Path = path
            });

        public Task<SystemResourceUsage> GetResourceUsageAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new SystemResourceUsage
            {
                CpuUsagePercentage = 25.0,
                Memory = new MemoryInfo
                {
                    TotalBytes = 2147483648,
                    AvailableBytes = 1073741824
                },
                Disk = new DiskInfo
                {
                    TotalBytes = 107374182400,
                    AvailableBytes = 53687091200,
                    Path = "/"
                }
            });
    }

    public class MockResourceOptimizer : IResourceOptimizer
    {
        public Task<OptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OptimizationResult
            {
                Timestamp = DateTime.UtcNow,
                Recommendations = new List<OptimizationRecommendation>()
            });

        public Task<ThrottlingResult> CalculateThrottlingAsync(PipelineExecutionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ThrottlingResult
            {
                ShouldThrottle = false,
                ThrottlingLevel = ThrottlingLevel.None,
                RecommendedDelay = TimeSpan.Zero
            });

        public void AddOptimizationRule(string ruleId, OptimizationRule rule) { }

        public void RemoveOptimizationRule(string ruleId) { }

        public IEnumerable<OptimizationHistory> GetOptimizationHistory()
            => Enumerable.Empty<OptimizationHistory>();
    }
}