using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Service for extracting, validating, and enhancing feature requirements from natural language input.
    /// This is the core implementation for Phase 5.1.2 of the Feature Factory pipeline.
    /// </summary>
    public class FeatureRequirementsExtractor : IFeatureRequirementsExtractor
    {
        private readonly ILogger<FeatureRequirementsExtractor> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly INaturalLanguageProcessor _naturalLanguageProcessor;
        private readonly IDomainLogicValidator _domainLogicValidator;

        public FeatureRequirementsExtractor(
            ILogger<FeatureRequirementsExtractor> logger,
            IModelOrchestrator modelOrchestrator,
            INaturalLanguageProcessor naturalLanguageProcessor,
            IDomainLogicValidator domainLogicValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _naturalLanguageProcessor = naturalLanguageProcessor ?? throw new ArgumentNullException(nameof(naturalLanguageProcessor));
            _domainLogicValidator = domainLogicValidator ?? throw new ArgumentNullException(nameof(domainLogicValidator));
        }

        /// <summary>
        /// Extracts feature requirements from natural language input with comprehensive analysis.
        /// </summary>
        public async Task<FeatureRequirementResult> ExtractRequirementsAsync(
            string input,
            ProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting feature requirements extraction for input: {InputLength} characters", input.Length);

            try
            {
                // Step 1: Use NaturalLanguageProcessor to get initial requirements
                var initialResult = await _naturalLanguageProcessor.ProcessRequirementsAsync(input, context);
                
                if (!initialResult.IsSuccess)
                {
                    _logger.LogWarning("Natural language processing failed: {Errors}", string.Join(", ", initialResult.Errors ?? new List<string>()));
                    return new FeatureRequirementResult
                    {
                        IsSuccess = false,
                        Errors = initialResult.Errors ?? new List<string>(),
                        Metadata = new ProcessingMetadata
                        {
                            ProcessedAt = DateTime.UtcNow,
                            ProcessingDurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                            Domain = context.Domain,
                            ProcessingModel = "FeatureRequirementsExtractor"
                        }
                    };
                }

                // Step 2: Enhance requirements with AI analysis
                var enhancedRequirements = await EnhanceRequirementsWithAIAsync(initialResult.Requirements, context, cancellationToken);

                // Step 3: Validate requirements
                var validationContext = new ValidationContext
                {
                    RequiredFields = new List<string> { "Title", "Description", "Type", "Priority" },
                    BusinessRules = context.BusinessRules,
                    MinimumCompleteness = 0.8,
                    MaximumAmbiguity = 0.3
                };

                var validationResult = await ValidateRequirementsAsync(enhancedRequirements, validationContext, cancellationToken);

                // Step 4: Calculate confidence score based on validation
                var confidenceScore = CalculateConfidenceScore(enhancedRequirements, validationResult);

                var result = new FeatureRequirementResult
                {
                    IsSuccess = true,
                    Requirements = enhancedRequirements,
                    ConfidenceScore = confidenceScore,
                                            Warnings = validationResult.Issues?.Where(i => i.Severity == IssueSeverity.Medium || i.Severity == IssueSeverity.Low)
                            .Select(i => i.Message).ToList() ?? new List<string>(),
                                            Errors = validationResult.Issues?.Where(i => i.Severity == IssueSeverity.Critical || i.Severity == IssueSeverity.High)
                            .Select(i => i.Message).ToList() ?? new List<string>(),
                    Metadata = new ProcessingMetadata
                    {
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                        Domain = context.Domain,
                        ProcessingModel = "FeatureRequirementsExtractor",
                        Version = "1.0.0"
                    }
                };

                _logger.LogInformation("Feature requirements extraction completed successfully. Extracted {Count} requirements with confidence {Confidence}",
                    enhancedRequirements.Count, confidenceScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during feature requirements extraction");
                return new FeatureRequirementResult
                {
                    IsSuccess = false,
                    Errors = new List<string> { ex.Message },
                    Metadata = new ProcessingMetadata
                    {
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                        Domain = context.Domain,
                        ProcessingModel = "FeatureRequirementsExtractor"
                    }
                };
            }
        }

        /// <summary>
        /// Validates extracted requirements against business rules and domain context.
        /// </summary>
        public async Task<ValidationResult> ValidateRequirementsAsync(
            List<FeatureRequirement> requirements,
            ValidationContext context,
            CancellationToken cancellationToken = default)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _logger.LogInformation("Starting requirements validation for {Count} requirements", requirements.Count);

            var issues = new List<ValidationIssue>();
            var recommendations = new List<string>();

            try
            {
                foreach (var requirement in requirements)
                {
                    // Validate required fields
                    var fieldValidation = ValidateRequiredFields(requirement, context.RequiredFields);
                    issues.AddRange(fieldValidation);

                    // Validate business rules
                    var businessRuleValidation = await ValidateBusinessRulesAsync(requirement, context.BusinessRules, cancellationToken);
                    issues.AddRange(businessRuleValidation);

                    // Validate completeness
                    var completenessValidation = ValidateCompleteness(requirement, context.MinimumCompleteness);
                    issues.AddRange(completenessValidation);

                    // Validate clarity and ambiguity
                    var clarityValidation = ValidateClarity(requirement, context.MaximumAmbiguity);
                    issues.AddRange(clarityValidation);
                }

                // Calculate validation scores
                var validationScore = CalculateValidationScore(requirements, issues);
                var completenessScore = CalculateCompletenessScore(requirements);
                var clarityScore = CalculateClarityScore(requirements);
                var ambiguityScore = CalculateAmbiguityScore(requirements);

                // Generate recommendations
                recommendations = GenerateRecommendations(issues, requirements);

                var result = new ValidationResult
                {
                    IsValid = validationScore >= 0.8 && issues.Count(i => i.Severity == IssueSeverity.Critical || i.Severity == IssueSeverity.High) == 0,
                    ValidationScore = validationScore,
                    CompletenessScore = completenessScore,
                    ClarityScore = clarityScore,
                    AmbiguityScore = ambiguityScore,
                    Issues = issues,
                    Recommendations = recommendations
                };

                _logger.LogInformation("Requirements validation completed. Score: {Score}, Issues: {IssueCount}, Recommendations: {RecommendationCount}",
                    validationScore, issues.Count, recommendations.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during requirements validation");
                return new ValidationResult
                {
                    IsValid = false,
                    ValidationScore = 0.0,
                    Issues = new List<ValidationIssue> { new ValidationIssue { Message = ex.Message, Severity = IssueSeverity.Critical } }
                };
            }
        }

        /// <summary>
        /// Enhances requirements with additional context, business rules, and technical specifications.
        /// </summary>
        public async Task<FeatureRequirementResult> EnhanceRequirementsAsync(
            List<FeatureRequirement> requirements,
            ProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _logger.LogInformation("Starting requirements enhancement for {Count} requirements", requirements.Count);

            try
            {
                var enhancedRequirements = new List<FeatureRequirement>();

                foreach (var requirement in requirements)
                {
                    // Enhance with AI-generated business rules
                    var enhancedBusinessRules = await GenerateBusinessRulesAsync(requirement, context, cancellationToken);
                    requirement.BusinessRules.AddRange(enhancedBusinessRules);

                    // Enhance with technical requirements
                    var technicalRequirements = await GenerateTechnicalRequirementsAsync(requirement, context, cancellationToken);
                    requirement.TechnicalRequirements.AddRange(technicalRequirements);

                    // Enhance with user stories
                    var userStories = await GenerateUserStoriesAsync(requirement, context, cancellationToken);
                    requirement.UserStories.AddRange(userStories);

                    // Enhance with acceptance criteria
                    var acceptanceCriteria = await GenerateAcceptanceCriteriaAsync(requirement, context, cancellationToken);
                    requirement.AcceptanceCriteria.AddRange(acceptanceCriteria);

                    // Enhance with integration points
                    var integrationPoints = await GenerateIntegrationPointsAsync(requirement, context, cancellationToken);
                    requirement.IntegrationPoints.AddRange(integrationPoints);

                    // Update completeness score
                    requirement.CompletenessScore = CalculateRequirementCompleteness(requirement);

                    enhancedRequirements.Add(requirement);
                }

                var result = new FeatureRequirementResult
                {
                    IsSuccess = true,
                    Requirements = enhancedRequirements,
                    ConfidenceScore = 0.9, // High confidence for enhanced requirements
                    Metadata = new ProcessingMetadata
                    {
                        ProcessedAt = DateTime.UtcNow,
                        Domain = context.Domain,
                        ProcessingModel = "FeatureRequirementsExtractor"
                    }
                };

                _logger.LogInformation("Requirements enhancement completed successfully for {Count} requirements", enhancedRequirements.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during requirements enhancement");
                return new FeatureRequirementResult
                {
                    IsSuccess = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// Prioritizes requirements based on business value, technical complexity, and risk.
        /// </summary>
        public async Task<PrioritizationResult> PrioritizeRequirementsAsync(
            List<FeatureRequirement> requirements,
            ProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _logger.LogInformation("Starting requirements prioritization for {Count} requirements", requirements.Count);

            try
            {
                var prioritizedRequirements = new List<PrioritizedRequirement>();
                var categories = new List<RequirementCategory>();

                foreach (var requirement in requirements)
                {
                    // Calculate priority scores
                    var businessValueScore = CalculateBusinessValueScore(requirement, context);
                    var technicalComplexityScore = CalculateTechnicalComplexityScore(requirement);
                    var riskScore = CalculateRiskScore(requirement);
                    var priorityScore = CalculateOverallPriorityScore(businessValueScore, technicalComplexityScore, riskScore);

                    var prioritizedRequirement = new PrioritizedRequirement
                    {
                        Requirement = requirement,
                        PriorityScore = priorityScore,
                        BusinessValueScore = businessValueScore,
                        TechnicalComplexityScore = technicalComplexityScore,
                        RiskScore = riskScore,
                        Category = DetermineRequirementCategory(requirement),
                        ImplementationOrder = 0 // Will be set after sorting
                    };

                    prioritizedRequirements.Add(prioritizedRequirement);
                }

                // Sort by priority score (descending)
                prioritizedRequirements = prioritizedRequirements.OrderByDescending(r => r.PriorityScore).ToList();

                // Set implementation order
                for (int i = 0; i < prioritizedRequirements.Count; i++)
                {
                    prioritizedRequirements[i].ImplementationOrder = i + 1;
                }

                // Generate categories
                categories = GenerateRequirementCategories(prioritizedRequirements);

                // Calculate metrics
                var metrics = CalculatePrioritizationMetrics(prioritizedRequirements);

                var result = new PrioritizationResult
                {
                    IsSuccess = true,
                    PrioritizedRequirements = prioritizedRequirements,
                    Categories = categories,
                    Metrics = metrics
                };

                _logger.LogInformation("Requirements prioritization completed. High priority: {HighCount}, Medium: {MediumCount}, Low: {LowCount}",
                    metrics.HighPriorityCount, metrics.MediumPriorityCount, metrics.LowPriorityCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during requirements prioritization");
                return new PrioritizationResult
                {
                    IsSuccess = false
                };
            }
        }

        /// <summary>
        /// Analyzes requirements for completeness, clarity, and consistency.
        /// </summary>
        public async Task<RequirementsAnalysisResult> AnalyzeRequirementsAsync(
            List<FeatureRequirement> requirements,
            CancellationToken cancellationToken = default)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            _logger.LogInformation("Starting requirements analysis for {Count} requirements", requirements.Count);

            try
            {
                var insights = new List<RequirementInsight>();
                var gaps = new List<RequirementGap>();
                var recommendations = new List<string>();

                // Analyze each requirement
                foreach (var requirement in requirements)
                {
                    // Generate insights
                    var requirementInsights = GenerateRequirementInsights(requirement);
                    insights.AddRange(requirementInsights);

                    // Identify gaps
                    var requirementGaps = IdentifyRequirementGaps(requirement);
                    gaps.AddRange(requirementGaps);
                }

                // Generate cross-requirement insights
                var crossInsights = GenerateCrossRequirementInsights(requirements);
                insights.AddRange(crossInsights);

                // Calculate analysis scores
                var completenessScore = CalculateCompletenessScore(requirements);
                var clarityScore = CalculateClarityScore(requirements);
                var consistencyScore = CalculateConsistencyScore(requirements);
                var feasibilityScore = CalculateFeasibilityScore(requirements);

                // Generate recommendations
                recommendations = GenerateAnalysisRecommendations(insights, gaps, requirements);

                var result = new RequirementsAnalysisResult
                {
                    IsSuccess = true,
                    CompletenessScore = completenessScore,
                    ClarityScore = clarityScore,
                    ConsistencyScore = consistencyScore,
                    FeasibilityScore = feasibilityScore,
                    Insights = insights,
                    Gaps = gaps,
                    Recommendations = recommendations,
                    Metadata = new ProcessingMetadata
                    {
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingModel = "FeatureRequirementsExtractor"
                    }
                };

                _logger.LogInformation("Requirements analysis completed. Completeness: {Completeness}, Clarity: {Clarity}, Consistency: {Consistency}",
                    completenessScore, clarityScore, consistencyScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during requirements analysis");
                return new RequirementsAnalysisResult
                {
                    IsSuccess = false
                };
            }
        }

        /// <summary>
        /// Generates acceptance criteria for requirements based on business rules and user stories.
        /// </summary>
        public async Task<AcceptanceCriteriaResult> GenerateAcceptanceCriteriaAsync(
            List<FeatureRequirement> requirements,
            ProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _logger.LogInformation("Starting acceptance criteria generation for {Count} requirements", requirements.Count);

            try
            {
                var criteria = new List<RequirementAcceptanceCriteria>();

                foreach (var requirement in requirements)
                {
                    // Generate acceptance criteria using AI
                    var acceptanceCriteria = await GenerateAcceptanceCriteriaWithAIAsync(requirement, context, cancellationToken);

                    // Generate test scenarios
                    var testScenarios = await GenerateTestScenariosAsync(requirement, acceptanceCriteria, context, cancellationToken);

                    var requirementCriteria = new RequirementAcceptanceCriteria
                    {
                        RequirementId = requirement.Id,
                        Criteria = acceptanceCriteria,
                        TestScenarios = testScenarios,
                        QualityScore = CalculateAcceptanceCriteriaQuality(acceptanceCriteria, testScenarios)
                    };

                    criteria.Add(requirementCriteria);
                }

                var overallQualityScore = criteria.Any() ? criteria.Average(c => c.QualityScore) : 0.0;

                var result = new AcceptanceCriteriaResult
                {
                    IsSuccess = true,
                    Criteria = criteria,
                    QualityScore = overallQualityScore,
                    Metadata = new ProcessingMetadata
                    {
                        ProcessedAt = DateTime.UtcNow,
                        Domain = context.Domain,
                        ProcessingModel = "FeatureRequirementsExtractor"
                    }
                };

                _logger.LogInformation("Acceptance criteria generation completed. Quality score: {Quality}", overallQualityScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during acceptance criteria generation");
                return new AcceptanceCriteriaResult
                {
                    IsSuccess = false
                };
            }
        }

        #region Private Helper Methods

        private async Task<List<FeatureRequirement>> EnhanceRequirementsWithAIAsync(
            List<FeatureRequirement> requirements,
            ProcessingContext context,
            CancellationToken cancellationToken)
        {
            var enhancedRequirements = new List<FeatureRequirement>();

            foreach (var requirement in requirements)
            {
                // Use AI to enhance the requirement
                var enhancedRequirement = await EnhanceRequirementWithAIAsync(requirement, context, cancellationToken);
                enhancedRequirements.Add(enhancedRequirement);
            }

            return enhancedRequirements;
        }

        private async Task<FeatureRequirement> EnhanceRequirementWithAIAsync(
            FeatureRequirement requirement,
            ProcessingContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"Enhance the following feature requirement with additional details:

Original Requirement:
Title: {requirement.Title}
Description: {requirement.Description}
Type: {requirement.Type}

Domain: {context.Domain}
Business Rules: {string.Join(", ", context.BusinessRules)}
Technical Constraints: {string.Join(", ", context.TechnicalConstraints)}

Please provide:
1. Enhanced description with more detail
2. Business value assessment (0.0-1.0)
3. Technical complexity assessment (0.0-1.0)
4. Estimated effort in story points
5. Suggested business rules
6. Technical requirements
7. User stories
8. Acceptance criteria

Respond in JSON format.";

                var response = await _modelOrchestrator.ExecuteAsync(new ModelRequest
                {
                    Input = prompt,
                    SystemPrompt = "You are an expert at enhancing software requirements with detailed specifications.",
                    MaxTokens = 2000,
                    Temperature = 0.3
                }, cancellationToken);

                // Parse the AI response and update the requirement
                // This is a simplified implementation - in practice, you'd parse the JSON response
                requirement.BusinessValue = 0.8; // Default value
                requirement.TechnicalComplexity = 0.6; // Default value
                requirement.EstimatedEffort = 8; // Default value

                return requirement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enhancing requirement with AI");
                return requirement; // Return original requirement if enhancement fails
            }
        }

        private List<ValidationIssue> ValidateRequiredFields(FeatureRequirement requirement, IEnumerable<string> requiredFields)
        {
            var issues = new List<ValidationIssue>();

            foreach (var field in requiredFields)
            {
                var value = GetPropertyValue(requirement, field);
                if (string.IsNullOrWhiteSpace(value))
                {
                    issues.Add(new ValidationIssue
                    {
                        Component = "FeatureRequirement",
                        Property = field,
                        Message = $"Required field '{field}' is missing or empty",
                        Severity = IssueSeverity.High,
                        Scope = ValidationScope.Syntax,
                        Suggestion = $"Provide a value for the '{field}' field"
                    });
                }
            }

            return issues;
        }

        private async Task<List<ValidationIssue>> ValidateBusinessRulesAsync(
            FeatureRequirement requirement,
            IEnumerable<string> businessRules,
            CancellationToken cancellationToken)
        {
            var issues = new List<ValidationIssue>();

            // This would validate against actual business rules
            // For now, we'll do basic validation
            if (string.IsNullOrWhiteSpace(requirement.Title))
            {
                issues.Add(new ValidationIssue
                {
                    Component = "FeatureRequirement",
                    Property = "Title",
                    Message = "Requirement title is required",
                    Severity = IssueSeverity.High,
                    Scope = ValidationScope.Business,
                    Suggestion = "Provide a clear, descriptive title for the requirement"
                });
            }

            return issues;
        }

        private List<ValidationIssue> ValidateCompleteness(FeatureRequirement requirement, double minimumCompleteness)
        {
            var issues = new List<ValidationIssue>();
            var completeness = CalculateRequirementCompleteness(requirement);

            if (completeness < minimumCompleteness)
            {
                issues.Add(new ValidationIssue
                {
                    Component = "FeatureRequirement",
                    Property = "Completeness",
                    Message = $"Requirement completeness ({completeness:P0}) is below minimum threshold ({minimumCompleteness:P0})",
                    Severity = IssueSeverity.Medium,
                    Scope = ValidationScope.Completeness,
                    Suggestion = "Add more details, acceptance criteria, or technical specifications"
                });
            }

            return issues;
        }

        private List<ValidationIssue> ValidateClarity(FeatureRequirement requirement, double maximumAmbiguity)
        {
            var issues = new List<ValidationIssue>();
            var ambiguity = CalculateRequirementAmbiguity(requirement);

            if (ambiguity > maximumAmbiguity)
            {
                issues.Add(new ValidationIssue
                {
                    Component = "FeatureRequirement",
                    Property = "Clarity",
                    Message = $"Requirement ambiguity ({ambiguity:P0}) exceeds maximum threshold ({maximumAmbiguity:P0})",
                    Severity = IssueSeverity.Medium,
                    Scope = ValidationScope.Clarity,
                    Suggestion = "Use more specific language and provide concrete examples"
                });
            }

            return issues;
        }

        private double CalculateConfidenceScore(List<FeatureRequirement> requirements, ValidationResult validationResult)
        {
            if (!requirements.Any())
                return 0.0;

            var baseScore = 0.8; // Base confidence
            var validationBonus = validationResult.ValidationScore * 0.2; // Up to 20% bonus for good validation
            var completenessBonus = validationResult.CompletenessScore * 0.1; // Up to 10% bonus for completeness

            return Math.Min(1.0, baseScore + validationBonus + completenessBonus);
        }

        private double CalculateValidationScore(List<FeatureRequirement> requirements, List<ValidationIssue> issues)
        {
            if (!requirements.Any())
                return 0.0;

            var criticalIssues = issues.Count(i => i.Severity == IssueSeverity.Critical);
            var highIssues = issues.Count(i => i.Severity == IssueSeverity.High);
            var mediumIssues = issues.Count(i => i.Severity == IssueSeverity.Medium);
            var lowIssues = issues.Count(i => i.Severity == IssueSeverity.Low);

            var penalty = (criticalIssues * 0.3) + (highIssues * 0.2) + (mediumIssues * 0.1) + (lowIssues * 0.05);
            var score = Math.Max(0.0, 1.0 - penalty);

            return score;
        }

        private double CalculateCompletenessScore(List<FeatureRequirement> requirements)
        {
            if (!requirements.Any())
                return 0.0;

            return requirements.Average(r => r.CompletenessScore);
        }

        private double CalculateClarityScore(List<FeatureRequirement> requirements)
        {
            if (!requirements.Any())
                return 0.0;

            // Simplified clarity calculation based on description length and structure
            var scores = requirements.Select(r =>
            {
                var descriptionLength = r.Description?.Length ?? 0;
                var hasAcceptanceCriteria = r.AcceptanceCriteria?.Any() == true;
                var hasUserStories = r.UserStories?.Any() == true;

                var score = 0.0;
                if (descriptionLength > 50) score += 0.3;
                if (descriptionLength > 100) score += 0.2;
                if (hasAcceptanceCriteria) score += 0.3;
                if (hasUserStories) score += 0.2;

                return Math.Min(1.0, score);
            });

            return scores.Average();
        }

        private double CalculateAmbiguityScore(List<FeatureRequirement> requirements)
        {
            if (!requirements.Any())
                return 1.0; // Maximum ambiguity for empty requirements

            // Simplified ambiguity calculation
            var scores = requirements.Select(r =>
            {
                var description = r.Description?.ToLower() ?? "";
                var ambiguousTerms = new[] { "maybe", "possibly", "might", "could", "should", "approximately", "around", "about" };
                var ambiguousCount = ambiguousTerms.Count(term => description.Contains(term));
                
                return Math.Min(1.0, ambiguousCount * 0.2); // 20% penalty per ambiguous term
            });

            return scores.Average();
        }

        private List<string> GenerateRecommendations(List<ValidationIssue> issues, List<FeatureRequirement> requirements)
        {
            var recommendations = new List<string>();

            if (issues.Any(i => i.Severity == IssueSeverity.Critical))
            {
                recommendations.Add("Address critical validation issues before proceeding with implementation");
            }

            if (issues.Any(i => i.Severity == IssueSeverity.High))
            {
                recommendations.Add("Resolve high-priority validation issues to improve requirement quality");
            }

            if (requirements.Any(r => r.CompletenessScore < 0.7))
            {
                recommendations.Add("Enhance incomplete requirements with additional details and specifications");
            }

            if (requirements.Any(r => string.IsNullOrWhiteSpace(r.Description) || r.Description.Length < 50))
            {
                recommendations.Add("Provide more detailed descriptions for requirements with insufficient information");
            }

            if (!requirements.Any(r => r.AcceptanceCriteria?.Any() == true))
            {
                recommendations.Add("Generate acceptance criteria for all requirements to ensure testability");
            }

            return recommendations;
        }

        private async Task<List<BusinessRule>> GenerateBusinessRulesAsync(
            FeatureRequirement requirement,
            ProcessingContext context,
            CancellationToken cancellationToken)
        {
            // This would use AI to generate business rules
            // For now, return empty list
            return new List<BusinessRule>();
        }

        private async Task<List<TechnicalRequirement>> GenerateTechnicalRequirementsAsync(
            FeatureRequirement requirement,
            ProcessingContext context,
            CancellationToken cancellationToken)
        {
            // This would use AI to generate technical requirements
            // For now, return empty list
            return new List<TechnicalRequirement>();
        }

        private async Task<List<UserStory>> GenerateUserStoriesAsync(
            FeatureRequirement requirement,
            ProcessingContext context,
            CancellationToken cancellationToken)
        {
            // This would use AI to generate user stories
            // For now, return empty list
            return new List<UserStory>();
        }

        private async Task<List<string>> GenerateAcceptanceCriteriaAsync(
            FeatureRequirement requirement,
            ProcessingContext context,
            CancellationToken cancellationToken)
        {
            // This would use AI to generate acceptance criteria
            // For now, return empty list
            return new List<string>();
        }

        private async Task<List<IntegrationPoint>> GenerateIntegrationPointsAsync(
            FeatureRequirement requirement,
            ProcessingContext context,
            CancellationToken cancellationToken)
        {
            // This would use AI to generate integration points
            // For now, return empty list
            return new List<IntegrationPoint>();
        }

        private double CalculateRequirementCompleteness(FeatureRequirement requirement)
        {
            var score = 0.0;

            if (!string.IsNullOrWhiteSpace(requirement.Title)) score += 0.2;
            if (!string.IsNullOrWhiteSpace(requirement.Description)) score += 0.3;
            if (requirement.AcceptanceCriteria?.Any() == true) score += 0.2;
            if (requirement.UserStories?.Any() == true) score += 0.15;
            if (requirement.BusinessRules?.Any() == true) score += 0.15;

            return Math.Min(1.0, score);
        }

        private double CalculateBusinessValueScore(FeatureRequirement requirement, ProcessingContext context)
        {
            // Simplified business value calculation
            var baseValue = requirement.BusinessValue;
            var domainBonus = context.Domain?.ToLower().Contains("customer") == true ? 0.1 : 0.0;
            var stakeholderBonus = context.UserRoles?.Any() == true ? 0.1 : 0.0;

            return Math.Min(1.0, baseValue + domainBonus + stakeholderBonus);
        }

        private double CalculateTechnicalComplexityScore(FeatureRequirement requirement)
        {
            // Simplified technical complexity calculation
            var baseComplexity = requirement.TechnicalComplexity;
            var integrationBonus = requirement.IntegrationPoints?.Any() == true ? 0.2 : 0.0;
            var technicalRequirementsBonus = requirement.TechnicalRequirements?.Any() == true ? 0.1 : 0.0;

            return Math.Min(1.0, baseComplexity + integrationBonus + technicalRequirementsBonus);
        }

        private double CalculateRiskScore(FeatureRequirement requirement)
        {
            // Simplified risk calculation
            var technicalComplexity = requirement.TechnicalComplexity;
            var integrationRisk = requirement.IntegrationPoints?.Any() == true ? 0.2 : 0.0;
            var businessValueRisk = requirement.BusinessValue > 0.8 ? 0.1 : 0.0; // High business value = higher risk

            return Math.Min(1.0, technicalComplexity + integrationRisk + businessValueRisk);
        }

        private double CalculateOverallPriorityScore(double businessValue, double technicalComplexity, double risk)
        {
            // Priority = Business Value - (Technical Complexity * 0.3) - (Risk * 0.2)
            return Math.Max(0.0, businessValue - (technicalComplexity * 0.3) - (risk * 0.2));
        }

        private string DetermineRequirementCategory(FeatureRequirement requirement)
        {
            if (requirement.BusinessValue > 0.8 && requirement.TechnicalComplexity < 0.4)
                return "Quick Wins";
            if (requirement.BusinessValue > 0.8 && requirement.TechnicalComplexity > 0.6)
                return "Major Projects";
            if (requirement.BusinessValue < 0.4 && requirement.TechnicalComplexity < 0.4)
                return "Fill-ins";
            return "Thankless Tasks";
        }

        private List<RequirementCategory> GenerateRequirementCategories(List<PrioritizedRequirement> prioritizedRequirements)
        {
            var categories = new List<RequirementCategory>();

            var categoryGroups = prioritizedRequirements.GroupBy(r => r.Category);
            foreach (var group in categoryGroups)
            {
                var category = new RequirementCategory
                {
                    Name = group.Key,
                    Description = $"Requirements categorized as {group.Key}",
                    RequirementCount = group.Count(),
                    TotalEffort = group.Sum(r => r.Requirement.EstimatedEffort),
                    AveragePriorityScore = group.Average(r => r.PriorityScore)
                };

                categories.Add(category);
            }

            return categories;
        }

        private PrioritizationMetrics CalculatePrioritizationMetrics(List<PrioritizedRequirement> prioritizedRequirements)
        {
            return new PrioritizationMetrics
            {
                TotalRequirements = prioritizedRequirements.Count,
                HighPriorityCount = prioritizedRequirements.Count(r => r.PriorityScore > 0.7),
                MediumPriorityCount = prioritizedRequirements.Count(r => r.PriorityScore > 0.4 && r.PriorityScore <= 0.7),
                LowPriorityCount = prioritizedRequirements.Count(r => r.PriorityScore <= 0.4),
                TotalEstimatedEffort = prioritizedRequirements.Sum(r => r.Requirement.EstimatedEffort),
                AverageBusinessValue = prioritizedRequirements.Average(r => r.BusinessValueScore),
                AverageTechnicalComplexity = prioritizedRequirements.Average(r => r.TechnicalComplexityScore)
            };
        }

        private List<RequirementInsight> GenerateRequirementInsights(FeatureRequirement requirement)
        {
            var insights = new List<RequirementInsight>();

            // Business value insight
            if (requirement.BusinessValue > 0.8)
            {
                insights.Add(new RequirementInsight
                {
                    Type = RequirementInsightType.BusinessValue,
                    Description = "High business value requirement that should be prioritized",
                    RequirementIds = new List<string> { requirement.Id },
                    ConfidenceScore = 0.9,
                    Impact = ImpactLevel.High,
                    SuggestedActions = new List<string> { "Prioritize for early implementation", "Ensure stakeholder buy-in" }
                });
            }

            // Technical complexity insight
            if (requirement.TechnicalComplexity > 0.7)
            {
                insights.Add(new RequirementInsight
                {
                    Type = RequirementInsightType.TechnicalComplexity,
                    Description = "High technical complexity that may require additional resources",
                    RequirementIds = new List<string> { requirement.Id },
                    ConfidenceScore = 0.8,
                    Impact = ImpactLevel.Medium,
                    SuggestedActions = new List<string> { "Consider breaking into smaller requirements", "Allocate experienced developers" }
                });
            }

            return insights;
        }

        private List<RequirementGap> IdentifyRequirementGaps(FeatureRequirement requirement)
        {
            var gaps = new List<RequirementGap>();

            if (string.IsNullOrWhiteSpace(requirement.Description) || requirement.Description.Length < 50)
            {
                gaps.Add(new RequirementGap
                {
                    Type = RequirementGapType.MissingFunctionalRequirement,
                    Description = "Insufficient requirement description",
                    Severity = IssueSeverity.High,
                    RequirementIds = new List<string> { requirement.Id },
                    SuggestedResolution = "Provide more detailed description of the requirement",
                    EstimatedEffort = 2
                });
            }

            if (!requirement.AcceptanceCriteria?.Any() == true)
            {
                gaps.Add(new RequirementGap
                {
                    Type = RequirementGapType.MissingAcceptanceCriteria,
                    Description = "No acceptance criteria defined",
                    Severity = IssueSeverity.Medium,
                    RequirementIds = new List<string> { requirement.Id },
                    SuggestedResolution = "Generate acceptance criteria for testability",
                    EstimatedEffort = 3
                });
            }

            return gaps;
        }

        private List<RequirementInsight> GenerateCrossRequirementInsights(List<FeatureRequirement> requirements)
        {
            var insights = new List<RequirementInsight>();

            // Dependency insights
            var highComplexityRequirements = requirements.Where(r => r.TechnicalComplexity > 0.7).ToList();
            if (highComplexityRequirements.Count > 1)
            {
                insights.Add(new RequirementInsight
                {
                    Type = RequirementInsightType.Dependency,
                    Description = $"Multiple high-complexity requirements ({highComplexityRequirements.Count}) may create implementation dependencies",
                    RequirementIds = highComplexityRequirements.Select(r => r.Id).ToList(),
                    ConfidenceScore = 0.8,
                    Impact = ImpactLevel.Medium,
                    SuggestedActions = new List<string> { "Review implementation order", "Consider parallel development" }
                });
            }

            return insights;
        }

        private List<string> GenerateAnalysisRecommendations(List<RequirementInsight> insights, List<RequirementGap> gaps, List<FeatureRequirement> requirements)
        {
            var recommendations = new List<string>();

            if (gaps.Any(g => g.Severity == IssueSeverity.Critical))
            {
                recommendations.Add("Address critical gaps before proceeding with implementation");
            }

            if (insights.Any(i => i.Type == RequirementInsightType.BusinessValue && i.Impact == ImpactLevel.High))
            {
                recommendations.Add("Prioritize high business value requirements for early delivery");
            }

            if (insights.Any(i => i.Type == RequirementInsightType.TechnicalComplexity && i.Impact == ImpactLevel.High))
            {
                recommendations.Add("Allocate additional resources for high complexity requirements");
            }

            return recommendations;
        }

        private double CalculateConsistencyScore(List<FeatureRequirement> requirements)
        {
            if (requirements.Count < 2)
                return 1.0;

            // Simplified consistency calculation
            var scores = new List<double>();

            // Check terminology consistency
            var allTerms = requirements.SelectMany(r => ExtractTerms(r.Description)).ToList();
            var uniqueTerms = allTerms.Distinct().Count();
            var totalTerms = allTerms.Count;
            var terminologyConsistency = totalTerms > 0 ? (double)uniqueTerms / totalTerms : 1.0;

            scores.Add(terminologyConsistency);

            return scores.Average();
        }

        private double CalculateFeasibilityScore(List<FeatureRequirement> requirements)
        {
            if (!requirements.Any())
                return 0.0;

            // Simplified feasibility calculation
            var scores = requirements.Select(r =>
            {
                var technicalFeasibility = 1.0 - r.TechnicalComplexity;
                var resourceFeasibility = r.EstimatedEffort <= 13 ? 1.0 : 0.5; // Assuming 13 story points is manageable
                var businessFeasibility = r.BusinessValue;

                return (technicalFeasibility + resourceFeasibility + businessFeasibility) / 3.0;
            });

            return scores.Average();
        }

        private async Task<List<string>> GenerateAcceptanceCriteriaWithAIAsync(
            FeatureRequirement requirement,
            ProcessingContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"Generate acceptance criteria for the following requirement:

Requirement: {requirement.Title}
Description: {requirement.Description}
Type: {requirement.Type}

Domain: {context.Domain}

Generate 3-5 clear, testable acceptance criteria. Each criterion should be specific and measurable.

Respond with a numbered list of acceptance criteria.";

                var response = await _modelOrchestrator.ExecuteAsync(new ModelRequest
                {
                    Input = prompt,
                    SystemPrompt = "You are an expert at writing clear, testable acceptance criteria for software requirements.",
                    MaxTokens = 500,
                    Temperature = 0.3
                }, cancellationToken);

                // Parse the response into a list of criteria
                var criteria = ParseAcceptanceCriteria(response.Content);
                return criteria;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating acceptance criteria with AI");
                return new List<string> { "Acceptance criteria generation failed" };
            }
        }

        private async Task<List<TestScenario>> GenerateTestScenariosAsync(
            FeatureRequirement requirement,
            List<string> acceptanceCriteria,
            ProcessingContext context,
            CancellationToken cancellationToken)
        {
            var scenarios = new List<TestScenario>();

            foreach (var criterion in acceptanceCriteria.Take(3)) // Limit to 3 scenarios for now
            {
                var scenario = new TestScenario
                {
                    Name = $"Test {requirement.Title} - {criterion.Substring(0, Math.Min(50, criterion.Length))}",
                    Description = criterion,
                    Steps = new List<string> { "Given the system is ready", "When the user performs the action", "Then the expected result occurs" },
                    ExpectedResults = new List<string> { "The requirement is satisfied" },
                    Priority = requirement.Priority
                };

                scenarios.Add(scenario);
            }

            return scenarios;
        }

        private double CalculateAcceptanceCriteriaQuality(List<string> criteria, List<TestScenario> scenarios)
        {
            if (!criteria.Any())
                return 0.0;

            var criteriaQuality = criteria.Count >= 3 ? 0.6 : 0.3; // Bonus for having multiple criteria
            var scenarioQuality = scenarios.Any() ? 0.4 : 0.0; // Bonus for having test scenarios

            return Math.Min(1.0, criteriaQuality + scenarioQuality);
        }

        private List<string> ParseAcceptanceCriteria(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new List<string>();

            // Simple parsing - split by lines and clean up
            var lines = content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            var criteria = new List<string>();

            foreach (var line in lines)
            {
                var cleanedLine = line.Trim();
                if (cleanedLine.Length > 10 && (cleanedLine.StartsWith("1.") || cleanedLine.StartsWith("2.") || 
                    cleanedLine.StartsWith("3.") || cleanedLine.StartsWith("4.") || cleanedLine.StartsWith("5.")))
                {
                    criteria.Add(cleanedLine.Substring(cleanedLine.IndexOf('.') + 1).Trim());
                }
            }

            return criteria.Any() ? criteria : new List<string> { "Acceptance criteria parsing failed" };
        }

        private List<string> ExtractTerms(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // Simple term extraction - split by spaces and filter
            var words = text.Split(' ').Where(w => !string.IsNullOrWhiteSpace(w)).ToArray();
            return words.Where(w => w.Length > 3 && !w.All(char.IsPunctuation)).ToList();
        }

        private double CalculateRequirementAmbiguity(FeatureRequirement requirement)
        {
            var description = requirement.Description?.ToLower() ?? "";
            var ambiguousTerms = new[] { "maybe", "possibly", "might", "could", "should", "approximately", "around", "about", "etc", "and so on" };
            var ambiguousCount = ambiguousTerms.Count(term => description.Contains(term));
            
            return Math.Min(1.0, ambiguousCount * 0.15); // 15% penalty per ambiguous term
        }

        private string GetPropertyValue(FeatureRequirement requirement, string propertyName)
        {
            var lowerPropertyName = propertyName.ToLower();
            switch (lowerPropertyName)
            {
                case "title":
                    return requirement.Title;
                case "description":
                    return requirement.Description;
                case "type":
                    return requirement.Type.ToString();
                case "priority":
                    return requirement.Priority.ToString();
                default:
                    return string.Empty;
            }
        }

        #endregion
    }
} 