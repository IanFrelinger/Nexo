using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Architect.Parsers;
using Nexo.Orchestration.Architect.Prompts;
using Nexo.Orchestration.Validation;

namespace Nexo.Orchestration.Architect;

/// <summary>
/// Architect Agent that decomposes requests into validated agent specifications.
/// </summary>
public sealed class ArchitectAgent : IArchitectAgent
{
    private readonly IModel _model;
    private readonly DecompositionRetriever _retriever;
    private readonly DomainRecognizer _domainRecognizer;
    private readonly IReadOnlyList<IValidator> _validators;
    private readonly DecompositionPromptBuilder _promptBuilder;
    private readonly DecompositionJsonParser _parser;
    private readonly ILogger<ArchitectAgent> _logger;
    private const int MaxCorrectionAttempts = 3;

    public ArchitectAgent(
        IModel model,
        DecompositionRetriever retriever,
        DomainRecognizer domainRecognizer,
        IEnumerable<IValidator> validators,
        DecompositionPromptBuilder promptBuilder,
        DecompositionJsonParser parser,
        ILogger<ArchitectAgent> logger)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _domainRecognizer = domainRecognizer ?? throw new ArgumentNullException(nameof(domainRecognizer));
        _validators = validators?.ToList() ?? throw new ArgumentNullException(nameof(validators));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DecompositionResult> DecomposeAsync(string request, CancellationToken cancellationToken = default)
    {
        return await DecomposeAsync(request, null, cancellationToken);
    }

    public async Task<DecompositionResult> DecomposeAsync(
        string request,
        DecompositionContext? context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            throw new ArgumentException("Request cannot be null or empty", nameof(request));
        }

        _logger.LogInformation("Decomposing request: {Request}", request);

        // Retrieve similar examples if context not provided
        if (context == null)
        {
            var similarExamples = await _retriever.RetrieveSimilarAsync(request, maxResults: 5, cancellationToken);
            var domainHints = _domainRecognizer.RecognizeDomains(request);

            context = new DecompositionContext
            {
                SimilarExamples = similarExamples.Select(e => e.Result).ToList(),
                DomainHints = domainHints
            };
        }

        // Build initial prompt
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildUserPrompt(request, context);

        // Attempt decomposition with self-correction loop
        DecompositionResult? result = null;
        var attempts = 0;

        while (attempts < MaxCorrectionAttempts)
        {
            attempts++;
            _logger.LogDebug("Decomposition attempt {Attempt}/{MaxAttempts}", attempts, MaxCorrectionAttempts);

            // Call model
            var modelInput = new ModelInput(new[]
            {
                ("system", systemPrompt),
                ("user", userPrompt)
            });

            var modelOutput = await _model.CompleteAsync(modelInput, cancellationToken);

            // Parse result
            result = _parser.Parse(modelOutput.Text, request);
            if (result == null)
            {
                _logger.LogWarning("Failed to parse decomposition on attempt {Attempt}", attempts);
                if (attempts < MaxCorrectionAttempts)
                {
                    userPrompt = _promptBuilder.BuildCorrectionPrompt(
                        request,
                        new DecompositionResult
                        {
                            Agents = Array.Empty<AgentSpawnSpec>(),
                            OriginalRequest = request,
                            Reasoning = "Parse error",
                            ValidationErrors = new[]
                            {
                                new ValidationError
                                {
                                    ErrorType = "Parse",
                                    Message = "Failed to parse JSON from model output",
                                    Severity = ValidationSeverity.Error
                                }
                            }
                        },
                        new[] { new ValidationError { ErrorType = "Parse", Message = "Invalid JSON format", Severity = ValidationSeverity.Error } });
                }
                continue;
            }

            // Validate result
            var allErrors = new List<ValidationError>();
            foreach (var validator in _validators)
            {
                var errors = await validator.ValidateAsync(result, cancellationToken);
                allErrors.AddRange(errors);
            }

            result = result with { ValidationErrors = allErrors };

            // If valid, return
            if (result.IsValid)
            {
                _logger.LogInformation("Decomposition successful after {Attempts} attempt(s)", attempts);
                return result;
            }

            // If invalid and we have more attempts, build correction prompt
            if (attempts < MaxCorrectionAttempts)
            {
                _logger.LogWarning("Decomposition validation failed with {ErrorCount} errors, attempting correction", allErrors.Count);
                userPrompt = _promptBuilder.BuildCorrectionPrompt(request, result, allErrors);
            }
        }

        // Return result even if invalid (after max attempts)
        _logger.LogWarning("Decomposition completed with {ErrorCount} validation errors after {Attempts} attempts",
            result?.ValidationErrors.Count ?? 0, attempts);

        return result ?? new DecompositionResult
        {
            Agents = Array.Empty<AgentSpawnSpec>(),
            OriginalRequest = request,
            Reasoning = "Failed to generate valid decomposition",
            ValidationErrors = new[]
            {
                new ValidationError
                {
                    ErrorType = "System",
                    Message = "Failed to generate valid decomposition after maximum attempts",
                    Severity = ValidationSeverity.Error
                }
            }
        };
    }
}

