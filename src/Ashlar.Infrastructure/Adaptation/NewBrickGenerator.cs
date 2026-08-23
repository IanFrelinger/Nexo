using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Infrastructure.Adaptation.Generation;

namespace Ashlar.Infrastructure.Adaptation;

/// <summary>
/// Generates brick manifests from observed patterns or arbitrary intent via <see cref="IGeneratorModel"/>.
/// Does NOT author witness specs — witness is an independent input at certification time.
/// </summary>
public sealed class NewBrickGenerator : INewBrickGenerator
{
    private readonly IGeneratorModel _model;
    private readonly IAdaptationLog? _adaptationLog;

    private static readonly Dictionary<string, BrickCategory> PatternCategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EmptyCatch"] = BrickCategory.Validation,
        ["repeated-edits"] = BrickCategory.Control,
        ["edit-then-build"] = BrickCategory.Control,
        ["MissingOutput"] = BrickCategory.Validation,
        ["code-analysis"] = BrickCategory.Analysis,
        ["ErrorSummaryExtractor"] = BrickCategory.Analysis,
        [FixtureGeneratorModel.LineSubstringCounterIntentId] = BrickCategory.Analysis,
    };

    /// <summary>Initializes a new new brick generator.</summary>
    public NewBrickGenerator(IGeneratorModel? model = null, IAdaptationLog? adaptationLog = null)
    {
        _model = model ?? new FixtureGeneratorModel();
        _adaptationLog = adaptationLog;
    }

    /// <inheritdoc />
    public async Task<BrickManifest> GenerateAsync(
        string patternType,
        IReadOnlyDictionary<string, object>? patternMetadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(patternType, "ErrorSummaryExtractor", StringComparison.OrdinalIgnoreCase))
        {
            return await GenerateFromIntentAsync(
                new IntentSpec(
                    "error-summary-extractor",
                    "Counts ERROR lines in a raw log string and returns the first error message.",
                    BrickId: "error-summary-extractor",
                    Name: "Error Summary Extractor"),
                new WitnessSignature(
                    "error-summary-extractor",
                    [new WitnessIoField("logText", "string")],
                    [
                        new WitnessIoField("errorCount", "int"),
                        new WitnessIoField("firstErrorMessage", "string")
                    ]),
                cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var category = PatternCategoryMap.TryGetValue(patternType, out var c) ? c : BrickCategory.Control;
        var inputs = InferInputs(patternType, patternMetadata);
        var outputs = InferOutputs(patternType, patternMetadata);

        var similarBrickId = (string?)null;
        if (_adaptationLog != null)
        {
            var since = DateTimeOffset.UtcNow.AddDays(-7);
            var records = await _adaptationLog.QueryAsync(since, null, null).ConfigureAwait(false);
            var match = records.FirstOrDefault(r =>
                string.Equals(r.FailureType, patternType, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                similarBrickId = match.BrickId;
        }

        var baseId = similarBrickId ?? patternType.ToLowerInvariant().Replace(".", "-");
        return new BrickManifest
        {
            Id = $"generated.{baseId}.{Guid.NewGuid():N}",
            Name = $"Generated from {patternType}",
            Version = "1.0.0",
            Category = category,
            Description = $"DomainBrick for pattern type '{patternType}'" + (similarBrickId != null ? $" (similar to {similarBrickId})" : ""),
            Interface = new BrickInterface { Inputs = inputs, Outputs = outputs },
            ImplementationSource = null,
        };
    }

    /// <inheritdoc />
    public async Task<BrickManifest> GenerateFromIntentAsync(
        IntentSpec intent,
        WitnessSignature witnessSignature,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var generated = await _model.GenerateAsync(intent, witnessSignature, cancellationToken).ConfigureAwait(false);
        var category = PatternCategoryMap.TryGetValue(intent.IntentId, out var c) ? c : BrickCategory.Analysis;

        return new BrickManifest
        {
            Id = witnessSignature.BrickId,
            Name = intent.Name ?? intent.Description,
            Version = "1.0.0",
            Category = category,
            Description = intent.Description,
            Interface = new BrickInterface
            {
                Inputs = witnessSignature.Inputs
                    .Select(i => new BrickInputDefinition(i.Name, i.Type, i.Name))
                    .ToList(),
                Outputs = witnessSignature.Outputs
                    .Select(o => new BrickOutputDefinition(o.Name, o.Type, o.Name))
                    .ToList()
            },
            ImplementationSource = generated.SourceCode,
            GenerationProvenance = generated.Provenance,
            GeneratedClassName = generated.ClassName,
            GeneratedNamespace = generated.Namespace,
        };
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
