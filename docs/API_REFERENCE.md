# Nexo API Reference

## Overview

This document provides comprehensive API reference for Nexo's core services and components. All APIs are designed with pipeline-first architecture and cross-platform compatibility in mind.

## Table of Contents

1. [Core Services](#core-services)
2. [Pipeline API](#pipeline-api)
3. [AI Services](#ai-services)
4. [Safety Services](#safety-services)
5. [Monitoring Services](#monitoring-services)
6. [Beta Testing Services](#beta-testing-services)
7. [Data Models](#data-models)
8. [Error Handling](#error-handling)

## Core Services

### IterationStrategySelector

The core service for selecting optimal iteration strategies based on context and requirements.

```csharp
public interface IIterationStrategySelector
{
    Task<IIterationStrategy<T>> SelectStrategy<T>(IterationContext context);
    void RegisterStrategy<T>(IIterationStrategy<T> strategy);
    List<IIterationStrategy<T>> GetAvailableStrategies<T>();
}
```

#### Methods

##### SelectStrategy<T>(IterationContext context)

Selects the optimal iteration strategy for the given context.

**Parameters:**
- `context` (IterationContext): The context containing data size, requirements, and environment profile

**Returns:**
- `Task<IIterationStrategy<T>>`: The selected strategy

**Example:**
```csharp
var context = new IterationContext
{
    DataSize = 1000,
    Requirements = new IterationRequirements
    {
        PrioritizeCpu = true,
        RequiresParallelization = true
    },
    EnvironmentProfile = new EnvironmentProfile
    {
        CurrentPlatform = PlatformType.Desktop,
        CpuCores = Environment.ProcessorCount
    }
};

var strategy = await selector.SelectStrategy<int>(context);
```

##### RegisterStrategy<T>(IIterationStrategy<T> strategy)

Registers a new iteration strategy.

**Parameters:**
- `strategy` (IIterationStrategy<T>): The strategy to register

**Example:**
```csharp
var customStrategy = new CustomIterationStrategy<int>();
selector.RegisterStrategy(customStrategy);
```

##### GetAvailableStrategies<T>()

Gets all available strategies for the specified type.

**Returns:**
- `List<IIterationStrategy<T>>`: List of available strategies

**Example:**
```csharp
var strategies = selector.GetAvailableStrategies<string>();
foreach (var strategy in strategies)
{
    Console.WriteLine($"Strategy: {strategy.StrategyId}");
}
```

## Pipeline API

### Pipeline Creation

```csharp
public static class Pipeline
{
    public static IPipelineBuilder Create();
    public static IPipelineBuilder<T> Create<T>();
}
```

#### IPipelineBuilder

```csharp
public interface IPipelineBuilder
{
    IPipelineBuilder AddStep<TStep>() where TStep : class, IPipelineStep;
    IPipelineBuilder AddStep<TStep>(TStep step) where TStep : class, IPipelineStep;
    IPipelineBuilder AddStep<TStep>(Func<IServiceProvider, TStep> factory) where TStep : class, IPipelineStep;
    IPipelineBuilder WithOptimization(OptimizationStrategy strategy);
    IPipelineBuilder WithErrorHandling(ErrorHandlingStrategy strategy);
    IPipeline Build();
}
```

#### IPipeline

```csharp
public interface IPipeline
{
    Task<PipelineResult> ExecuteAsync(object input);
    Task<PipelineResult<T>> ExecuteAsync<T>(T input);
    Task<PipelineResult> ExecuteAsync(object input, PipelineContext context);
    Task<PipelineResult<T>> ExecuteAsync<T>(T input, PipelineContext context);
}
```

#### Example Usage

```csharp
// Create a simple pipeline
var pipeline = Pipeline.Create()
    .AddStep<DataValidationStep>()
    .AddStep<DataProcessingStep>()
    .AddStep<DataStorageStep>()
    .WithOptimization(OptimizationStrategy.Performance)
    .Build();

// Execute the pipeline
var result = await pipeline.ExecuteAsync(data);

// Create a typed pipeline
var typedPipeline = Pipeline.Create<MyData>()
    .AddStep<ValidationStep<MyData>>()
    .AddStep<ProcessingStep<MyData>>()
    .Build();

var result = await typedPipeline.ExecuteAsync(myData);
```

## AI Services

### IAI Service

```csharp
public interface IAIService
{
    Task<CodeGenerationResult> GenerateCodeAsync(string prompt, CodeGenerationContext context);
    Task<CodeReviewResult> ReviewCodeAsync(string code, CodeReviewOptions options);
    Task<RefactoringResult> RefactorCodeAsync(string code, RefactoringOptions options);
    Task<AnalysisResult> AnalyzeCodeAsync(string code, AnalysisOptions options);
}
```

#### Methods

##### GenerateCodeAsync(string prompt, CodeGenerationContext context)

Generates code based on natural language description.

**Parameters:**
- `prompt` (string): Natural language description of desired code
- `context` (CodeGenerationContext): Context including framework, database, etc.

**Returns:**
- `Task<CodeGenerationResult>`: Generated code and metadata

**Example:**
```csharp
var context = new CodeGenerationContext
{
    Framework = "ASP.NET Core",
    Database = "Entity Framework",
    Authentication = "JWT",
    TargetPlatform = PlatformType.Web
};

var result = await aiService.GenerateCodeAsync(
    "Create a REST API controller for user management with CRUD operations",
    context
);

Console.WriteLine($"Generated {result.Files.Count} files");
```

##### ReviewCodeAsync(string code, CodeReviewOptions options)

Reviews code and provides suggestions for improvement.

**Parameters:**
- `code` (string): Code to review
- `options` (CodeReviewOptions): Review options

**Returns:**
- `Task<CodeReviewResult>`: Review results and suggestions

**Example:**
```csharp
var options = new CodeReviewOptions
{
    CheckPerformance = true,
    CheckSecurity = true,
    CheckBestPractices = true,
    SuggestImprovements = true
};

var review = await aiService.ReviewCodeAsync(code, options);

foreach (var issue in review.Issues)
{
    Console.WriteLine($"{issue.Severity}: {issue.Message}");
}
```

## Safety Services

### IUserSafetyService

```csharp
public interface IUserSafetyService
{
    Task<SafetyCheckResult> ValidateOperationAsync(UserOperation operation);
    Task<SafeguardExecutionResult> ExecuteSafeguardsAsync(UserOperation operation, SafetyCheckResult safetyResult);
    Task<BackupResult> CreateBackupAsync(UserOperation operation);
    Task<DryRunResult> ExecuteDryRunAsync(UserOperation operation);
    Task<RollbackResult> RollbackOperationAsync(string operationId);
}
```

#### Methods

##### ValidateOperationAsync(UserOperation operation)

Validates a user operation for safety risks.

**Parameters:**
- `operation` (UserOperation): The operation to validate

**Returns:**
- `Task<SafetyCheckResult>`: Safety validation results

**Example:**
```csharp
var operation = new UserOperation
{
    Type = OperationType.DeleteFile,
    TargetPath = "/important-data/file.txt",
    IsDestructive = true,
    AffectedFiles = 1
};

var safetyResult = await safetyService.ValidateOperationAsync(operation);

if (!safetyResult.IsSafeToProceed)
{
    Console.WriteLine("Operation has safety risks:");
    foreach (var risk in safetyResult.Risks)
    {
        Console.WriteLine($"- {risk.Message}");
    }
}
```

##### ExecuteDryRunAsync(UserOperation operation)

Executes a dry-run to preview changes.

**Parameters:**
- `operation` (UserOperation): The operation to simulate

**Returns:**
- `Task<DryRunResult>`: Dry-run results showing what would change

**Example:**
```csharp
var dryRun = await safetyService.ExecuteDryRunAsync(operation);

Console.WriteLine($"Would affect {dryRun.Changes.Count} files");
Console.WriteLine($"Estimated duration: {dryRun.EstimatedDuration}");

foreach (var change in dryRun.Changes)
{
    Console.WriteLine($"{change.ChangeType}: {change.Path}");
}
```

### IBackupService

```csharp
public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(BackupRequest request);
    Task<RestoreResult> RestoreFromBackupAsync(string backupId);
    Task<BackupInfo?> GetBackupByOperationIdAsync(string operationId);
    Task<List<BackupInfo>> ListBackupsAsync(string userId, BackupFilter? filter = null);
    Task<CleanupResult> CleanupOldBackupsAsync(BackupRetentionPolicy policy);
}
```

## Monitoring Services

### IProductionMonitoringService

```csharp
public interface IProductionMonitoringService
{
    Task<MonitoringInitializationResult> InitializeAsync(MonitoringConfiguration config);
    Task<MetricsCollectionResult> CollectMetricsAsync();
    Task<HealthCheckResult> PerformHealthChecksAsync();
    Task<MonitoringReport> GenerateReportAsync(MonitoringReportRequest request);
    Task<AlertingConfigurationResult> ConfigureAlertingAsync(AlertingConfiguration config);
}
```

#### Methods

##### CollectMetricsAsync()

Collects real-time metrics from all systems.

**Returns:**
- `Task<MetricsCollectionResult>`: Collected metrics and metadata

**Example:**
```csharp
var metrics = await monitoringService.CollectMetricsAsync();

Console.WriteLine($"Collected {metrics.MetricsCollected} metrics");
foreach (var metric in metrics.Metrics)
{
    Console.WriteLine($"{metric.Name}: {metric.Value} {metric.Unit}");
}
```

##### PerformHealthChecksAsync()

Performs comprehensive health checks.

**Returns:**
- `Task<HealthCheckResult>`: Health check results

**Example:**
```csharp
var health = await monitoringService.PerformHealthChecksAsync();

Console.WriteLine($"Overall health: {health.OverallHealth}");
foreach (var check in health.HealthChecks)
{
    Console.WriteLine($"{check.Name}: {check.Status}");
}
```

## Beta Testing Services

### IBetaTestingProgram

```csharp
public interface IBetaTestingProgram
{
    Task<BetaProgramResult> InitializeProgramAsync(BetaProgramConfiguration config);
    Task<RecruitmentResult> RecruitUsersAsync(string programId, RecruitmentRequest request);
    Task<FeedbackCollectionResult> CollectFeedbackAsync(string programId, FeedbackCollectionRequest request);
    Task<BetaAnalyticsReport> GenerateAnalyticsReportAsync(string programId, AnalyticsReportRequest request);
    Task<ProgramHealthReport> MonitorProgramHealthAsync(string programId);
}
```

### IBetaOnboardingService

```csharp
public interface IBetaOnboardingService
{
    Task<OnboardingSession> StartOnboardingAsync(string userId, OnboardingPreferences preferences);
    Task<EnvironmentValidationResult> ValidateEnvironmentAsync(string userId);
    Task<OnboardingStepResult> ExecuteStepAsync(string sessionId, string stepId);
    Task<OnboardingCompletionResult> CompleteOnboardingAsync(string sessionId);
    Task<OnboardingProgress> GetProgressAsync(string sessionId);
}
```

## Data Models

### IterationContext

```csharp
public class IterationContext
{
    public int DataSize { get; set; }
    public IterationRequirements Requirements { get; set; } = new();
    public EnvironmentProfile EnvironmentProfile { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### IterationRequirements

```csharp
public class IterationRequirements
{
    public bool PrioritizeCpu { get; set; }
    public bool PrioritizeMemory { get; set; }
    public bool RequiresParallelization { get; set; }
    public bool RequiresLowLatency { get; set; }
    public Dictionary<string, object> CustomRequirements { get; set; } = new();
}
```

### EnvironmentProfile

```csharp
public class EnvironmentProfile
{
    public PlatformType CurrentPlatform { get; set; }
    public long AvailableMemory { get; set; }
    public int CpuCores { get; set; }
    public string? OperatingSystem { get; set; }
    public Dictionary<string, object> PlatformSpecific { get; set; } = new();
}
```

### PerformanceEstimate

```csharp
public class PerformanceEstimate
{
    public TimeSpan EstimatedDuration { get; set; }
    public long EstimatedMemoryUsage { get; set; }
    public double CpuUtilization { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}
```

## Error Handling

### Exception Types

#### NexoException
Base exception for all Nexo-related errors.

```csharp
public class NexoException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Details { get; }
    
    public NexoException(string message, string errorCode, Dictionary<string, object>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details ?? new Dictionary<string, object>();
    }
}
```

#### StrategySelectionException
Thrown when strategy selection fails.

```csharp
public class StrategySelectionException : NexoException
{
    public StrategySelectionException(string message, Dictionary<string, object>? details = null)
        : base(message, "STRATEGY_SELECTION_FAILED", details)
    {
    }
}
```

#### SafetyValidationException
Thrown when safety validation fails.

```csharp
public class SafetyValidationException : NexoException
{
    public List<SafetyRisk> Risks { get; }
    
    public SafetyValidationException(string message, List<SafetyRisk> risks)
        : base(message, "SAFETY_VALIDATION_FAILED")
    {
        Risks = risks;
    }
}
```

### Error Handling Best Practices

```csharp
try
{
    var strategy = await selector.SelectStrategy<int>(context);
    var result = await strategy.ProcessAsync(data);
}
catch (StrategySelectionException ex)
{
    _logger.LogError(ex, "Failed to select strategy: {ErrorCode}", ex.ErrorCode);
    // Handle strategy selection failure
}
catch (NexoException ex)
{
    _logger.LogError(ex, "Nexo error: {ErrorCode}", ex.ErrorCode);
    // Handle general Nexo errors
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error occurred");
    // Handle unexpected errors
}
```

## Configuration

### AppSettings.json

```json
{
  "Nexo": {
    "AI": {
      "Provider": "OpenAI",
      "ApiKey": "your-api-key",
      "Model": "gpt-4",
      "MaxTokens": 4000
    },
    "Safety": {
      "EnableBackups": true,
      "BackupRetentionDays": 30,
      "RequireConfirmation": true
    },
    "Monitoring": {
      "Enabled": true,
      "CollectionIntervalSeconds": 30,
      "RetentionDays": 90
    },
    "Platforms": {
      "Web": {
        "Enabled": true,
        "Optimization": "Performance"
      },
      "Desktop": {
        "Enabled": true,
        "Optimization": "Memory"
      },
      "Mobile": {
        "Enabled": true,
        "Optimization": "Battery"
      }
    }
  }
}
```

### Dependency Injection

```csharp
// Program.cs or Startup.cs
services.AddNexoCore()
    .AddAIServices(options =>
    {
        options.Provider = "OpenAI";
        options.ApiKey = configuration["Nexo:AI:ApiKey"];
    })
    .AddSafetyServices(options =>
    {
        options.EnableBackups = true;
        options.BackupRetentionDays = 30;
    })
    .AddMonitoringServices(options =>
    {
        options.Enabled = true;
        options.CollectionIntervalSeconds = 30;
    })
    .AddBetaTestingServices();
```

## Examples

### Complete Example: Data Processing Pipeline

```csharp
public class DataProcessingExample
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly IAIService _aiService;
    private readonly IUserSafetyService _safetyService;

    public DataProcessingExample(
        IIterationStrategySelector strategySelector,
        IAIService aiService,
        IUserSafetyService safetyService)
    {
        _strategySelector = strategySelector;
        _aiService = aiService;
        _safetyService = safetyService;
    }

    public async Task<ProcessingResult> ProcessDataAsync(List<DataItem> data)
    {
        try
        {
            // 1. Validate operation safety
            var operation = new UserOperation
            {
                Type = OperationType.BulkOperation,
                TargetPath = "/data/processing",
                AffectedFiles = data.Count,
                IsDestructive = false
            };

            var safetyResult = await _safetyService.ValidateOperationAsync(operation);
            if (!safetyResult.IsSafeToProceed)
            {
                throw new SafetyValidationException("Operation not safe", safetyResult.Risks);
            }

            // 2. Select optimal strategy
            var context = new IterationContext
            {
                DataSize = data.Count,
                Requirements = new IterationRequirements
                {
                    PrioritizeCpu = true,
                    RequiresParallelization = data.Count > 1000
                },
                EnvironmentProfile = new EnvironmentProfile
                {
                    CurrentPlatform = PlatformType.Desktop,
                    CpuCores = Environment.ProcessorCount
                }
            };

            var strategy = await _strategySelector.SelectStrategy<DataItem>(context);

            // 3. Process data
            var result = await strategy.ProcessAsync(data);

            // 4. Generate AI insights
            var insights = await _aiService.AnalyzeCodeAsync(
                JsonSerializer.Serialize(result),
                new AnalysisOptions
                {
                    CheckPerformance = true,
                    SuggestOptimizations = true
                }
            );

            return new ProcessingResult
            {
                Data = result,
                Insights = insights,
                StrategyUsed = strategy.StrategyId,
                ProcessingTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process data");
            throw;
        }
    }
}
```

---

This API reference provides comprehensive documentation for all Nexo services. For more examples and tutorials, visit the [Nexo Documentation](https://docs.nexo.dev) or join the [Community](https://community.nexo.dev).
