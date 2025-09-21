using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nexo.Core.Contracts;
using Nexo.Core.Pipeline;
using Nexo.Core.Configuration;
using Nexo.Core.Extensions;

namespace Nexo.Examples
{
    /// <summary>
    /// Example demonstrating how to use the recovery loop functionality in the pipeline.
    /// </summary>
    public class RecoveryLoopUsageExample
    {
        public static async Task Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("repair-loop-config-example.json", optional: true)
                .Build();

            // Build host with services
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Add pipeline services with repair loop configuration
                    services.AddPipelineServices<string, string>(configuration.GetSection("RepairLoop"));

                    // Register example implementations
                    services.AddTransient<IExtensionGenerator<string, string>, ExampleGenerator>();
                    services.AddTransient<ICompilationGate, ExampleCompilationGate>();
                    services.AddTransient<IPolicyGate<string>, ExamplePolicyGate>();
                    services.AddTransient<IArtifactPublisher<string>, ExampleArtifactPublisher>();
                    services.AddTransient<IRepairStrategy<string, string>, ExampleRepairStrategy>();
                    services.AddTransient<ICanaryDeployer<string>, ExampleCanaryDeployer>();
                    services.AddTransient<IRollbackStrategy<string>, ExampleRollbackStrategy>();
                })
                .Build();

            // Get the pipeline from DI
            var pipeline = host.Services.GetRequiredService<ExtensionPipeline<string, string>>();

            // Example 1: Successful pipeline with repair
            Console.WriteLine("=== Example 1: Successful pipeline with repair ===");
            var result1 = await pipeline.RunAsync("Generate user service");
            Console.WriteLine($"Result: Success={result1.Succeeded}, QualityScore={result1.QualityScore}");
            Console.WriteLine($"Notes: {string.Join(", ", result1.Notes)}");
            Console.WriteLine();

            // Example 2: Pipeline with canary failure and rollback
            Console.WriteLine("=== Example 2: Pipeline with canary failure and rollback ===");
            // This would require a different configuration or mock setup
            Console.WriteLine("This example would demonstrate canary failure triggering rollback");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Example generator implementation.
    /// </summary>
    public class ExampleGenerator : IExtensionGenerator<string, string>
    {
        public Task<GenerationResult<string>> GenerateAsync(string request, CancellationToken cancellationToken = default)
        {
            var artifact = $"Generated artifact for: {request}";
            var sourceCode = $"// Generated code for {request}\npublic class GeneratedClass {{ }}";
            var notes = new List<string> { "Generated using example generator" };

            return Task.FromResult(new GenerationResult<string>(artifact, sourceCode, notes));
        }
    }

    /// <summary>
    /// Example compilation gate that sometimes fails.
    /// </summary>
    public class ExampleCompilationGate : ICompilationGate
    {
        private static int _callCount = 0;

        public Task<ValidationResult> ValidateAsync(string sourceCode, CancellationToken cancellationToken = default)
        {
            _callCount++;
            
            // Simulate failure on first call, success on subsequent calls
            var passed = _callCount > 1;
            var findings = passed ? new List<string>() : new List<string> { "Missing semicolon", "Syntax error" };

            return Task.FromResult(new ValidationResult(passed, "ExampleCompilationGate", findings));
        }
    }

    /// <summary>
    /// Example policy gate.
    /// </summary>
    public class ExamplePolicyGate : IPolicyGate<string>
    {
        public Task<PolicyOutcome> EvaluateAsync(string artifact, CancellationToken cancellationToken = default)
        {
            var passed = true;
            var findings = new List<string>();
            var score = 0.9;

            return Task.FromResult(new PolicyOutcome(passed, "ExamplePolicy", findings, score));
        }
    }

    /// <summary>
    /// Example artifact publisher.
    /// </summary>
    public class ExampleArtifactPublisher : IArtifactPublisher<string>
    {
        public Task<PipelineReport> PublishAsync(string artifact, IReadOnlyList<ValidationResult> validationResults, PolicyOutcome policyOutcome, CancellationToken cancellationToken = default)
        {
            var report = new PipelineReport(
                $"example-{Guid.NewGuid():N}",
                policyOutcome.Passed,
                policyOutcome.Score,
                new List<string> { "Published using example publisher" }
            );

            return Task.FromResult(report);
        }
    }

    /// <summary>
    /// Example repair strategy that fixes compilation issues.
    /// </summary>
    public class ExampleRepairStrategy : IRepairStrategy<string, string>
    {
        private readonly ILogger<ExampleRepairStrategy> _logger;

        public ExampleRepairStrategy(ILogger<ExampleRepairStrategy> logger)
        {
            _logger = logger;
        }

        public Task<(bool Applied, string PatchedSource, string Note)> TryRepairAsync(
            string request, 
            GenerationResult<string> lastGen, 
            IReadOnlyList<ValidationResult> gateFindings, 
            PolicyOutcome? policy, 
            CancellationToken ct)
        {
            _logger.LogInformation("Attempting repair for request: {Request}", request);

            // Simple repair: add semicolon if missing
            var patchedSource = lastGen.SourceCode;
            if (!patchedSource.EndsWith(";"))
            {
                patchedSource += ";";
            }

            // Add some additional fixes
            patchedSource += "\n// Fixed by repair strategy";

            var applied = true;
            var note = "Applied syntax fixes and added missing semicolon";

            _logger.LogInformation("Repair result: Applied={Applied}, Note={Note}", applied, note);

            return Task.FromResult((applied, patchedSource, note));
        }
    }

    /// <summary>
    /// Example canary deployer.
    /// </summary>
    public class ExampleCanaryDeployer : ICanaryDeployer<string>
    {
        private readonly ILogger<ExampleCanaryDeployer> _logger;

        public ExampleCanaryDeployer(ILogger<ExampleCanaryDeployer> logger)
        {
            _logger = logger;
        }

        public Task<(bool Healthy, string CanaryId, string Note)> CanaryAsync(string artifact, CancellationToken ct)
        {
            _logger.LogInformation("Deploying to canary: {Artifact}", artifact);

            var canaryId = $"canary-{Guid.NewGuid():N}";
            var healthy = true; // Simulate successful canary deployment
            var note = "Canary deployment successful - all health checks passed";

            _logger.LogInformation("Canary result: Healthy={Healthy}, CanaryId={CanaryId}", healthy, canaryId);

            return Task.FromResult((healthy, canaryId, note));
        }
    }

    /// <summary>
    /// Example rollback strategy.
    /// </summary>
    public class ExampleRollbackStrategy : IRollbackStrategy<string>
    {
        private readonly ILogger<ExampleRollbackStrategy> _logger;

        public ExampleRollbackStrategy(ILogger<ExampleRollbackStrategy> logger)
        {
            _logger = logger;
        }

        public Task<string> RollbackAsync(string artifact, string reason, CancellationToken ct)
        {
            _logger.LogInformation("Rolling back artifact: {Artifact}, Reason: {Reason}", artifact, reason);

            var result = $"Successfully rolled back artifact due to: {reason}";

            _logger.LogInformation("Rollback result: {Result}", result);

            return Task.FromResult(result);
        }
    }
}
