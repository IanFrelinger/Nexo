using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Generates new brick manifests from observed patterns.
/// Infers interface from pattern metadata and optionally from adaptation history.
/// </summary>
public sealed class NewBrickGenerator : INewBrickGenerator
{
    private readonly IAdaptationLog? _adaptationLog;

    private static readonly Dictionary<string, BrickCategory> PatternCategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EmptyCatch"] = BrickCategory.Validation,
        ["repeated-edits"] = BrickCategory.Control,
        ["edit-then-build"] = BrickCategory.Control,
        ["MissingOutput"] = BrickCategory.Validation,
        ["code-analysis"] = BrickCategory.Analysis,
    };

    public NewBrickGenerator(IAdaptationLog? adaptationLog = null)
    {
        _adaptationLog = adaptationLog;
    }

    /// <inheritdoc />
    public async Task<BrickManifest> GenerateAsync(
        string patternType,
        IReadOnlyDictionary<string, object>? patternMetadata = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var category = PatternCategoryMap.TryGetValue(patternType, out var c) ? c : BrickCategory.Control;
        var inputs = InferInputs(patternType, patternMetadata);
        var outputs = InferOutputs(patternType, patternMetadata);

        var similarBrickId = (string?)null;
        if (_adaptationLog != null)
        {
            var since = DateTimeOffset.UtcNow.AddDays(-7);
            var records = await _adaptationLog.QueryAsync(since, null, null);
            var match = records.FirstOrDefault(r =>
                string.Equals(r.FailureType, patternType, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                similarBrickId = match.BrickId;
        }

        var baseId = similarBrickId ?? patternType.ToLowerInvariant().Replace(".", "-");
        var manifest = new BrickManifest
        {
            Id = $"generated.{baseId}.{Guid.NewGuid():N}",
            Name = $"Generated from {patternType}",
            Version = "1.0.0",
            Category = category,
            Description = $"Brick for pattern type '{patternType}'" + (similarBrickId != null ? $" (similar to {similarBrickId})" : ""),
            Interface = new BrickInterface { Inputs = inputs, Outputs = outputs },
            ImplementationSource = null,
        };

        return manifest;
    }

    private static List<BrickInputDefinition> InferInputs(string patternType, IReadOnlyDictionary<string, object>? metadata)
    {
        var inputs = new List<BrickInputDefinition>();
        if (metadata != null)
        {
            if (metadata.ContainsKey("filePath") || metadata.ContainsKey("path"))
                inputs.Add(new BrickInputDefinition("path", "string", "File or directory path"));
            if (metadata.ContainsKey("paths") || metadata.ContainsKey("files"))
                inputs.Add(new BrickInputDefinition("paths", "string[]", "Paths to process"));
            if (metadata.ContainsKey("failureType") || metadata.ContainsKey("fixType"))
                inputs.Add(new BrickInputDefinition("failureType", "string", "Type of failure to fix"));
        }
        if (inputs.Count == 0)
            inputs.Add(new BrickInputDefinition("input", "object", "Input data"));
        return inputs;
    }

    private static List<BrickOutputDefinition> InferOutputs(string patternType, IReadOnlyDictionary<string, object>? metadata)
    {
        return
        [
            new BrickOutputDefinition("result", "object", "Result of processing"),
            new BrickOutputDefinition("fixed", "bool", "Whether fix was applied"),
        ];
    }
}
