# Director Studio API Reference

This document provides comprehensive API documentation for Director Studio, including all public interfaces, classes, and methods.

## Table of Contents

- [Core Interfaces](#core-interfaces)
- [Data Transfer Objects](#data-transfer-objects)
- [Adapters](#adapters)
- [Commands](#commands)
- [Validators](#validators)
- [Profiles](#profiles)
- [Orchestration](#orchestration)
- [Policies](#policies)
- [Utilities](#utilities)

## Core Interfaces

### IAdapter

Base interface for all offline adapters.

```csharp
public interface IAdapter
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    bool IsAvailable { get; }
    DateTime LastHealthCheck { get; }
    
    Task<HealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken = default);
    Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
    void Dispose();
}
```

### ICommand<TInput, TOutput>

Generic command interface for all operations.

```csharp
public interface ICommand<TInput, TOutput>
{
    ValueTask<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken);
}
```

### IValidator<T>

Generic validator interface for validation operations.

```csharp
public interface IValidator<in T>
{
    ValueTask<ValidationResult> ValidateAsync(T input, CancellationToken cancellationToken);
}
```

## Data Transfer Objects

### DesignBrief

Represents the natural language input for game slice generation.

```csharp
public sealed record DesignBrief
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Description { get; init; } = "";
    public string GenreHint { get; init; } = "";
    public int TargetDurationMinutes { get; init; } = 10;
    public float DifficultyLevel { get; init; } = 0.5f;
    public string[] Keywords { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
```

### GamePlan

Represents the generated game plan from a design brief.

```csharp
public sealed record GamePlan
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public DesignBrief SourceBrief { get; init; } = new();
    public string Genre { get; init; } = "";
    public string Description { get; init; } = "";
    public string[] CoreMechanics { get; init; } = Array.Empty<string>();
    public string[] PlayerExperience { get; init; } = Array.Empty<string>();
    public int EstimatedDurationMinutes { get; init; }
    public string[] NarrativeBeats { get; init; } = Array.Empty<string>();
    public AssetRequirement[] RequiredAssets { get; init; } = Array.Empty<AssetRequirement>();
    public int Seed { get; init; }
}
```

### WorldLayout

Describes the structural layout of the game world.

```csharp
public sealed record WorldLayout
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public Vector2Int GridSize { get; init; }
    public TileData[] Tiles { get; init; } = Array.Empty<TileData>();
    public SpawnPoint[] SpawnPoints { get; init; } = Array.Empty<SpawnPoint>();
    public Checkpoint[] Checkpoints { get; init; } = Array.Empty<Checkpoint>();
}
```

### InteractionGraph

Represents the graph of interactions within the game slice.

```csharp
public sealed record InteractionGraph
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public InteractionNode[] Nodes { get; init; } = Array.Empty<InteractionNode>();
    public InteractionEdge[] Edges { get; init; } = Array.Empty<InteractionEdge>();
    public Trigger[] Triggers { get; init; } = Array.Empty<Trigger>();
}
```

### ContentBundle

Represents a collection of generated content assets.

```csharp
public sealed record ContentBundle
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public ScriptableObject[] ScriptableObjects { get; init; } = Array.Empty<ScriptableObject>();
    public AudioClip[] AudioClips { get; init; } = Array.Empty<AudioClip>();
    public Texture2D[] Textures { get; init; } = Array.Empty<Texture2D>();
    public GameObject[] Prefabs { get; init; } = Array.Empty<GameObject>();
}
```

## Adapters

### IOllamaAdapter

Interface for Ollama LLM adapter.

```csharp
public interface IOllamaAdapter : IAdapter
{
    Task<GamePlan> PlanAsync(DesignBrief brief, int seed, string genreId, CancellationToken cancellationToken = default);
    Task<GamePlanAnalysis> AnalyzeAsync(GamePlan gamePlan, CancellationToken cancellationToken = default);
    Task<AutoFixSuggestions> SuggestFixesAsync(ValidationReport validationReport, CancellationToken cancellationToken = default);
    Task<GamePlan> EnhanceNarrativeAsync(GamePlan gamePlan, CancellationToken cancellationToken = default);
}
```

### ITextureGenAdapter

Interface for ComfyUI texture generation adapter.

```csharp
public interface ITextureGenAdapter : IAdapter
{
    Task<TextureSet> GeneratePackAsync(string prompt, string style, CancellationToken cancellationToken = default);
    Task<Texture> GenerateTextureAsync(string prompt, int width, int height, string style, CancellationToken cancellationToken = default);
    Task<TextureCollection> GenerateForAssetsAsync(IReadOnlyList<AssetRequirement> requirements, CancellationToken cancellationToken = default);
}
```

### ITtsAdapter

Interface for Piper TTS adapter.

```csharp
public interface ITtsAdapter : IAdapter
{
    Task<AudioClip[]> SynthesizeAsync(string[] lines, string voice, CancellationToken cancellationToken = default);
    Task<AudioClip> SynthesizeLineAsync(string text, string voice, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Voice>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default);
    Task<AudioCollection> GenerateGameAudioAsync(DialogueContent dialogue, CancellationToken cancellationToken = default);
}
```

## Commands

### IPlanGameSliceCommand

Command for generating game plans from design briefs.

```csharp
public interface IPlanGameSliceCommand : ICommand<IPlanGameSliceCommand.Input, GamePlan>
{
    public sealed record Input(DesignBrief DesignBrief, string GenreHint = null);
}
```

### IBuildWorldLayoutCommand

Command for building world layouts from game plans.

```csharp
public interface IBuildWorldLayoutCommand : ICommand<IBuildWorldLayoutCommand.Input, WorldLayout>
{
    public sealed record Input(GamePlan GamePlan, WorldLayoutOptions Options = null);
}
```

### IPlaceInteractionsCommand

Command for placing interactions in the world.

```csharp
public interface IPlaceInteractionsCommand : ICommand<IPlaceInteractionsCommand.Input, InteractionGraph>
{
    public sealed record Input(WorldLayout WorldLayout, GamePlan GamePlan);
}
```

### ICreateContentBundleCommand

Command for creating content bundles.

```csharp
public interface ICreateContentBundleCommand : ICommand<ICreateContentBundleCommand.Input, ContentBundle>
{
    public sealed record Input(InteractionGraph InteractionGraph, GamePlan GamePlan);
}
```

## Validators

### PlayabilityValidator

Validates that game slices are playable and completable.

```csharp
public class PlayabilityValidator : IValidator<GamePlan>
{
    public ValueTask<ValidationResult> ValidateAsync(GamePlan input, CancellationToken cancellationToken)
    {
        // Implementation details...
    }
}
```

### MechanicsValidator

Validates genre-specific mechanics and affordances.

```csharp
public class MechanicsValidator : IValidator<GamePlan>
{
    public ValueTask<ValidationResult> ValidateAsync(GamePlan input, CancellationToken cancellationToken)
    {
        // Implementation details...
    }
}
```

### ValidationReport

Aggregates validation results from multiple validators.

```csharp
public class ValidationReport
{
    public string ReportId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public List<ValidationResult> Results { get; } = new List<ValidationResult>();
    
    public ValidationStatus OverallStatus { get; }
    public void AddResult(ValidationResult result);
    public string GetSummary();
}
```

## Profiles

### IGenreProfile

Interface for genre-specific profiles.

```csharp
public interface IGenreProfile
{
    string GenreId { get; }
    string GenreName { get; }
    string Description { get; }
    List<string> Keywords { get; }
    
    PerformanceBudget PerformanceBudget { get; }
    PacingConfig PacingConfig { get; }
    AccessibilityDefaults AccessibilityDefaults { get; }
    DifficultyProgression DifficultyProgression { get; }
    List<RequiredAsset> CoreAssetRequirements { get; }
    
    bool ValidateMechanic(string mechanic);
    bool ValidateAsset(RequiredAsset asset);
    float CalculatePacingScore(GamePlan gamePlan);
    ValidationResult ValidateGenreSpecifics(GamePlan gamePlan);
}
```

### GenreRegistry

Manages genre profile registration and retrieval.

```csharp
public class GenreRegistry
{
    public GenreRegistry(IEnumerable<IGenreProfile> profiles, ILogger<GenreRegistry> logger);
    
    public IGenreProfile GetProfile(string genreId);
    public IReadOnlyCollection<IGenreProfile> GetAllProfiles();
    public IGenreProfile DetectGenre(DesignBrief brief);
}
```

## Orchestration

### DirectorStudioService

Main service composition and dependency injection container.

```csharp
public class DirectorStudioService : IDisposable
{
    public DirectorStudioService();
    
    public T GetService<T>() where T : class;
    public void Dispose();
}
```

## Policies

### FileTransaction

Implements staging and promotion workflow for asset generation.

```csharp
public class FileTransaction
{
    public FileTransaction(string stagingPath, string targetPath);
    
    public Task<string> StageFileAsync(string content, string fileName, CancellationToken cancellationToken);
    public Task PromoteAsync(CancellationToken cancellationToken);
    public Task RollbackAsync(CancellationToken cancellationToken);
    public void Dispose();
}
```

## Utilities

### JsonRepair

Utility for repairing malformed JSON responses from AI adapters.

```csharp
public static class JsonRepair
{
    public static JsonRepairResult Repair(string json);
    public static JsonValidationResult Validate(string json);
    public static JsonRepairResult RepairWithSchema(string json, JsonSchema expectedSchema);
}
```

### HealthCheckResult

Result of adapter health check operations.

```csharp
public sealed record HealthCheckResult
{
    public bool IsHealthy { get; init; }
    public string Message { get; init; } = "";
    public long ResponseTimeMs { get; init; }
    public string Details { get; init; } = "";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### ValidationResult

Result of validation operations.

```csharp
public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public string ValidatorName { get; init; } = "";
    public string Message { get; init; } = "";
    public List<ValidationIssue> Issues { get; init; } = new();
    public List<ValidationSuggestion> Suggestions { get; init; } = new();
}
```

## Error Handling

### Common Exceptions

- `InvalidOperationException`: Thrown when operations are attempted on unavailable adapters
- `ArgumentException`: Thrown when invalid arguments are provided
- `OperationCanceledException`: Thrown when operations are cancelled
- `UnauthorizedAccessException`: Thrown when attempting to write outside allowed paths

### Error Recovery

All adapters implement graceful error recovery:
- Health checks before operations
- Fallback to stub implementations when adapters are unavailable
- Detailed error messages with suggested fixes
- Automatic retry logic with exponential backoff

## Performance Considerations

### Memory Management

- All adapters implement `IDisposable` for proper resource cleanup
- Large objects are disposed immediately after use
- Streaming is used for large file operations

### Async Operations

- All I/O operations are asynchronous
- Cancellation tokens are supported throughout
- Long-running operations can be cancelled gracefully

### Caching

- Health check results are cached for a short period
- Generated assets are cached to avoid regeneration
- Validation results are cached until inputs change

## Security

### Path Constraints

- All generated assets must be under `Assets/Generated/**`
- Staging operations use temporary directories
- Atomic promotion prevents partial writes

### Resource Limits

- Maximum 200MB per generation run
- Timeout limits on all operations
- Memory limits on large operations

### Audit Logging

- All operations are logged with timestamps
- Seeds and versions are tracked for reproducibility
- Security events are logged for monitoring

## Testing

### Test Categories

- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end workflow testing
- **Smoke Tests**: Basic functionality verification
- **PlayMode Tests**: Unity-specific testing
- **Headless Tests**: CI environment testing

### Test Coverage

- Minimum 80% code coverage required
- All public APIs must have tests
- Edge cases and error conditions are tested
- Performance characteristics are validated

## Examples

### Basic Usage

```csharp
// Get the service
var service = new DirectorStudioServiceUnified();

// Create a design brief
var brief = new DesignBrief
{
    Description = "A simple platformer level with a few jumps and a single enemy.",
    GenreHint = "Platformer",
    TargetDurationMinutes = 5
};

// Generate a game plan
var planCommand = service.GetService<IPlanGameSliceCommand>();
var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

// Validate the plan
var validators = service.GetService<IEnumerable<IValidator<GamePlan>>>();
var report = new ValidationReport();
foreach (var validator in validators)
{
    var result = await validator.ValidateAsync(gamePlan, CancellationToken.None);
    report.AddResult(result);
}

// Check if plan is valid
if (report.OverallStatus == ValidationStatus.Pass)
{
    Console.WriteLine("Game plan is valid!");
}
else
{
    Console.WriteLine($"Game plan has issues: {report.GetSummary()}");
}
```

### Adapter Health Check

```csharp
// Check adapter health
var ollamaAdapter = service.GetService<IOllamaAdapter>();
var healthResult = await ollamaAdapter.HealthCheckAsync(CancellationToken.None);

if (healthResult.IsHealthy)
{
    Console.WriteLine($"Ollama is healthy: {healthResult.Message}");
}
else
{
    Console.WriteLine($"Ollama is unhealthy: {healthResult.Message}");
}
```

### JSON Repair

```csharp
// Repair malformed JSON
var malformedJson = @"{""name"": ""test"", ""value"": 123,}";
var repairResult = JsonRepair.Repair(malformedJson);

if (repairResult.IsSuccessful)
{
    Console.WriteLine($"Repaired JSON: {repairResult.RepairedJson}");
}
else
{
    Console.WriteLine($"Repair failed: {repairResult.ErrorMessage}");
}
```

This API reference provides comprehensive documentation for all public interfaces and classes in Director Studio. For more detailed examples and usage patterns, see the [Examples](examples/) directory.
