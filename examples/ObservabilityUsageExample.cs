using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Observability;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Metrics;

namespace Nexo.Examples;

/// <summary>
/// Example demonstrating how to use Nexo observability features.
/// </summary>
public partial class ObservabilityUsageExample
{
    /// <summary>
    /// Demonstrates basic observability setup and usage.
    /// </summary>
    public static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("observability-config-example.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // Create host builder
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add Nexo observability
                services.AddNexoObservability(configuration);
                
                // Add your application services
                services.AddScoped<ExampleService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        // Build and run the host
        using var host = hostBuilder.Build();
        await host.RunAsync();
    }
}

/// <summary>
/// Example service demonstrating observability usage.
/// </summary>
public partial class ExampleService
{
    private readonly ILogger<ExampleService> _logger;
    private readonly ActivitySource _generationActivitySource;
    private readonly ActivitySource _validationActivitySource;
    private readonly ActivitySource _policyActivitySource;
    private readonly PipelineMetrics _metrics;

    public ExampleService(
        ILogger<ExampleService> logger,
        ActivitySource generationActivitySource,
        ActivitySource validationActivitySource,
        ActivitySource policyActivitySource,
        PipelineMetrics metrics)
    {
        _logger = logger;
        _generationActivitySource = generationActivitySource;
        _validationActivitySource = validationActivitySource;
        _policyActivitySource = policyActivitySource;
        _metrics = metrics;
    }

    /// <summary>
    /// Example method demonstrating activity and metrics usage.
    /// </summary>
    public async Task<string> ProcessRequestAsync(string request)
    {
        using var activity = _generationActivitySource.StartActivity("example.process_request");
        activity?.SetTag("request.type", "example");
        activity?.SetTag("request.id", Guid.NewGuid().ToString("N")[..8]);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing request: {Request}", request);

            // Simulate generation step
            var result = await SimulateGenerationAsync(request);
            
            // Simulate validation step
            var isValid = await SimulateValidationAsync(result);
            
            if (!isValid)
            {
                _metrics.RecordValidationFailure("example_validation", "invalid_result");
                activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                throw new InvalidOperationException("Validation failed");
            }

            // Simulate policy evaluation
            var score = await SimulatePolicyEvaluationAsync(result);
            _metrics.RecordPolicyScore(score, "example_policy");

            stopwatch.Stop();
            _metrics.RecordPipelineSuccess("example_pipeline", stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation("Request processed successfully in {Duration}ms", stopwatch.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordPipelineFailure("example_pipeline", stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Request processing failed after {Duration}ms", stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    private async Task<string> SimulateGenerationAsync(string request)
    {
        using var activity = _generationActivitySource.StartActivity("generation.simulate");
        activity?.SetTag("generation.type", "example");

        var stopwatch = Stopwatch.StartNew();
        
        // Simulate work
        await Task.Delay(100);
        
        stopwatch.Stop();
        _metrics.RecordGenerationDuration(stopwatch.Elapsed.TotalMilliseconds, "example_generation", true);
        
        return $"Generated: {request}";
    }

    private async Task<bool> SimulateValidationAsync(string result)
    {
        using var activity = _validationActivitySource.StartActivity("validation.simulate");
        activity?.SetTag("validation.type", "example");

        var stopwatch = Stopwatch.StartNew();
        
        // Simulate work
        await Task.Delay(50);
        
        stopwatch.Stop();
        var isValid = result.Length > 5;
        _metrics.RecordValidationDuration(stopwatch.Elapsed.TotalMilliseconds, "example_validation", isValid);
        
        return isValid;
    }

    private async Task<long> SimulatePolicyEvaluationAsync(string result)
    {
        using var activity = _policyActivitySource.StartActivity("policy.simulate");
        activity?.SetTag("policy.type", "example");

        var stopwatch = Stopwatch.StartNew();
        
        // Simulate work
        await Task.Delay(75);
        
        stopwatch.Stop();
        _metrics.RecordPolicyEvaluationDuration(stopwatch.Elapsed.TotalMilliseconds, "example_policy");
        
        // Return a score based on result length
        return Math.Min(100, result.Length * 10);
    }
}
