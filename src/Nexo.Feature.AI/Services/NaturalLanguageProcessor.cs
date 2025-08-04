using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Processes natural language feature requirements and converts them into structured data.
    /// </summary>
    public class NaturalLanguageProcessor : INaturalLanguageProcessor
    {
        private readonly ILogger<NaturalLanguageProcessor> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly Dictionary<string, List<string>> _domainTerminology;

        public NaturalLanguageProcessor(
            ILogger<NaturalLanguageProcessor> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            
            _domainTerminology = InitializeDomainTerminology();
        }

        /// <summary>
        /// Processes natural language input and extracts feature requirements.
        /// </summary>
        public async Task<FeatureRequirementResult> ProcessRequirementsAsync(string input, ProcessingContext context)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(input))
                return new FeatureRequirementResult
                {
                    IsSuccess = false,
                    Errors = new List<string> { "Input is empty." },
                    Metadata = new ProcessingMetadata
                    {
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDurationMs = 0,
                        Domain = context.Domain,
                        ProcessingModel = "NaturalLanguageProcessor",
                        Version = "1.0.0"
                    }
                };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Starting natural language processing for domain: {Domain}", context.Domain);

            try
            {
                // Detect input format
                var inputFormat = DetectInputFormat(input);
                _logger.LogDebug("Detected input format: {Format}", inputFormat);

                // Pre-process input
                var processedInput = await PreprocessInputAsync(input, context);
                
                // Extract requirements using AI model
                var requirements = await ExtractRequirementsWithAIAsync(processedInput, context, inputFormat);
                
                // Check if requirements were extracted successfully
                if (requirements == null || !requirements.Any())
                {
                    stopwatch.Stop();
                    return new FeatureRequirementResult
                    {
                        IsSuccess = false,
                        Errors = new List<string> { "Failed to extract requirements from input." },
                        Metadata = new ProcessingMetadata
                        {
                            ProcessedAt = DateTime.UtcNow,
                            ProcessingDurationMs = stopwatch.ElapsedMilliseconds,
                            InputFormat = inputFormat,
                            Domain = context.Domain,
                            ProcessingModel = "NaturalLanguageProcessor",
                            Version = "1.0.0"
                        }
                    };
                }
                
                // Post-process and validate
                var validatedRequirements = await PostProcessRequirementsAsync(requirements, context);
                
                // Calculate confidence score
                var confidenceScore = CalculateConfidenceScore(validatedRequirements, processedInput);

                stopwatch.Stop();
                
                var result = new FeatureRequirementResult
                {
                    IsSuccess = true,
                    Requirements = validatedRequirements,
                    ConfidenceScore = confidenceScore,
                    Metadata = new ProcessingMetadata
                    {
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDurationMs = stopwatch.ElapsedMilliseconds,
                        InputFormat = inputFormat,
                        Domain = context.Domain,
                        ProcessingModel = "NaturalLanguageProcessor",
                        Version = "1.0.0"
                    }
                };

                _logger.LogInformation("Successfully processed {Count} requirements with confidence {Confidence:F2}",
                    validatedRequirements.Count, confidenceScore);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error processing natural language input");
                
                return new FeatureRequirementResult
                {
                    IsSuccess = false,
                    Errors = new List<string> { ex.Message },
                    Metadata = new ProcessingMetadata
                    {
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDurationMs = stopwatch.ElapsedMilliseconds,
                        Domain = context.Domain,
                        ProcessingModel = "NaturalLanguageProcessor",
                        Version = "1.0.0"
                    }
                };
            }
        }

        public async Task<ValidationResult> ValidateInputAsync(string input, ValidationContext context)
        {
            // Placeholder implementation
            return new ValidationResult
            {
                IsValid = true,
                Issues = new List<ValidationIssue>()
            };
        }

        public async Task<ExtractionResult> ExtractComponentsAsync(string input, ExtractionType extractionType, ExtractionContext context)
        {
            // Placeholder implementation
            return new ExtractionResult
            {
                IsSuccess = true,
                Components = new List<ExtractedComponent>(),
                ConfidenceScore = 0.8
            };
        }

        public async Task<DomainTerminologyResult> ProcessDomainTerminologyAsync(string input, string domain)
        {
            // Placeholder implementation
            return new DomainTerminologyResult
            {
                IsSuccess = true,
                RecognizedTerms = new List<DomainTerm>(),
                UnrecognizedTerms = new List<string>()
            };
        }

        public async Task<PrioritizationResult> CategorizeAndPrioritizeAsync(IEnumerable<FeatureRequirement> requirements, PrioritizationContext context)
        {
            try
            {
                var requirementsList = requirements.ToList();
                if (!requirementsList.Any())
                {
                    return new PrioritizationResult
                    {
                        IsSuccess = false
                    };
                }

                // Create prioritized requirements from input
                var prioritizedRequirements = requirementsList.Select((r, index) => new PrioritizedRequirement
                {
                    Requirement = r,
                    PriorityScore = CalculatePriorityScore(r, context),
                    BusinessValueScore = r.BusinessValue,
                    TechnicalComplexityScore = r.TechnicalComplexity,
                    RiskScore = 1.0 - r.BusinessValue, // Higher business value = lower risk
                    Category = r.Type.ToString(),
                    ImplementationOrder = index + 1
                }).OrderByDescending(pr => pr.PriorityScore).ToList();

                // Create categories based on requirement types
                var categories = requirementsList.GroupBy(r => r.Type)
                    .Select(g => new RequirementCategory
                    {
                        Name = g.Key.ToString(),
                        Description = $"Requirements of type {g.Key}",
                        RequirementCount = g.Count(),
                        TotalEffort = g.Sum(r => r.EstimatedEffort),
                        AveragePriorityScore = g.Average(r => (double)r.Priority)
                    }).ToList();

                // Calculate metrics
                var metrics = new PrioritizationMetrics
                {
                    TotalRequirements = requirementsList.Count,
                    HighPriorityCount = requirementsList.Count(r => r.Priority == RequirementPriority.High),
                    MediumPriorityCount = requirementsList.Count(r => r.Priority == RequirementPriority.Medium),
                    LowPriorityCount = requirementsList.Count(r => r.Priority == RequirementPriority.Low),
                    AverageBusinessValue = requirementsList.Average(r => r.BusinessValue),
                    AverageTechnicalComplexity = requirementsList.Average(r => r.TechnicalComplexity),
                    TotalEstimatedEffort = requirementsList.Sum(r => r.EstimatedEffort)
                };

                return new PrioritizationResult
                {
                    IsSuccess = true,
                    PrioritizedRequirements = prioritizedRequirements,
                    Categories = categories,
                    Metrics = metrics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error categorizing and prioritizing requirements");
                return new PrioritizationResult
                {
                    IsSuccess = false
                };
            }
        }

        private double CalculatePriorityScore(FeatureRequirement requirement, PrioritizationContext context)
        {
            var businessValueWeight = context.BusinessValueWeights?.ContainsKey(requirement.Type.ToString()) == true 
                ? context.BusinessValueWeights[requirement.Type.ToString()] 
                : 1.0;
            var technicalComplexityWeight = context.TechnicalComplexityWeights?.ContainsKey(requirement.Type.ToString()) == true 
                ? context.TechnicalComplexityWeights[requirement.Type.ToString()] 
                : 1.0;
            
            var businessValueScore = requirement.BusinessValue * businessValueWeight;
            var technicalComplexityScore = (1.0 - requirement.TechnicalComplexity) * technicalComplexityWeight;
            
            return (businessValueScore + technicalComplexityScore) / 2.0;
        }

        public bool SupportsFormat(InputFormat inputFormat)
        {
            return inputFormat == InputFormat.PlainText;
        }

        public IEnumerable<InputFormat> GetSupportedFormats()
        {
            return new InputFormat[] { InputFormat.PlainText };
        }

        public IEnumerable<string> GetSupportedDomains()
        {
            return new string[] { "General", "E-commerce", "Healthcare", "Finance" };
        }

        // Placeholder methods - will be implemented in next chunks
        private Dictionary<string, List<string>> InitializeDomainTerminology() => new Dictionary<string, List<string>>();
        private InputFormat DetectInputFormat(string input) => InputFormat.PlainText;
        private async Task<string> PreprocessInputAsync(string input, ProcessingContext context) => input;
        private async Task<List<FeatureRequirement>> ExtractRequirementsWithAIAsync(string input, ProcessingContext context, InputFormat format)
        {
            try
            {
                // Call the model orchestrator to extract requirements
                var response = await _modelOrchestrator.ExecuteAsync(new ModelRequest
                {
                    Input = $"Extract feature requirements from the following input: {input}",
                    SystemPrompt = "You are an expert at extracting software requirements from natural language descriptions. Extract clear, actionable requirements.",
                    MaxTokens = 1000,
                    Temperature = 0.3,
                    Metadata = new Dictionary<string, object>
                    {
                        ["domain"] = context.Domain,
                        ["format"] = format.ToString(),
                        ["businessRules"] = context.BusinessRules,
                        ["technicalConstraints"] = context.TechnicalConstraints
                    }
                }, CancellationToken.None);

                // Create a requirement based on the response
                var requirement = new FeatureRequirement
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = ExtractTitleFromInput(input),
                    Description = response.Content ?? input,
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.Medium,
                    BusinessValue = 0.7,
                    TechnicalComplexity = 0.5,
                    EstimatedEffort = 5,
                    Tags = new List<string> { "extracted", "ai-processed" },
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };

                return new List<FeatureRequirement> { requirement };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting requirements with AI");
                return new List<FeatureRequirement>();
            }
        }
        private async Task<List<FeatureRequirement>> PostProcessRequirementsAsync(List<FeatureRequirement> requirements, ProcessingContext context) => requirements;
        private double CalculateConfidenceScore(List<FeatureRequirement> requirements, string input) => 0.8;

        private string ExtractTitleFromInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Untitled Requirement";

            // Try to extract a title from the first line or sentence
            var lines = input.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
            {
                var firstLine = lines[0].Trim();
                if (firstLine.Length > 0)
                {
                    // If it looks like a title (short, ends with colon, etc.)
                    if (firstLine.Length <= 100 && (firstLine.EndsWith(":") || firstLine.EndsWith(".") || !firstLine.Contains(' ')))
                    {
                        return firstLine.TrimEnd(':', '.');
                    }
                }
            }

            // Fallback: use first few words
            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0)
            {
                var title = string.Join(" ", words.Take(5));
                return title.Length > 50 ? title.Substring(0, 47) + "..." : title;
            }

            return "Untitled Requirement";
        }
    }
}