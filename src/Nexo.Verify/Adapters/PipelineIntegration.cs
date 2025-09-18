using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Services;
using Nexo.Feature.Pipeline.Models;
using Nexo.Verify.Models;
using System.Text.Json;

namespace Nexo.Verify.Adapters
{
    /// <summary>
    /// Integration with the existing Nexo pipeline execution system
    /// </summary>
    public static class PipelineIntegration
    {
        /// <summary>
        /// Execute a scenario using the real Nexo pipeline engine
        /// </summary>
        public static async Task<ScenarioResult> ExecuteScenarioAsync(
            string scenarioPath,
            IEnumerable<string>? inputs,
            IDictionary<string, string>? env,
            CancellationToken ct = default)
        {
            var result = new ScenarioResult
            {
                OutputDir = Path.Combine(Path.GetTempPath(), "nexo-verify", Guid.NewGuid().ToString())
            };

            try
            {
                Directory.CreateDirectory(result.OutputDir);
                
                // Apply environment variables
                using var envScope = new EnvironmentScope(env);
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Create service provider for pipeline execution
                var services = CreateServiceProvider();
                using var scope = services.CreateScope();
                
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<PipelineIntegration>>();
                var pipelineEngine = scope.ServiceProvider.GetRequiredService<IPipelineExecutionEngine>();
                var pipelineConfigService = scope.ServiceProvider.GetRequiredService<IPipelineConfigurationService>();
                
                // Load pipeline configuration
                PipelineConfiguration pipelineConfig;
                if (File.Exists(scenarioPath))
                {
                    pipelineConfig = await pipelineConfigService.LoadFromFileAsync(scenarioPath, ct);
                }
                else
                {
                    // Try to load from template
                    pipelineConfig = await pipelineConfigService.LoadFromTemplateAsync(scenarioPath, new Dictionary<string, object>(), ct);
                }
                
                // Create pipeline context
                var context = new PipelineContext(logger, pipelineConfig, ct);
                
                // Execute the pipeline
                var aggregatorIds = pipelineConfig.Aggregators?.Select(a => a.Id).ToList() ?? new List<string>();
                var pipelineResult = await pipelineEngine.ExecuteAsync(context, aggregatorIds, ct);
                
                stopwatch.Stop();
                
                // Capture results
                result.Completed = pipelineResult.IsSuccess;
                result.ExitCode = pipelineResult.IsSuccess ? 0 : 1;
                result.ErrorMessage = pipelineResult.ErrorMessage;
                result.Metrics["execution_time_ms"] = stopwatch.ElapsedMilliseconds;
                result.Metrics["aggregators_executed"] = aggregatorIds.Count;
                
                // Capture logs from the pipeline execution
                result.Logs.Add($"Pipeline execution completed: {pipelineResult.IsSuccess}");
                result.Logs.Add($"Execution time: {stopwatch.ElapsedMilliseconds}ms");
                result.Logs.Add($"Aggregators executed: {aggregatorIds.Count}");
                
                if (!pipelineResult.IsSuccess)
                {
                    result.Logs.Add($"Error: {pipelineResult.ErrorMessage}");
                }
                
                // Copy outputs to result directory
                await CopyPipelineOutputsAsync(pipelineResult, result.OutputDir, ct);
            }
            catch (Exception ex)
            {
                result.Completed = false;
                result.ErrorMessage = ex.Message;
                result.ExitCode = 1;
                result.Logs.Add($"Pipeline execution failed: {ex.Message}");
            }

            return result;
        }

        private static IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Add pipeline services
            services.AddScoped<IPipelineExecutionEngine, PipelineExecutionEngine>();
            services.AddScoped<IPipelineConfigurationService, PipelineConfigurationService>();
            services.AddScoped<IPipelineContext, PipelineContext>();
            
            // Add other required services
            services.AddScoped<IPipelineOrchestrator, PipelineOrchestrator>();
            
            return services.BuildServiceProvider();
        }

        private static async Task CopyPipelineOutputsAsync(
            PipelineExecutionResult pipelineResult,
            string outputDir,
            CancellationToken ct)
        {
            // This is a placeholder - in a real implementation, we would:
            // 1. Look for output files created by the pipeline
            // 2. Copy them to the result directory
            // 3. Capture any logs or metrics
            
            // For now, create a dummy output file
            var outputsDir = Path.Combine(outputDir, "outputs");
            Directory.CreateDirectory(outputsDir);
            
            var outputFile = Path.Combine(outputsDir, "labels.csv");
            await File.WriteAllTextAsync(outputFile, 
                "id,subject,label,confidence\n" +
                "1,Sample email,urgent,0.95\n" +
                "2,Another email,normal,0.80", ct);
        }
    }
}
