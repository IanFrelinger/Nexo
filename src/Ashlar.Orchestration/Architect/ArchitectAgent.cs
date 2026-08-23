using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Abstractions.Barriers;
using Ashlar.Abstractions.Routing;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Architect.Parsers;
using Ashlar.Orchestration.Architect.Prompts;
using Ashlar.Orchestration.Validation;

namespace Ashlar.Orchestration.Architect;

/// <summary>
/// Architect Agent that decomposes requests into validated agent specifications.
/// 
/// Implementation of IArchitectAgent that:
/// - Uses LLM (IModel) to decompose requests
/// - Retrieves similar examples for context
/// - Recognizes domains from request text
/// - Validates decomposition results
/// - Self-corrects invalid decompositions (up to MaxCorrectionAttempts)
/// - Builds structured prompts for LLM
/// - Parses JSON responses into AgentSpawnSpec objects
/// 
/// The first agent in the orchestration flow, responsible for breaking down
/// user requests into executable agent specifications.
/// </summary>
public sealed class ArchitectAgent : IArchitectAgent
{
    private readonly IModel _model;
    private readonly DecompositionRetriever _retriever;
    private readonly DomainRecognizer _domainRecognizer;
    private readonly IReadOnlyList<IValidator> _validators;
    private readonly DecompositionPromptBuilder _promptBuilder;
    private readonly DecompositionJsonParser _parser;
    private readonly IEndpointRouter _router;
    private readonly ILogger<ArchitectAgent> _logger;
    private const int MaxCorrectionAttempts = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchitectAgent"/> class.
    /// </summary>
    /// <param name="model">The LLM model for decomposition.</param>
    /// <param name="retriever">The retriever for similar decomposition examples.</param>
    /// <param name="domainRecognizer">The domain recognizer for extracting domain hints.</param>
    /// <param name="validators">The validators for validating decomposition results.</param>
    /// <param name="promptBuilder">The prompt builder for constructing LLM prompts.</param>
    /// <param name="parser">The JSON parser for parsing decomposition results.</param>
    /// <param name="router">Endpoint router for assigning per-agent target endpoints.</param>
    /// <param name="logger">The logger instance.</param>
    public ArchitectAgent(
        IModel model,
        DecompositionRetriever retriever,
        DomainRecognizer domainRecognizer,
        IEnumerable<IValidator> validators,
        DecompositionPromptBuilder promptBuilder,
        DecompositionJsonParser parser,
        IEndpointRouter router,
        ILogger<ArchitectAgent> logger)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _domainRecognizer = domainRecognizer ?? throw new ArgumentNullException(nameof(domainRecognizer));
        _validators = validators?.ToList() ?? throw new ArgumentNullException(nameof(validators));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Decomposes a request into agent specifications.
    /// 
    /// Convenience overload that creates context automatically by retrieving similar examples
    /// and recognizing domains from the request.
    /// </summary>
    /// <param name="request">The user request to decompose.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="DecompositionResult"/> containing agent specifications.</returns>
    public async Task<DecompositionResult> DecomposeAsync(string request, CancellationToken cancellationToken = default)
    {
        return await DecomposeAsync(request, null, cancellationToken);
    }

    /// <summary>
    /// Decomposes a request into agent specifications with optional context.
    /// 
    /// Process:
    /// 1. Retrieves similar examples and recognizes domains (if context not provided)
    /// 2. Builds prompts for LLM
    /// 3. Calls LLM to generate decomposition
    /// 4. Parses JSON response
    /// 5. Validates decomposition using all validators
    /// 6. Self-corrects if validation fails (up to MaxCorrectionAttempts)
    /// 
    /// Returns the decomposition result, which may contain validation errors if self-correction fails.
    /// </summary>
    /// <param name="request">The user request to decompose.</param>
    /// <param name="context">Optional decomposition context with similar examples and domain hints.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="DecompositionResult"/> containing agent specifications and validation errors.</returns>
    /// <exception cref="ArgumentException">Thrown if request is null or empty.</exception>
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
                return await ApplyRoutingAsync(result, context, cancellationToken);
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

        var finalResult = result ?? new DecompositionResult
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

        return await ApplyRoutingAsync(finalResult, context, cancellationToken);
    }

    private async Task<DecompositionResult> ApplyRoutingAsync(
        DecompositionResult result,
        DecompositionContext context,
        CancellationToken cancellationToken)
    {
        if (result.Agents.Count == 0)
        {
            return result;
        }

        var barrierLevel = string.IsNullOrWhiteSpace(context.BarrierLevel)
            ? string.Empty
            : context.BarrierLevel;

        var routedAgents = new List<AgentSpawnSpec>(result.Agents.Count);
        foreach (var spec in result.Agents)
        {
            var agentName = string.IsNullOrWhiteSpace(spec.Name) ? spec.AgentId : spec.Name;
            var routingContext = new EndpointRoutingContext(
                AgentName: agentName,
                CorrelationId: context.CorrelationId,
                RequiredCapabilities: spec.RequiredCapabilities,
                BarrierLevel: barrierLevel,
                PreferredRegion: context.PreferredRegion,
                AgentMetadata: spec.Metadata);

            var targetEndpoint = await _router.ResolveAsync(routingContext, cancellationToken);
            routedAgents.Add(spec with
            {
                Name = agentName,
                TargetEndpoint = targetEndpoint
            });
        }

        return result with { Agents = routedAgents };
    }
}

