using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Assets.Models;
using Nexo.Orchestration.Assets.Ports;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.Orchestration.Agents.Assets;

/// <summary>
/// Base class for agents that generate binary assets (images, audio, 3D models).
/// 
/// Provides common functionality for asset generation:
/// - Prompt generation using LLM
/// - Asset generation via provider adapters
/// - Asset validation against constraints
/// - Retry logic with prompt refinement
/// - Asset storage and metadata management
/// 
/// Derived classes (ImageAssetAgent, AudioAssetAgent, Model3DAssetAgent) implement
/// asset-type-specific generation and validation logic.
/// </summary>
public abstract class BaseAssetAgent : BaseAgent
{
    protected readonly IModel? Model;
    protected readonly IAssetStorage AssetStorage;

    public abstract AssetType AssetType { get; }

    protected BaseAssetAgent(
        AgentSpawnSpec spec,
        IModel? model,
        IAssetStorage assetStorage,
        ILogger<BaseAgent> logger) : base(spec, logger)
    {
        Model = model;
        AssetStorage = assetStorage ?? throw new ArgumentNullException(nameof(assetStorage));
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing {AssetType} asset agent: {AgentId}", AssetType, Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("Dependencies resolved for {AgentId}: {Count} dependencies", Spec.AgentId, dependencyOutputs.Count);
        return Task.CompletedTask;
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Shutting down {AssetType} asset agent: {AgentId}", AssetType, Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        const int MaxRetries = 2;
        var retryCount = 0;

        // Step 1: Generate asset prompt from spec using LLM
        var prompt = await GenerateAssetPromptAsync(Spec, dependencyOutputs ?? new Dictionary<string, object>(), cancellationToken);
        Logger.LogInformation("Generated asset prompt: {Prompt}", prompt.TextPrompt);

        GeneratedAssetBase asset;
        AssetValidation validation;

        do
        {
            // Step 2: Generate the asset using provider
            asset = await GenerateAssetAsync(prompt, cancellationToken);
            Logger.LogInformation("Generated asset at: {Path}", asset.FilePath);

            // Step 3: Validate asset against constraints
            validation = await ValidateAssetAsync(asset, Spec.Constraints, cancellationToken);

            if (!validation.IsValid)
            {
                Logger.LogWarning("Asset validation failed (attempt {Attempt}): {Failures}",
                    retryCount + 1, string.Join(", ", validation.FailedChecks));

                if (retryCount < MaxRetries && await ShouldRetryAsync(validation, cancellationToken))
                {
                    prompt = await RefinePromptAsync(prompt, validation, cancellationToken);
                    retryCount++;
                }
                else
                {
                    break; // Exit retry loop
                }
            }
        } while (!validation.IsValid && retryCount <= MaxRetries);

        // Step 4: Store asset and return output
        var storedPath = await AssetStorage.StoreAsync(asset.FilePath, Spec.AgentId, cancellationToken);

        var fileInfo = new FileInfo(asset.FilePath);
        return new AssetOutput
        {
            AssetId = Guid.NewGuid().ToString(),
            AssetType = AssetType,
            FilePath = storedPath,
            MimeType = GetMimeType(asset.FilePath),
            Prompt = prompt,
            Validation = validation,
            FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            Metadata = new Dictionary<string, object>
            {
                ["retryCount"] = retryCount
            }
        };
    }

    /// <summary>
    /// Generates a prompt for asset creation based on spec and dependencies.
    /// </summary>
    protected virtual async Task<GenerationPrompt> GenerateAssetPromptAsync(
        AgentSpawnSpec spec,
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        if (Model == null)
        {
            // Fallback: use spec description directly
            return new GenerationPrompt
            {
                TextPrompt = $"{spec.Goal}. {spec.Description}",
                Parameters = ExtractParametersFromConstraints(spec.Constraints)
            };
        }

        var systemPrompt = GetPromptGenerationSystemPrompt();
        var userPrompt = BuildPromptGenerationRequest(spec, dependencyOutputs);

        var modelInput = new ModelInput(new[]
        {
            ("system", systemPrompt),
            ("user", userPrompt)
        });

        var output = await Model.CompleteAsync(modelInput, cancellationToken);
        return ParseGenerationPrompt(output.Text);
    }

    /// <summary>
    /// Generates the actual asset using the appropriate provider.
    /// </summary>
    protected abstract Task<GeneratedAssetBase> GenerateAssetAsync(
        GenerationPrompt prompt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates the generated asset against spec constraints.
    /// </summary>
    protected virtual Task<AssetValidation> ValidateAssetAsync(
        GeneratedAssetBase asset,
        IReadOnlyList<AgentConstraint> constraints,
        CancellationToken cancellationToken)
    {
        var passed = new List<string>();
        var failed = new List<string>();
        var warnings = new List<string>();

        foreach (var constraint in constraints)
        {
            var result = EvaluateConstraint(asset, constraint);
            switch (result)
            {
                case ConstraintResult.Passed:
                    passed.Add(constraint.Description);
                    break;
                case ConstraintResult.Failed:
                    failed.Add(constraint.Description);
                    break;
                case ConstraintResult.Warning:
                    warnings.Add(constraint.Description);
                    break;
            }
        }

        return Task.FromResult(new AssetValidation
        {
            IsValid = failed.Count == 0,
            PassedChecks = passed,
            FailedChecks = failed,
            Warnings = warnings
        });
    }

    protected abstract string GetPromptGenerationSystemPrompt();
    protected abstract ConstraintResult EvaluateConstraint(GeneratedAssetBase asset, AgentConstraint constraint);
    protected abstract string GetMimeType(string filePath);

    private Task<bool> ShouldRetryAsync(AssetValidation validation, CancellationToken ct)
    {
        // Don't retry if no failures
        if (!validation.FailedChecks.Any())
        {
            return Task.FromResult(false);
        }

        // Check if failures are recoverable (prompt-correctable issues)
        var recoverablePatterns = new[]
        {
            "style", "mood", "color", "composition", "framing",
            "lighting", "detail", "quality", "resolution mismatch"
        };

        var unrecoverablePatterns = new[]
        {
            "format not supported", "size exceeds", "invalid file",
            "api error", "quota exceeded", "authentication"
        };

        foreach (var failure in validation.FailedChecks)
        {
            var lowerFailure = failure.ToLowerInvariant();

            // If any unrecoverable pattern matches, don't retry
            if (unrecoverablePatterns.Any(p => lowerFailure.Contains(p)))
            {
                Logger.LogDebug("Unrecoverable failure detected: {Failure}", failure);
                return Task.FromResult(false);
            }
        }

        // Retry if all failures appear recoverable and count is reasonable
        var allRecoverable = validation.FailedChecks.All(f =>
            recoverablePatterns.Any(p => f.ToLowerInvariant().Contains(p)));

        // Allow retry if failures are recoverable OR if there are few enough to try
        var shouldRetry = allRecoverable || validation.FailedChecks.Count <= 2;

        Logger.LogDebug("Retry decision for {Count} failures: {ShouldRetry}", 
            validation.FailedChecks.Count, shouldRetry);

        return Task.FromResult(shouldRetry);
    }

    private async Task<GenerationPrompt> RefinePromptAsync(
        GenerationPrompt original,
        AssetValidation validation,
        CancellationToken ct)
    {
        if (Model == null) return original;

        var refinementPrompt = $"""
            The generated asset failed these checks: {string.Join(", ", validation.FailedChecks)}

            Original prompt: {original.TextPrompt}

            Generate a refined prompt that addresses these failures.
            """;

        var output = await Model.CompleteAsync(new ModelInput(new[]
        {
            ("system", "You refine asset generation prompts based on validation feedback."),
            ("user", refinementPrompt)
        }), ct);

        return new GenerationPrompt
        {
            TextPrompt = output.Text.Trim(),
            NegativePrompt = original.NegativePrompt,
            Parameters = original.Parameters
        };
    }

    private GenerationPrompt ParseGenerationPrompt(string output)
    {
        // Try to parse as JSON, fallback to plain text
        try
        {
            var doc = JsonDocument.Parse(output);
            return new GenerationPrompt
            {
                TextPrompt = doc.RootElement.GetProperty("prompt").GetString() ?? output,
                NegativePrompt = doc.RootElement.TryGetProperty("negativePrompt", out var neg)
                    ? neg.GetString() : null,
                StyleReference = doc.RootElement.TryGetProperty("style", out var style)
                    ? style.GetString() : null
            };
        }
        catch
        {
            return new GenerationPrompt { TextPrompt = output.Trim() };
        }
    }

    private IReadOnlyDictionary<string, object> ExtractParametersFromConstraints(
        IReadOnlyList<AgentConstraint> constraints)
    {
        var parameters = new Dictionary<string, object>();

        foreach (var constraint in constraints)
        {
            // Extract numeric parameters from constraint descriptions
            // e.g., "resolution must be 1024x1024" → { "width": 1024, "height": 1024 }
            var desc = constraint.Description.ToLowerInvariant();

            if (desc.Contains("resolution") || desc.Contains("size"))
            {
                var match = Regex.Match(desc, @"(\d+)\s*x\s*(\d+)");
                if (match.Success)
                {
                    parameters["width"] = int.Parse(match.Groups[1].Value);
                    parameters["height"] = int.Parse(match.Groups[2].Value);
                }
            }
        }

        return parameters;
    }

    private string BuildPromptGenerationRequest(
        AgentSpawnSpec spec,
        IReadOnlyDictionary<string, object> dependencyOutputs)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Asset Type: {AssetType}");
        sb.AppendLine($"## Goal: {spec.Goal}");
        sb.AppendLine($"## Description: {spec.Description}");

        if (spec.Constraints.Any())
        {
            sb.AppendLine("## Constraints:");
            foreach (var c in spec.Constraints)
            {
                sb.AppendLine($"- {c.Description}");
            }
        }

        if (dependencyOutputs.Any())
        {
            sb.AppendLine("## Context from other agents:");
            foreach (var (key, value) in dependencyOutputs)
            {
                sb.AppendLine($"- {key}: {JsonSerializer.Serialize(value)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Generate a detailed prompt for this asset. Include style, mood, technical requirements.");

        return sb.ToString();
    }
}

/// <summary>
/// Base record for generated assets.
/// </summary>
public abstract record GeneratedAssetBase
{
    public required string FilePath { get; init; }
}

/// <summary>
/// Result of constraint evaluation.
/// </summary>
public enum ConstraintResult
{
    Passed,
    Failed,
    Warning
}

